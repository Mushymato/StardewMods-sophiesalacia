using System.Reflection;
using StardewValley.Locations;

namespace Calcifer.Features.ContentPatcherTokens;

internal class MinesTokens
{
    private static PropertyInfo? IsQuarryArea;

    internal static void RegisterTokens()
    {
        if (Globals.ContentPatcherApi is null)
            return;

        IsQuarryArea = typeof(MineShaft).GetProperty("isQuarryArea", BindingFlags.NonPublic | BindingFlags.Instance);

        Globals.ContentPatcherApi.RegisterToken(Globals.Manifest, "InMines", () =>
        {
            return Context.IsWorldReady switch
            {
                // save is loaded and we are in a mineshaft
                true when Game1.currentLocation is MineShaft ms => ms.mineLevel < 121
                    ? [bool.TrueString]
                    : [bool.FalseString],
                // save is loaded but we are not in a mineshaft
                true => [bool.FalseString],
                // no save loaded (e.g. on the title screen)
                _ => []
            };
        });

        Globals.ContentPatcherApi.RegisterToken(Globals.Manifest, "InQuarry", () =>
            {
                switch (Context.IsWorldReady)
                {
                    // save is loaded and we are in a mineshaft
                    case true when Game1.currentLocation is MineShaft ms:
                        {
                            bool quarryArea = (bool?)IsQuarryArea?.GetValue(ms) ?? false;
                            bool quarryLevel = ms.mineLevel == 77377;

                            return (quarryArea || quarryLevel) ? [bool.TrueString] : [bool.FalseString];
                        }
                    // save is loaded but we are not in a mineshaft
                    case true:
                        return [bool.FalseString];
                    default:
                        // no save loaded (e.g. on the title screen)
                        return [];
                }
            });

        Globals.ContentPatcherApi.RegisterToken(Globals.Manifest, "InSkullCavern", () =>
        {
            return Context.IsWorldReady switch
            {
                // save is loaded and we are in a mineshaft
                true when Game1.currentLocation is MineShaft ms => ms.mineLevel >= 121 && ms.mineLevel != 77377
                    ? [bool.TrueString]
                    : [bool.FalseString],
                // save is loaded but we are not in a mineshaft
                true => [bool.FalseString],
                // no save loaded (e.g. on the title screen)
                _ => []
            };
        });

        Globals.ContentPatcherApi.RegisterToken(Globals.Manifest, "CurrentMineLevel", () =>
            Context.IsWorldReady && Game1.currentLocation is MineShaft ms
                ? [ms.mineLevel.ToString()]
                : []
        );

        Globals.ContentPatcherApi.RegisterToken(Globals.Manifest, "DeepestMineLevel", () =>
            Context.IsWorldReady
                ? [Game1.player.deepestMineLevel.ToString()]
                : []
        );

        Globals.ContentPatcherApi.RegisterToken(Globals.Manifest, "IsHardModeActive", () =>
            Context.IsWorldReady
                ? [(Game1.netWorldState.Value.MinesDifficulty > 0).ToString()]
                : []
        );
    }
}
