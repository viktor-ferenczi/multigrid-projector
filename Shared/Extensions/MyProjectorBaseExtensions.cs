using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MultigridProjector.Utilities;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
using VRage.Game;
using VRageMath;

namespace MultigridProjector.Extensions
{
    // The projector's internal members are reached directly thanks to the Krafs publicizer.
    // Game methods that are private/internal or plain (non-virtual) protected (IsProjecting,
    // RemoveProjection, SendNewBlueprint, UpdateSounds, UpdateText, SetRotation, CheckMissingDlcs,
    // RequestRemoveProjection, MyProjector_IsWorkingChanged) are now called on the instance
    // directly, so no wrapper extensions are needed for them.
    //
    // SetTransparency is the exception: it is protected *virtual*, which the Pulsar/Magnetar
    // source-compile publicizer leaves protected (publicizing a virtual member would break
    // override chains, since an override cannot change accessibility). It must therefore still
    // be reached by reflection. In the local Krafs build the public instance method wins over
    // this extension, so the wrapper is harmless there.
    public static class MyProjectorBaseExtensions
    {
        private static readonly MethodInfo SetTransparencyMethodInfo = Validation.EnsureInfo(AccessTools.DeclaredMethod(typeof(MyProjectorBase), "SetTransparency"));

        public static void SetTransparency(this MyProjectorBase projector, MySlimBlock cubeBlock, float transparency)
        {
            SetTransparencyMethodInfo.Invoke(projector, new object[] {cubeBlock, transparency});
        }

        public static MyProjectorClipboard GetClipboard(this MyProjectorBase projector)
        {
            return projector.Clipboard;
        }

        public static void SetBuildableBlocksCount(this MyProjectorBase projector, int value)
        {
            projector.m_buildableBlocksCount = value;
        }

        public static bool GetShowOnlyBuildable(this MyProjectorBase projector)
        {
            return projector.m_showOnlyBuildable;
        }

        public static bool GetKeepProjection(this MyProjectorBase projector)
        {
            return projector.m_keepProjection;
        }

        // Sets KeepProjection by writing the synced backing field directly, exactly as the
        // game's own KeepProjection property setter does. This avoids the fragile terminal
        // property path (block.SetValue("KeepProjection", ...)), which throws
        // InvalidOperationException("Invalid property") whenever the "KeepProjection" terminal
        // control has not been registered for the block's runtime type yet (see issue #99).
        public static void SetKeepProjection(this MyProjectorBase projector, bool value)
        {
            projector.m_keepProjection.Value = value;
        }

        public static bool GetInstantBuildingEnabled(this MyProjectorBase projector)
        {
            return projector.m_instantBuildingEnabled;
        }

        public static bool GetShouldUpdateTexts(this MyProjectorBase projector)
        {
            return projector.m_shouldUpdateTexts;
        }

        public static void SetShouldUpdateTexts(this MyProjectorBase projector, bool value)
        {
            projector.m_shouldUpdateTexts = value;
        }

        public static void SetRemainingBlocks(this MyProjectorBase projector, int value)
        {
            projector.m_remainingBlocks = value;
        }

        public static void SetStatsDirty(this MyProjectorBase projector, bool value)
        {
            projector.m_statsDirty = value;
        }

        public static void SetTotalBlocks(this MyProjectorBase projector, int value)
        {
            projector.m_totalBlocks = value;
        }

        public static void SetRemainingArmorBlocks(this MyProjectorBase projector, int value)
        {
            projector.m_remainingArmorBlocks = value;
        }

        public static int GetRemainingArmorBlocks(this MyProjectorBase projector)
        {
            return projector.m_remainingArmorBlocks;
        }

        public static Dictionary<MyCubeBlockDefinition, int> GetRemainingBlocksPerType(this MyProjectorBase projector)
        {
            return projector.m_remainingBlocksPerType;
        }

        public static List<MyObjectBuilder_CubeGrid> GetOriginalGridBuilders(this MyProjectorBase projector)
        {
            return projector.m_originalGridBuilders;
        }

        public static void SetOriginalGridBuilders(this MyProjectorBase projector, List<MyObjectBuilder_CubeGrid> gridBuilders)
        {
            projector.m_originalGridBuilders = gridBuilders;
        }

        public static int GetProjectionTimer(this MyProjectorBase projector)
        {
            return projector.m_projectionTimer;
        }

        public static void SetProjectionTimer(this MyProjectorBase projector, int value)
        {
            projector.m_projectionTimer = value;
        }

        public static void SetHiddenBlock(this MyProjectorBase projector, MySlimBlock block)
        {
            projector.m_hiddenBlock = block;
        }

        public static bool GetTierCanProject(this MyProjectorBase projector)
        {
            return projector.m_tierCanProject;
        }

        public static bool GetRemoveRequested(this MyProjectorBase projector)
        {
            return projector.m_removeRequested;
        }

        public static void SetRemoveRequested(this MyProjectorBase projector, bool value)
        {
            projector.m_removeRequested = value;
        }

        public static bool GetShouldResetBuildable(this MyProjectorBase projector)
        {
            return projector.m_shouldResetBuildable;
        }

        public static void SetShouldResetBuildable(this MyProjectorBase projector, bool value)
        {
            projector.m_shouldResetBuildable = value;
        }

        public static bool GetForceUpdateProjection(this MyProjectorBase projector)
        {
            return projector.m_forceUpdateProjection;
        }

        public static void SetForceUpdateProjection(this MyProjectorBase projector, bool value)
        {
            projector.m_forceUpdateProjection = value;
        }

        public static bool GetShouldUpdateProjection(this MyProjectorBase projector)
        {
            return projector.m_shouldUpdateProjection;
        }

        public static void SetShouldUpdateProjection(this MyProjectorBase projector, bool value)
        {
            projector.m_shouldUpdateProjection = value;
        }

        public static int GetLastUpdate(this MyProjectorBase projector)
        {
            return projector.m_lastUpdate;
        }

        public static void SetLastUpdate(this MyProjectorBase projector, int value)
        {
            projector.m_lastUpdate = value;
        }

        public static Vector3I GetProjectionRotation(this MyProjectorBase projector)
        {
            return projector.m_projectionRotation;
        }

        public static void SetIsActivating(this MyProjectorBase projector, bool value)
        {
            projector.IsActivating = value;
        }

        public static void RemapObjectBuilders(this MyProjectorBase projector)
        {
            var gridBuilders = projector.GetOriginalGridBuilders();
            if (gridBuilders == null || gridBuilders.Count <= 0)
                return;

            // Consistent remapping of all grids to keep sub-grid relations intact
            lock (gridBuilders)
                MyEntities.RemapObjectBuilderCollection(gridBuilders);
        }
    }
}
