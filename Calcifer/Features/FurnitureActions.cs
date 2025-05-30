using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using StardewModdingAPI.Events;
using StardewValley.Objects;
using StardewValley.Triggers;
using xTile.Dimensions;

// ReSharper disable UnusedMember.Global
// ReSharper disable InconsistentNaming

#pragma warning disable IDE1006 // Naming Styles

namespace Calcifer.Features;

[HarmonyPatch]
class FurnitureActionPatches
{
    private const string ActionTriggerString = "sophie.Calcifer_FurnitureTriggered";
    private const string ActionsAssetString = "sophie.Calcifer/FurnitureActions";

    private static Dictionary<string, FurnitureActionData>? _customFurnitureActionsAsset;
    internal static Dictionary<string, FurnitureActionData> CustomFurnitureActionsAsset
    {
        get => _customFurnitureActionsAsset ??=
            Globals.GameContent.Load<Dictionary<string, FurnitureActionData>>(ActionsAssetString);
        set => _customFurnitureActionsAsset = value;
    }

    [HarmonyPatch(typeof(Furniture), nameof(Furniture.checkForAction))]
    [HarmonyPostfix]
    public static void checkForAction_Postfix(Furniture __instance, Farmer who)
    {
        try
        {
            // do associated tile action stuff
            if (!CustomFurnitureActionsAsset.TryGetValue(__instance.QualifiedItemId, out FurnitureActionData? actionData))
                return;

            // iterate through list of possible tile actions, perform first one whose conditions are met
            // update - keep going until told to stop
            foreach (FurnitureActionData.FurnitureAction furnitureAction in actionData.TileActions.Where(furnitureAction => GameStateQuery.CheckConditions(furnitureAction.Condition, location: __instance.Location, player: who, inputItem: who.CurrentItem)))
            {
                Game1.currentLocation.performAction(furnitureAction.TileAction, who, new Location((int)who.Tile.X, (int)who.Tile.Y));

                if (furnitureAction.StopProcessingActions)
                    break;
            }
        
            // raise trigger
            TriggerActionManager.Raise(
                trigger: ActionTriggerString,
                triggerArgs: [__instance, who],
                location: __instance.Location,
                player: who,
                inputItem: who.CurrentItem,
                targetItem: __instance
            );
        }
        catch (Exception e)
        {
            Log.Error($"Failed in {nameof(checkForAction_Postfix)}:\n{e}");
        }
    }
}

[HasEventHooks]
internal static class FurnitureActionHooks
{
    private const string ActionTriggerString = "sophie.Calcifer_FurnitureTriggered";
    private const string ActionsAssetString = "sophie.Calcifer/FurnitureActions";
    private static readonly IAssetName ActionsAssetName = Globals.GameContent.ParseAssetName(ActionsAssetString);

    internal static void InitHooks()
    {
        Globals.EventHelper.GameLoop.GameLaunched += (_, _) => TriggerActionManager.RegisterTrigger(ActionTriggerString);

        // content pipeline
        Globals.EventHelper.Content.AssetRequested += OnAssetRequested;
        Globals.EventHelper.Content.AssetReady += OnAssetReady;
        Globals.EventHelper.Content.AssetsInvalidated += OnAssetsInvalidated;
    }

    private static void OnAssetsInvalidated(object? sender, AssetsInvalidatedEventArgs e)
    {
        if (!e.NamesWithoutLocale.Contains(ActionsAssetName))
            return;

        FurnitureActionPatches.CustomFurnitureActionsAsset = Game1.content.Load<Dictionary<string, FurnitureActionData>>(ActionsAssetString);
    }

    private static void OnAssetReady(object? sender, AssetReadyEventArgs e)
    {
        if (!e.NameWithoutLocale.IsEquivalentTo(ActionsAssetString))
            return;

        FurnitureActionPatches.CustomFurnitureActionsAsset = Game1.content.Load<Dictionary<string, FurnitureActionData>>(ActionsAssetString);
    }

    private static void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(ActionsAssetString))
            e.LoadFrom(() => new Dictionary<string, FurnitureActionData>(), AssetLoadPriority.Low);
    }
}

public class FurnitureActionData
{
    public class FurnitureAction
    {
        public string Condition = "";
        public string TileAction = "";
        public bool StopProcessingActions = true;
    }

    public List<FurnitureAction> TileActions = [];
}

#pragma warning restore IDE1006 // Naming Styles
