using System;
using System.Reflection;
using MultigridProjector.Logic;
using MultigridProjector.Utilities;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.Weapons;
using Sandbox.ModAPI;
using Torch.Managers.PatchManager;

namespace MultigridProjector.Patches
{
    [PatchShim]
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once UnusedType.Global
    public static class MyWelder_FindProjectedBlock
    {
        public static void Patch(PatchContext ctx) => ctx.GetPattern(typeof (MyWelder).GetMethod("FindProjectedBlock", BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Static)).Prefixes.Add(typeof (MyWelder_FindProjectedBlock).GetMethod(nameof(Prefix), BindingFlags.Static | BindingFlags.NonPublic));
        
        private static bool Prefix(
            // ReSharper disable once InconsistentNaming
            MyCasterComponent rayCaster,
            float distanceMultiplier,
            // ReSharper disable once InconsistentNaming
            ref MyWelder.ProjectionRaycastData __result)
        {
            __result = new MyWelder.ProjectionRaycastData {raycastResult = BuildCheckResult.NotFound};
            
            try
            {
                return MultigridProjection.MyWelder_FindProjectedBlock(rayCaster, distanceMultiplier, ref __result); 
            }
            catch (Exception e)
            {
                PluginLog.Error(e);
            }

            return false;
        }
    }
}