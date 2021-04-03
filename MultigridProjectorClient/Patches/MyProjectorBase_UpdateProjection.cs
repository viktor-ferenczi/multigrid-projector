using HarmonyLib;
using MultigridProjector.Utilities;
using Sandbox.Game.Entities.Blocks;

namespace MultigridProjector.Patches
{
    // ReSharper disable once UnusedType.Global
    [HarmonyPatch(typeof(MyProjectorBase))]
    [HarmonyPatch("UpdateProjection")]
    [EnsureOriginal("576ba4a7")]
    // ReSharper disable once InconsistentNaming
    public static class MyProjectorBase_UpdateProjection
    {
        // ReSharper disable once InconsistentNaming
        // ReSharper disable once UnusedMember.Local
        private static bool Prefix(MyProjectorBase __instance)
        {
            var projector = __instance;

            // Console blocks set up the hologram look here
            if (!projector.AllowWelding || projector.AllowScaling)
                return true;

            // Disallow any use of the original ProjectorUpdateWork
            // In theory this is not called for projectors, but disable it just in case.
            return false;
        }
    }
}