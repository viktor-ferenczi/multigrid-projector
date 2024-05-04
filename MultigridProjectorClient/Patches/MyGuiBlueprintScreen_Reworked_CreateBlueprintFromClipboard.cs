using System.Linq;
using HarmonyLib;
using MultigridProjector.Extensions;
using MultigridProjector.Utilities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;

namespace MultigridProjectorClient.Patches
{
    // This is called when the player makes a new blueprint (Ctrl-B) or
    // when an existing blueprint is replaced by the clipboard contents
    // ReSharper disable once UnusedType.Global
    [HarmonyPatch(typeof(MyGuiBlueprintScreen_Reworked))]
    [HarmonyPatch("CreateBlueprintFromClipboard")]
    // [EnsureOriginal("4ece7678")]
    // ReSharper disable once InconsistentNaming
    public static class MyGuiBlueprintScreen_Reworked_CreateBlueprintFromClipboard
    {
        [ClientOnly]
        private static bool Prefix(MyGridClipboard ___m_clipboard)
        {
            var gridBuilders = ___m_clipboard.CopiedGrids;
            if (gridBuilders == null || gridBuilders.Count == 0)
                return true;

            gridBuilders.First().AlignToRepairProjector(null);
            gridBuilders.CensorWorldPosition();

            return true;
        }
    }
}