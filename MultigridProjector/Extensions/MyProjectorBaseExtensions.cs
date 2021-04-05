using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MultigridProjector.Utilities;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
using VRage.Game;
using VRage.Sync;
using VRageMath;

namespace MultigridProjector.Extensions
{
    public static class MyProjectorBaseExtensions
    {
        // ReSharper disable once InconsistentNaming
        private static readonly MethodInfo MyProjector_IsWorkingChangedMethodInfo = AccessTools.DeclaredMethod(typeof(MyProjectorBase), "MyProjector_IsWorkingChanged");

        public static void MyProjector_IsWorkingChanged(this MyProjectorBase projector, MyCubeBlock obj)
        {
            MyProjector_IsWorkingChangedMethodInfo.Invoke(projector, new object[] {obj});
        }

        private static readonly MethodInfo IsProjectingMethodInfo = AccessTools.DeclaredMethod(typeof(MyProjectorBase), "IsProjecting");

        public static bool IsProjecting(this MyProjectorBase obj)
        {
            return (bool) IsProjectingMethodInfo.Invoke(obj, new object[] { });
        }

        private static readonly MethodInfo RequestRemoveProjectionMethodInfo = AccessTools.DeclaredMethod(typeof(MyProjectorBase), "RequestRemoveProjection");

        public static void RequestRemoveProjection(this MyProjectorBase obj)
        {
            RequestRemoveProjectionMethodInfo.Invoke(obj, new object[] { });
        }

        private static readonly MethodInfo RemoveProjectionMethodInfo = AccessTools.DeclaredMethod(typeof(MyProjectorBase), "RemoveProjection");

        public static void RemoveProjection(this MyProjectorBase obj, bool keepProjection)
        {
            RemoveProjectionMethodInfo.Invoke(obj, new object[] {keepProjection});
        }

        private static readonly MethodInfo SendNewBlueprintMethodInfo = AccessTools.DeclaredMethod(typeof(MyProjectorBase), "SendNewBlueprint");

        public static void SendNewBlueprint(this MyProjectorBase obj, List<MyObjectBuilder_CubeGrid> projectedGrids)
        {
            SendNewBlueprintMethodInfo.Invoke(obj, new object[] {projectedGrids});
        }

        private static readonly FieldInfo ClipboardFieldInfo = AccessTools.Field(typeof(MyProjectorBase), "m_clipboard");

        public static MyProjectorClipboard GetClipboard(this MyProjectorBase projector)
        {
            return ClipboardFieldInfo.GetValue(projector) as MyProjectorClipboard;
        }

        private static readonly FieldInfo BuildableBlocksCountFieldInfo = AccessTools.Field(typeof(MyProjectorBase), "m_buildableBlocksCount");

        public static void SetBuildableBlocksCount(this MyProjectorBase projector, int value)
        {
            BuildableBlocksCountFieldInfo.SetValue(projector, value);
        }

        private static readonly FieldInfo ShowOnlyBuildableFieldInfo = AccessTools.Field(typeof(MyProjectorBase), "m_showOnlyBuildable");

        public static bool GetShowOnlyBuildable(this MyProjectorBase projector)
        {
            return (bool) ShowOnlyBuildableFieldInfo.GetValue(projector);
        }

        private static readonly FieldInfo KeepProjectionFieldInfo = AccessTools.Field(typeof(MyProjectorBase), "m_keepProjection");

        public static bool GetKeepProjection(this MyProjectorBase projector)
        {
            return (Sync<bool, SyncDirection.BothWays>) KeepProjectionFieldInfo.GetValue(projector);
        }

        private static readonly FieldInfo InstantBuildingEnabledFieldInfo = AccessTools.Field(typeof(MyProjectorBase), "m_instantBuildingEnabled");

        public static bool GetInstantBuildingEnabled(this MyProjectorBase projector)
        {
            return (Sync<bool, SyncDirection.BothWays>) InstantBuildingEnabledFieldInfo.GetValue(projector);
        }

        private static readonly FieldInfo ShouldUpdateTextsFieldInfo = AccessTools.Field(typeof(MyProjectorBase), "m_shouldUpdateTexts");

        public static bool GetShouldUpdateTexts(this MyProjectorBase projector)
        {
            return (bool) ShouldUpdateTextsFieldInfo.GetValue(projector);
        }

        public static void SetShouldUpdateTexts(this MyProjectorBase projector, bool value)
        {
            ShouldUpdateTextsFieldInfo.SetValue(projector, value);
        }

        private static readonly FieldInfo RemainingBlocksFieldInfo = AccessTools.Field(typeof(MyProjectorBase), "m_remainingBlocks");

        public static void SetRemainingBlocks(this MyProjectorBase projector, int value)
        {
            RemainingBlocksFieldInfo.SetValue(projector, value);
        }

        private static readonly MethodInfo CanBuildMethodInfo = AccessTools.DeclaredMethod(typeof(MyProjectorBase), "CanBuild", new[] {typeof(MySlimBlock)});

        public static bool CanBuild(this MyProjectorBase projector, MySlimBlock cubeBlock)
        {
            return (bool) CanBuildMethodInfo.Invoke(projector, new object[] {cubeBlock});
        }

        private static readonly MethodInfo UpdateSoundsMethodInfo = AccessTools.DeclaredMethod(typeof(MyProjectorBase), "UpdateSounds");

        public static void UpdateSounds(this MyProjectorBase projector)
        {
            UpdateSoundsMethodInfo.Invoke(projector, new object[] { });
        }

        private static readonly MethodInfo UpdateTextMethodInfo = AccessTools.DeclaredMethod(typeof(MyProjectorBase), "UpdateText");

        public static void UpdateText(this MyProjectorBase projector)
        {
            UpdateTextMethodInfo.Invoke(projector, new object[] { });
        }

        private static readonly FieldInfo StatsDirtyFieldInfo = AccessTools.Field(typeof(MyProjectorBase), "m_statsDirty");

        public static void SetStatsDirty(this MyProjectorBase projector, bool value)
        {
            StatsDirtyFieldInfo.SetValue(projector, value);
        }

        private static readonly FieldInfo TotalBlocksFieldInfo = AccessTools.Field(typeof(MyProjectorBase), "m_totalBlocks");

        public static void SetTotalBlocks(this MyProjectorBase projector, int value)
        {
            TotalBlocksFieldInfo.SetValue(projector, value);
        }

        private static readonly FieldInfo RemainingArmorBlocksFieldInfo = AccessTools.Field(typeof(MyProjectorBase), "m_remainingArmorBlocks");

        public static void SetRemainingArmorBlocks(this MyProjectorBase projector, int value)
        {
            RemainingArmorBlocksFieldInfo.SetValue(projector, value);
        }

        private static readonly FieldInfo RemainingBlocksPerTypeFieldInfo = AccessTools.Field(typeof(MyProjectorBase), "m_remainingBlocksPerType");

        public static Dictionary<MyCubeBlockDefinition, int> GetRemainingBlocksPerType(this MyProjectorBase projector)
        {
            return (Dictionary<MyCubeBlockDefinition, int>) RemainingBlocksPerTypeFieldInfo.GetValue(projector);
        }

        private static readonly FieldInfo OriginalGridBuildersFieldInfo = AccessTools.Field(typeof(MyProjectorBase), "m_originalGridBuilders");

        public static List<MyObjectBuilder_CubeGrid> GetOriginalGridBuilders(this MyProjectorBase projector)
        {
            return (List<MyObjectBuilder_CubeGrid>) OriginalGridBuildersFieldInfo.GetValue(projector);
        }

        public static void SetOriginalGridBuilders(this MyProjectorBase projector, List<MyObjectBuilder_CubeGrid> gridBuilders)
        {
            OriginalGridBuildersFieldInfo.SetValue(projector, gridBuilders);
        }

        private static readonly FieldInfo ProjectionTimerFieldInfo = AccessTools.Field(typeof(MyProjectorBase), "m_projectionTimer");

        public static int GetProjectionTimer(this MyProjectorBase projector)
        {
            return (int) ProjectionTimerFieldInfo.GetValue(projector);
        }

        public static void SetProjectionTimer(this MyProjectorBase projector, int value)
        {
            ProjectionTimerFieldInfo.SetValue(projector, value);
        }

        private static readonly FieldInfo HiddenBlockFieldInfo = AccessTools.Field(typeof(MyProjectorBase), "m_hiddenBlock");

        public static void SetHiddenBlock(this MyProjectorBase projector, MySlimBlock block)
        {
            HiddenBlockFieldInfo.SetValue(projector, block);
        }

        private static readonly FieldInfo TierCanProjectFieldInfo = AccessTools.Field(typeof(MyProjectorBase), "m_tierCanProject");

        public static bool GetTierCanProject(this MyProjectorBase obj)
        {
            return (bool) TierCanProjectFieldInfo.GetValue(obj);
        }

        private static readonly FieldInfo RemoveRequestedFieldInfo = AccessTools.Field(typeof(MyProjectorBase), "m_removeRequested");

        public static bool GetRemoveRequested(this MyProjectorBase projector)
        {
            return (bool) RemoveRequestedFieldInfo.GetValue(projector);
        }

        public static void SetRemoveRequested(this MyProjectorBase projector, bool value)
        {
            RemoveRequestedFieldInfo.SetValue(projector, value);
        }

        private static readonly FieldInfo ShouldResetBuildableFieldInfo = AccessTools.Field(typeof(MyProjectorBase), "m_shouldResetBuildable");

        public static bool GetShouldResetBuildable(this MyProjectorBase projector)
        {
            return (bool) ShouldResetBuildableFieldInfo.GetValue(projector);
        }

        public static void SetShouldResetBuildable(this MyProjectorBase projector, bool value)
        {
            ShouldResetBuildableFieldInfo.SetValue(projector, value);
        }

        private static readonly FieldInfo ForceUpdateProjectionFieldInfo = AccessTools.Field(typeof(MyProjectorBase), "m_forceUpdateProjection");

        public static bool GetForceUpdateProjection(this MyProjectorBase projector)
        {
            return (bool) ForceUpdateProjectionFieldInfo.GetValue(projector);
        }

        public static void SetForceUpdateProjection(this MyProjectorBase projector, bool value)
        {
            ForceUpdateProjectionFieldInfo.SetValue(projector, value);
        }

        private static readonly FieldInfo ShouldUpdateProjectionFieldInfo = AccessTools.Field(typeof(MyProjectorBase), "m_shouldUpdateProjection");

        public static bool GetShouldUpdateProjection(this MyProjectorBase projector)
        {
            return (bool) ShouldUpdateProjectionFieldInfo.GetValue(projector);
        }

        public static void SetShouldUpdateProjection(this MyProjectorBase projector, bool value)
        {
            ShouldUpdateProjectionFieldInfo.SetValue(projector, value);
        }

        private static readonly FieldInfo LastUpdateFieldInfo = AccessTools.Field(typeof(MyProjectorBase), "m_lastUpdate");

        public static int GetLastUpdate(this MyProjectorBase projector)
        {
            return (int) LastUpdateFieldInfo.GetValue(projector);
        }

        public static void SetLastUpdate(this MyProjectorBase projector, int value)
        {
            LastUpdateFieldInfo.SetValue(projector, value);
        }

        private static readonly FieldInfo ProjectionRotationFieldInfo = AccessTools.Field(typeof(MyProjectorBase), "m_projectionRotation");

        public static Vector3I GetProjectionRotation(this MyProjectorBase projector)
        {
            return (Vector3I) ProjectionRotationFieldInfo.GetValue(projector);
        }

        public static void SetProjectionRotation(this MyProjectorBase projector, Vector3I rotation)
        {
            ProjectionRotationFieldInfo.SetValue(projector, rotation);
        }

        private static readonly FieldInfo ProjectionOffsetFieldInfo = AccessTools.Field(typeof(MyProjectorBase), "m_projectionOffset");

        public static Vector3I GetProjectionOffset(this MyProjectorBase projector)
        {
            return (Vector3I) ProjectionOffsetFieldInfo.GetValue(projector);
        }

        public static void SetProjectionOffset(this MyProjectorBase projector, Vector3I offset)
        {
            ProjectionOffsetFieldInfo.SetValue(projector, offset);
        }

        private static readonly PropertyInfo IsActivatingFieldInfo = AccessTools.Property(typeof(MyProjectorBase), "IsActivating");

        public static void SetIsActivating(this MyProjectorBase projector, bool value)
        {
            IsActivatingFieldInfo.SetValue(projector, value);
        }

        private static readonly MethodInfo SetTransparencyMethodInfo = AccessTools.DeclaredMethod(typeof(MyProjectorBase), "SetTransparency");

        public static void SetTransparency(this MyProjectorBase projector, MySlimBlock cubeBlock, float transparency)
        {
            SetTransparencyMethodInfo.Invoke(projector, new object[] {cubeBlock, transparency});
        }

        private static readonly MethodInfo SetRotationMethodInfo = AccessTools.DeclaredMethod(typeof(MyProjectorBase), "SetRotation");

        public static void SetRotation(this MyProjectorBase projector, MyGridClipboard clipboard, Vector3I rotation)
        {
            SetRotationMethodInfo.Invoke(projector, new object[] {clipboard, rotation});
        }

        public static void RemapObjectBuilders(this MyProjectorBase projector)
        {
            var gridBuilders = projector.GetOriginalGridBuilders();
            if (gridBuilders == null || gridBuilders.Count <= 0)
                return;

            // Consistent remapping of all grids to keep sub-grid relations intact
            MyEntities.RemapObjectBuilderCollection(gridBuilders);
        }

        public static bool AlignToRepairProjector(this MyProjectorBase projector, MyObjectBuilder_CubeGrid gridBuilder)
        {
            var projectorBuilder = gridBuilder
                .CubeBlocks
                .OfType<MyObjectBuilder_Projector>()
                .FirstOrDefault(b =>
                    b.SubtypeId == projector.BlockDefinition.Id.SubtypeId &&
                    (Vector3I) b.Min == projector.Min &&
                    (MyBlockOrientation) b.BlockOrientation == projector.Orientation &&
                    (b.CustomName ?? b.Name) == projector.GetSafeName());

            if (projectorBuilder == null)
                return false;

            projector.Orientation.GetQuaternion(out var gridToProjectorQuaternion);
            var projectorToGridQuaternion = Quaternion.Inverse(gridToProjectorQuaternion);
            if (!OrientationAlgebra.ProjectionRotationFromForwardAndUp(Base6Directions.GetDirection(projectorToGridQuaternion.Forward), Base6Directions.GetDirection(projectorToGridQuaternion.Up), out var projectionRotation))
                return false;

            var anchorBlock = projector.CubeGrid.CubeBlocks.FirstOrDefault();
            if (anchorBlock == null)
                return false;

            var offsetInsideGrid = projector.Position - anchorBlock.Position;
            var projectionOffset = new Vector3I(Vector3.Round(projectorToGridQuaternion * offsetInsideGrid));
            projectionOffset = Vector3I.Clamp(projectionOffset, new Vector3I(-50), new Vector3I(50));

            projector.SetProjectionRotation(projectionRotation);
            projector.SetProjectionOffset(projectionOffset);

            return true;
        }
    }
}