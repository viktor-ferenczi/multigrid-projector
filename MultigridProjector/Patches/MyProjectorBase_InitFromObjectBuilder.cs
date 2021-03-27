using System;
using System.Collections.Generic;
using HarmonyLib;
using MultigridProjector.Extensions;
using MultigridProjector.Logic;
using MultigridProjector.Utilities;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using VRage.Game;

namespace MultigridProjector.Patches
{
    // ReSharper disable once UnusedType.Global
    [HarmonyPatch(typeof(MyProjectorBase))]
    [HarmonyPatch("InitFromObjectBuilder")]
    [EnsureOriginal("8e865331")]
    // ReSharper disable once InconsistentNaming
    public static class MyProjectorBase_InitFromObjectBuilder
    {
        // ReSharper disable once UnusedMember.Local
        private static bool Prefix(
            // ReSharper disable once InconsistentNaming
            MyProjectorBase __instance,
            List<MyObjectBuilder_CubeGrid> gridsObs)
        {
            try
            {
                return InitFromObjectBuilder(__instance, gridsObs);
            }
            catch (Exception e)
            {
                PluginLog.Error(e);
                return false;
            }
        }

        public static bool InitFromObjectBuilder(MyProjectorBase projector, List<MyObjectBuilder_CubeGrid> gridBuilders)
        {
            if (gridBuilders == null)
                return true;

            // Projector update
            projector.ResourceSink.Update();
            projector.UpdateIsWorking();

            if (!projector.Enabled)
                return true;

            // Fall back to the original implementation to handle failure cases
            if (gridBuilders.Count < 1)
                return true;

            // Is the projector is dead?
            if (!projector.IsWorking)
                return false;

            // Clone and remap the blueprint before modifying it
            gridBuilders = gridBuilders.Clone();
            MyEntities.RemapObjectBuilderCollection(gridBuilders);

            projector.SetHiddenBlock(null);

            // Fixes the multiplayer preview position issue with console blocks (aka hologram table) and now projectors, which caused by damaged
            // first subgrid position. Something is clearing the first subgrid's position, but if we transform the whole blueprint to the origin,
            // then this is not a problem.
            // IMPORTANT: This issue does not appear in single player! Even testing it needs two players in multiplayer setup!
            gridBuilders.NormalizeBlueprintPositionAndOrientation();

            // Console block (aka hologram table)?
            if (!projector.AllowWelding || projector.AllowScaling)
            {
                gridBuilders.PrepareForConsoleProjection(projector.GetClipboard());
                projector.SetOriginalGridBuilders(gridBuilders);
                projector.SendNewBlueprint(gridBuilders);
                return false;
            }

            // Prevent re-initializing an existing multigrid projection
            if (MultigridProjection.TryFindProjectionByProjector(projector, out _))
                return false;

            // Ensure compatible grid size between the projector and the first subgrid to be built
            var compatibleGridSize = gridBuilders[0].GridSizeEnum == projector.CubeGrid.GridSizeEnum;
            if (!compatibleGridSize)
                return true;

            // Sign up for auto alignment
            MultigridProjection.ProjectorsWithBlueprintLoadedByHand.Add(projector.EntityId);

            // Prepare the blueprint for being projected for welding
            gridBuilders.PrepareForProjection();

            // Load the blueprint
            projector.SetOriginalGridBuilders(gridBuilders);

            // Notify the server and all clients (including this one) to create the projection,
            // our data model will be created by SetNewBlueprint the same way at all locations
            projector.SendNewBlueprint(gridBuilders);
            return false;
        }
   }
}