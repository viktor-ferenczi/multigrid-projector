using System;
using HarmonyLib;
using MultigridProjector.Extensions;
using MultigridProjector.Logic;
using MultigridProjector.Utilities;
using Sandbox;
using Sandbox.Game.Entities.Blocks;
using VRage.Game.Entity.EntityComponents.Interfaces;

namespace MultigridProjector.Patches
{
    // ReSharper disable once UnusedType.Global
    [HarmonyPatch(typeof(MyProjectorBase))]
    [HarmonyPatch("UpdateAfterSimulation")]
    [EnsureOriginal("47184779")]
    // ReSharper disable once InconsistentNaming
    public static class MyProjectorBase_UpdateAfterSimulation
    {
        // ReSharper disable once UnusedMember.Local
        private static bool Prefix(
            // ReSharper disable once InconsistentNaming
            MyProjectorBase __instance,
            // ReSharper disable once InconsistentNaming
            IMyGameLogicComponent ___m_gameLogic)
        {
            var projector = __instance;

            try
            {
                return UpdateAfterSimulation(projector, ___m_gameLogic);
            }
            catch (Exception e)
            {
                PluginLog.Error(e);
                return false;
            }
        }

        private static bool UpdateAfterSimulation(MyProjectorBase projector, IMyGameLogicComponent gameLogic)
        {
            // Create the MultigridProjection instance on demand
            if (!MultigridProjection.TryFindProjectionByProjector(projector, out var projection))
            {
                if (projector == null || 
                    projector.Closed || 
                    projector.CubeGrid.IsPreview ||
                    !projector.Enabled || 
                    !projector.IsFunctional || 
                    !projector.AllowWelding || 
                    projector.AllowScaling || 
                    projector.Clipboard.PreviewGrids == null || 
                    projector.Clipboard.PreviewGrids.Count == 0)
                    return true;
                
                var gridBuilders = projector.GetOriginalGridBuilders();
                if (gridBuilders == null || gridBuilders.Count != projector.Clipboard.PreviewGrids.Count)
                    return true;
                    
                projection = MultigridProjection.Create(projector, gridBuilders);
                if (projection == null)
                    return true;
            }

            // Call the base class implementation
            //projector.UpdateAfterSimulation();
            // Could not call virtual base class method, so copied it here from MyEntity where it is defined:
            gameLogic.UpdateAfterSimulation(true);

            // Call custom update logic
            projection.UpdateAfterSimulation();

            // Based on the original code

            var projectionTimer = projector.GetProjectionTimer();
            if (!projector.GetTierCanProject() && projectionTimer > 0)
            {
                --projectionTimer;
                projector.SetProjectionTimer(projectionTimer);
                if (projectionTimer == 0)
                    projector.MyProjector_IsWorkingChanged(projector);
            }

            projector.ResourceSink.Update();
            if (projector.GetRemoveRequested())
            {
                var frameCount = projector.GetFrameCount();
                ++frameCount;
                projector.SetFrameCount(frameCount);

                if (frameCount > 9)
                {
                    projector.UpdateIsWorking();
                    if (projector.IsProjecting())
                    {
                        if (!projector.IsWorking || !projector.TierCanProject || projection.IsBuildCompleted)
                            projector.RemoveProjection(true);
                    }

                    projector.SetFrameCount(0);
                    projector.SetRemoveRequested(false);
                }
            }

            var clipboard = projector.GetClipboard();
            if (!clipboard.IsActive)
                return false;

            clipboard.Update();

            if (projector.GetShouldResetBuildable())
            {
                projector.SetShouldResetBuildable(false);
                foreach (var previewGrid in projection.PreviewGrids)
                {
                    foreach (var cubeBlock in previewGrid.CubeBlocks)
                    {
                        projector.HideCube(cubeBlock);
                    }
                }
            }

            if (!projector.GetForceUpdateProjection() && (!projector.GetShouldUpdateProjection() || MySandboxGame.TotalGamePlayTimeInMilliseconds - projector.GetLastUpdate() <= 2000))
                return false;

            // Call patched UpdateProjection
            var methodInfo = AccessTools.DeclaredMethod(typeof(MyProjectorBase), "UpdateProjection");
            methodInfo.Invoke(projector, new object[]{});
            
            projector.SetShouldUpdateProjection(false);
            projector.SetForceUpdateProjection(false);
            
            projector.SetLastUpdate(MySandboxGame.TotalGamePlayTimeInMilliseconds);

            return false;
        }
    }
}