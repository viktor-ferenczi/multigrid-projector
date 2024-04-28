using System.Linq;
using HarmonyLib;
using MultigridProjector.Extensions;
using MultigridProjector.Utilities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;

namespace MultigridProjectorClient.Patches
{
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

            return true;
        }
    }
}