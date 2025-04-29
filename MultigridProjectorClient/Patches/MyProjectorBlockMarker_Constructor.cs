using HarmonyLib;
using System.Reflection;


namespace MultigridProjectorClient.Patches
{
    [HarmonyPatch]
    // ReSharper disable once UnusedType.Global
    public static class MyProjectorBlockMarkerCtorPatch
    {
        // ReSharper disable once UnusedMember.Local
        private static MethodBase TargetMethod()
        {
            var type = AccessTools.Inner(typeof(Sandbox.Game.Entities.Blocks.MyProjectorBase), "MyProjectorBlockMarker");
            return AccessTools.Constructor(type, new[] { typeof(int), typeof(int) });
        }

        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once InconsistentNaming
        private static bool Prefix(out int maxMissingBlocks, out int maxUnfinishedBlocks)
        {
            // Disable Keen's block highlighting
            maxMissingBlocks = 0;
            maxUnfinishedBlocks = 0;

            return true;
        }
    }
}