using System.Linq;
using HarmonyLib;
using MultigridProjector.Extensions;
using MultigridProjector.Utilities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;
using Sandbox.Game.GUI;
using VRage.Game;

namespace MultigridProjectorClient.Patches
{
    // This is called when the player makes a new blueprint (Ctrl-B) or
    // when an existing blueprint is replaced by the clipboard contents
    // ReSharper disable once UnusedType.Global
    [HarmonyPatch]
    // ReSharper disable once InconsistentNaming
    public static class MyGuiBlueprintScreenPatches
    {
        private static bool InCreateBlueprintFromClipboard;

        // ReSharper disable once UnusedMember.Local
        [HarmonyPrefix]
        [HarmonyPatch(typeof(MyGuiBlueprintScreen_Reworked))]
        [HarmonyPatch("CreateBlueprintFromClipboard")]
        private static bool CreateBlueprintFromClipboardPrefix()
        {
            InCreateBlueprintFromClipboard = true;
            return true;
        }

        // ReSharper disable once UnusedMember.Local
        [HarmonyPostfix]
        [HarmonyPatch(typeof(MyGuiBlueprintScreen_Reworked))]
        [HarmonyPatch("CreateBlueprintFromClipboard")]
        private static void CreateBlueprintFromClipboardPostfix()
        {
            InCreateBlueprintFromClipboard = false;
        }

        // ReSharper disable once UnusedMember.Local
        [HarmonyPrefix]
        [HarmonyPatch(typeof(MyBlueprintUtils))]
        [HarmonyPatch(nameof(MyBlueprintUtils.SavePrefabToFile))]
        private static bool SavePrefabToFilePrefix(MyObjectBuilder_Definitions prefab, bool replace)
        {
            if (replace || InCreateBlueprintFromClipboard)
                ProcessBlueprint(prefab);

            return true;
        }

        private static void ProcessBlueprint(MyObjectBuilder_Definitions definitions)
        {
            foreach (var blueprint in definitions.ShipBlueprints)
            {
                if (blueprint.CubeGrid != null)
                {
                    blueprint.CubeGrid.AlignToRepairProjector(null);
                    blueprint.CubeGrid.CensorWorldPosition();
                }

                if (blueprint.CubeGrids != null && blueprint.CubeGrids.Length != 0)
                {
                    blueprint.CubeGrids.First().AlignToRepairProjector(null);
                    blueprint.CubeGrids.CensorWorldPosition();
                }
            }
        }
    }
}