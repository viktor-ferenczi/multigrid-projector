using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MultigridProjector.Utilities;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Game.Screens.Helpers;
using Sandbox.ModAPI;
using Sandbox.Graphics.GUI;
using SpaceEngineers.Game.Entities.Blocks;
using VRage.Game;
using VRage.ObjectBuilder;
using VRage.Sync;
using VRageMath;

namespace MultigridProjector.Extensions
{
    public static class MyCubeBlockExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetSafeName(this MyCubeBlock block)
        {
            return block?.DisplayNameText ?? block?.DisplayName ?? block?.Name ?? "";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetDebugName(this MyCubeBlock block)
        {
            return $"{block.GetSafeName()} [{block.EntityId}]";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasSameDefinition(this MySlimBlock block, MySlimBlock other)
        {
            return block.BlockDefinition.Id == other.BlockDefinition.Id;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsMatchingBuilder(this MySlimBlock previewBlock, MyObjectBuilder_CubeBlock blockBuilder)
        {
            return previewBlock.BlockDefinition.Id == blockBuilder.GetId() &&
                   previewBlock.Orientation.Forward == blockBuilder.BlockOrientation.Forward &&
                   previewBlock.Orientation.Up == blockBuilder.BlockOrientation.Up;
        }

        private static readonly MethodInfo RecreateTopInfo = Validation.EnsureInfo(AccessTools.DeclaredMethod(typeof(MyMechanicalConnectionBlockBase), "RecreateTop"));

        public static void RecreateTop(this MyMechanicalConnectionBlockBase stator, long? builderId = null, MyMechanicalConnectionBlockBase.MyTopBlockSize topSize = MyMechanicalConnectionBlockBase.MyTopBlockSize.Normal, bool instantBuild = false)
        {
            RecreateTopInfo.Invoke(stator, new object[] { builderId, topSize, instantBuild });
        }

        // Aligns the grid of the block to a corresponding block on another grid
        public static void AlignGrid(this MyCubeBlock block, MyCubeBlock referenceBlock)
        {
            // Misaligned preview grid position and orientation
            var wm = block.CubeGrid.WorldMatrix;

            // Center the preview block
            wm.Translation -= block.WorldMatrix.Translation;

            // Reorient to match the built block
            wm *= MatrixD.Invert(block.PositionComp.GetOrientation());
            wm *= referenceBlock.PositionComp.GetOrientation();

            // Translate to the built block's position
            wm.Translation += referenceBlock.WorldMatrix.Translation;

            // Move the preview grid
            block.CubeGrid.PositionComp.SetWorldMatrix(ref wm, skipTeleportCheck: true);
        }

        public static List<MyCubeGrid> GetFocusedGridsInMechanicalGroup(this MyCubeBlock focusedBlock)
        {
            var physicalGroup = MyCubeGridGroups.Static.Physical.GetGroup(focusedBlock.CubeGrid);
            if (physicalGroup == null)
                return null;

            var grids = physicalGroup.Nodes.Select(node => node.NodeData).ToList();

            grids.Remove(focusedBlock.CubeGrid);
            grids.Insert(0, focusedBlock.CubeGrid);

            return grids;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MyToolbar GetToolbar(this MyTerminalBlock block)
        {
            switch (block)
            {
                case MySensorBlock b:
                    return b.Toolbar;
                case MyButtonPanel b:
                    return b.Toolbar;
                case MyEventControllerBlock b:
                    return b.Toolbar;
                case MyFlightMovementBlock b:
                    return b.Toolbar;
                case MyShipController b:
                    return b.Toolbar;
                case MyTimerBlock b:
                    return b.Toolbar;
                case MyDefensiveCombatBlock b:
                    return b.GetWaypointActionsToolbar();
            }

            return null;
        }

        private static readonly FieldInfo WaypointActionsToolbarField = AccessTools.DeclaredField(typeof(MyDefensiveCombatBlock), "m_waypointActionsToolbar");

        public static MyToolbar GetWaypointActionsToolbar(this MyDefensiveCombatBlock defensiveCombatBlock)
        {
            return (MyToolbar)WaypointActionsToolbarField.GetValue(defensiveCombatBlock);
        }

        private static readonly FieldInfo BoundCameraSyncField = AccessTools.DeclaredField(typeof(MyRemoteControl), "m_bindedCamera" /* sic */);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Sync<long, SyncDirection.BothWays> GetBoundCameraSync(this MyRemoteControl remoteControlBlock)
        {
            return (Sync<long, SyncDirection.BothWays>)BoundCameraSyncField.GetValue(remoteControlBlock);
        }

        private static readonly MethodInfo AddBlocksMethod = AccessTools.DeclaredMethod(typeof(MyEventControllerBlock), "AddBlocks");

        public static void AddBlocks(this MyEventControllerBlock eventControllerBlock, List<long> toSync)
        {
            AddBlocksMethod.Invoke(eventControllerBlock, new[] { toSync });
        }

        private static readonly MethodInfo RemoveBlocksMethod = AccessTools.DeclaredMethod(typeof(MyEventControllerBlock), "RemoveBlocks");

        public static void RemoveBlocks(this MyEventControllerBlock eventControllerBlock, List<long> toSync)
        {
            RemoveBlocksMethod.Invoke(eventControllerBlock, new[] { toSync });
        }

        private static readonly FieldInfo SelectedBlockIdsField = AccessTools.DeclaredField(typeof(MyEventControllerBlock), "m_selectedBlockIds");

        public static MySerializableList<long> GetSelectedBlockIds(this MyEventControllerBlock eventControllerBlock)
        {
            return (MySerializableList<long>)SelectedBlockIdsField.GetValue(eventControllerBlock);
        }

        public static void SetSelectedBlockIds(this MyEventControllerBlock eventControllerBlock, MySerializableList<long> selectedBlockIds)
        {
            SelectedBlockIdsField.SetValue(eventControllerBlock, selectedBlockIds);
        }

        private static readonly FieldInfo SelectedBlocksField = AccessTools.DeclaredField(typeof(MyEventControllerBlock), "m_selectedBlocks");

        public static Dictionary<long, IMyTerminalBlock> GetSelectedBlocks(this MyEventControllerBlock eventControllerBlock)
        {
            return (Dictionary<long, IMyTerminalBlock>)SelectedBlocksField.GetValue(eventControllerBlock);
        }

        private static readonly FieldInfo SelectedBlockIdsFieldInfo = AccessTools.Field(typeof(MyEventControllerBlock), "m_selectedBlockIds");
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MySerializableList<long> GetSelectedBlockIds(this MyEventControllerBlock block)
        {
            return SelectedBlockIdsFieldInfo.GetValue(block) as MySerializableList<long>;
        }

        private static readonly MethodInfo SelectAvailableBlocksMethodInfo = AccessTools.DeclaredMethod(typeof(MyEventControllerBlock), "SelectAvailableBlocks");
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SelectAvailableBlocks(this MyEventControllerBlock block, List<MyGuiControlListbox.Item> selection)
        {
            SelectAvailableBlocksMethodInfo.Invoke(block, new object[]{selection});
        }

        private static readonly MethodInfo SelectButtonMethodInfo = AccessTools.DeclaredMethod(typeof(MyEventControllerBlock), "SelectButton");
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SelectButton(this MyEventControllerBlock block)
        {
            SelectButtonMethodInfo.Invoke(block, new object[]{});
        }
    }
}