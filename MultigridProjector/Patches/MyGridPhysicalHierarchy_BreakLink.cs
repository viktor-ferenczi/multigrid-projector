using HarmonyLib;
using MultigridProjector.Utilities;
using Sandbox.Engine.Physics;
using Sandbox.Game.Entities;

namespace MultigridProjector.Patches
{
    // ReSharper disable once UnusedType.Global
    [HarmonyPatch(typeof(MyGridPhysicalHierarchy))]
    [HarmonyPatch("BreakLink")]
    [EnsureOriginal("72e7d0ea")]
    // ReSharper disable once InconsistentNaming
    public static class MyGridPhysicalHierarchy_BreakLink
    {
        public static bool Prefix(
            // ReSharper disable once InconsistentNaming
            MyGridPhysicalHierarchy __instance,
            long linkId, 
            MyCubeGrid parentNode, 
            MyCubeGrid childNode)
        {
            // Crash fix on cutting mechanical groups. It only prevents the crash, does not properly fix the root cause,
            // which is most likely in the original server code base.
            
            // See also: https://support.keenswh.com/spaceengineers/pc/topic/1-105-024-ds-crash-keynotfoundexception-detatching-mechanical-connection
            
            // The original reporter of the problem (Lord Tylus) correlated a very similar issue with NPC ship cleanup event, when those ships were
            // deleted from the world. That is a similar operation to manually cutting out (deleting) a group of mechanically connected grids.
            
            // 100% reproduction rate achieved using the Rings MGP test world by welding up the 3 subgrids, then deleting a block in one of them
            // causing a grid split, then cutting one of the split grid parts with Ctrl-Shift-X, separating the remaining subgrids from the projector
            // by deleting a block next to it, then cutting all remaining (still mechanically connected) group of grids by Ctrl-X. 
            
            if (childNode == null)
            {
                // This is the actual error case which can happen on deletion of a mechanically connected group of grids 
                PluginLog.Warn("MyGridPhysicalHierarchy_BreakLink: Skipping the original BreakLink method to avoid crash on deleting a group of mechanically connected subgrids");
                
                // Skip the original implementation. It may leave some garbage, but that is still better than crashing the server hard
                return false;
            }

            if (parentNode == null)
            {
                // Handling this case as well, just in case (it did not actually happen)
                PluginLog.Warn("MyGridPhysicalHierarchy_BreakLink: Parent node is null, this should not happen");
                return false;
            }
            
            // Run the original implementation
            return true;
        }
    }
}