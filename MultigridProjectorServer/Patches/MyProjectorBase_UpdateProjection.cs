using System;
using System.Reflection;
using MultigridProjector.Logic;
using MultigridProjector.Utilities;
using Sandbox.Game.Entities.Blocks;
using Torch.Managers.PatchManager;

namespace MultigridProjector.Patches
{
    [PatchShim]
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once UnusedType.Global
    public static class MyProjectorBase_UpdateProjection
    {
        public static void Patch(PatchContext ctx) => ctx.GetPattern(typeof (MyProjectorBase).GetMethod("UpdateProjection", BindingFlags.DeclaredOnly | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic)).Prefixes.Add(typeof (MyProjectorBase_UpdateProjection).GetMethod(nameof(Prefix), BindingFlags.Static | BindingFlags.NonPublic));
        
        // ReSharper disable once InconsistentNaming
        // ReSharper disable once UnusedMember.Local
        private static bool Prefix(MyProjectorBase __instance)
        {
            var projector = __instance;

            try
            {
                return MultigridProjection.MyProjectorBase_UpdateProjection(projector);
            }
            catch (Exception e)
            {
                PluginLog.Error(e);
            }

            return false;
        }
    }
}