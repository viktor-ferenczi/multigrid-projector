using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Linq;
using VRageMath;
using MultigridProjector.Logic;
using MultigridProjector.Api;
using VRage.Utils;
using Sandbox.Game.Entities.Cube;
using MultigridProjectorClient.Utilities;
using VRage.Game;
using MultigridProjector.Extensions;
using Sandbox.Definitions;
using Sandbox.Game.World;
using static Sandbox.Game.Entities.MyCubeGrid;
using MultigridProjector.Utilities;
using Sandbox.Game.Multiplayer;

namespace MultigridProjectorClient.Extra
{
    static class ConnectSubgrids
    {
        public enum ConnectionType
        {
            Default,      // Head added when `Add Head` is clicked
            SmallDefault, // Head added when `Add Small Head` is clicked
            Special,      // Heads manually placed and attached with the `Attach` button
            Legacy,       // Head combinations that can no longer be welded (they can still be projected)
            None,         // There is no connection
        }

        public static bool TryGetSubgrid(MySlimBlock block, out Subgrid subgrid)
        {
            return TryGetSubgrid(block, out subgrid, out _);
        }

        public static bool TryGetSubgrid(MySlimBlock block, out Subgrid subgrid, out MultigridProjection projection)
        {
            subgrid = null;
            projection = null;

            if (block == null)
                return false;

            MyCubeGrid blockGrid = block.CubeGrid;
            MyProjectorBase projector = blockGrid.Projector;

            subgrid = null;

            if (projector == null)
            {
                return MultigridProjection.TryFindProjectionByBuiltGrid(blockGrid, out projection, out subgrid);
            }
            
            if (!MultigridProjection.TryFindProjectionByProjector(projector, out projection))
                return false;

            if (!projection.TryFindPreviewGrid(blockGrid, out int gridIndex))
                return false;

            return projection.TryGetSupportedSubgrid(gridIndex, out subgrid);
        }

        public static void SkinTopParts(MyAttachableTopBlockBase sourceTop, MyAttachableTopBlockBase destinationTop)
        {
            MyCubeGrid destinationGrid = destinationTop.CubeGrid;

            Vector3 color = sourceTop.SlimBlock.ColorMaskHSV;
            MyStringHash armor = sourceTop.SlimBlock.SkinSubtypeId;

            destinationGrid.SkinBlocks(destinationTop.Min, destinationTop.Max, color, armor, false);
        }

        public static MyAttachableTopBlockBase GetTopPart(MyMechanicalConnectionBlockBase block)
        {
            MyProjectorBase projector = block.CubeGrid.Projector;

            // If the block is not projected we can just return its .TopBlock field
            if (projector == null)
                return block.TopBlock;

            TryGetSubgrid(block.SlimBlock, out Subgrid subgrid, out MultigridProjection projection);

            // Get the connection where the preview block (real blocks were handled earlier) equals our target block
            BaseConnection baseConnection = subgrid.BaseConnections
                .Select(kvp => kvp.Value)
                .FirstOrDefault(val => val.Preview == block);

            // The base might not have a top part
            if (baseConnection == null)
                return null;

            // Get the subgrid that the top connection is on
            BlockLocation topLocation = baseConnection.TopLocation;
            if (!projection.TryGetSupportedSubgrid(topLocation.GridIndex, out var topSubgrid))
            {
                return null;
            }

            // Get and return the subpart
            MySlimBlock topSlimBlock = topSubgrid.PreviewGrid.GetCubeBlock(topLocation.Position);
            MyAttachableTopBlockBase topPart = (MyAttachableTopBlockBase)topSlimBlock.FatBlock;

            return topPart;
        }

        public static MyMechanicalConnectionBlockBase GetBasePart(MyAttachableTopBlockBase block)
        {
            MyProjectorBase projector = block.CubeGrid.Projector;

            // If the block is not projected we can just return its .TopBlock field
            if (projector == null)
                return block.Stator;

            TryGetSubgrid(block.SlimBlock, out Subgrid subgrid, out MultigridProjection projection);

            // Get the connection where the preview block (real blocks were handled earlier) equals our target block
            TopConnection topConnection = subgrid.TopConnections
                .Select(kvp => kvp.Value)
                .FirstOrDefault(val => val.Preview == block);

            // The top part might not have a base
            if (topConnection == null)
                return null;

            // Get the subgrid that the base connection is on
            BlockLocation baseLocation = topConnection.BaseLocation;
            if (!projection.TryGetSupportedSubgrid(baseLocation.GridIndex, out var baseSubgrid))
            {
                return null;
            }

            // Get and return the subpart
            MySlimBlock baseSlimBlock = baseSubgrid.PreviewGrid.GetCubeBlock(baseLocation.Position);
            MyMechanicalConnectionBlockBase basePart = (MyMechanicalConnectionBlockBase)baseSlimBlock.FatBlock;

            return basePart;
        }

        public static void UpdateTopParts(MyMechanicalConnectionBlockBase sourceBase, MyMechanicalConnectionBlockBase destinationBase)
        {
            Action OnOldHeadRemove = null;
            Action OnNewHeadAttach = null;

            MyAttachableTopBlockBase sourceTop = GetTopPart(sourceBase);
            MyAttachableTopBlockBase destinationTop = GetTopPart(destinationBase);

            // Get the type of connection to figure out how to create it
            // These are all documented in the ConnectionType Enum
            ConnectionType connectionType = AnalyzeConnection(
                (MyMechanicalConnectionBlockBaseDefinition)sourceBase.BlockDefinition,
                sourceTop?.BlockDefinition);

            if (connectionType == ConnectionType.Default)
            {
                SkinTopParts(sourceTop, destinationTop);
                return;
            }

            // We will need to replace the prebuilt top part since they're not the same
            Construction.GrindBlock(destinationTop.CubeGrid, destinationTop.Position);

            if (connectionType == ConnectionType.SmallDefault)
            {
                OnOldHeadRemove = () => destinationBase.RecreateTop();
                OnNewHeadAttach = () => SkinTopParts(sourceTop, destinationBase.TopBlock);
            }
            else if (connectionType == ConnectionType.Legacy)
            {
                MyAPIGateway.Utilities.ShowMessage("Multigrid Projector",
                    "This connection appears to have been deprecated and cannot be made in survival anymore. " +
                    "Either paste the blueprint in or find one designed for the latest Space Engineers version");
            }
            else if (connectionType == ConnectionType.Special && sourceTop != null)
            {
                MyBlockVisuals visuals = new MyBlockVisuals(
                    sourceTop.SlimBlock.ColorMaskHSV.PackHSVToUint(),
                    sourceTop.SlimBlock.SkinSubtypeId);

                void OnNewHeadBuilt(MyAttachableTopBlockBase newHead)
                {
                    SkinTopParts(sourceTop, newHead);
                    destinationBase.CallAttach(); // This will not work for every scenario but it's worth trying
                }

                OnOldHeadRemove = () =>
                {
                    if (TryPlaceBlock(sourceTop.BlockDefinition, sourceTop.WorldMatrix, visuals))
                    {
                        Events.OnBlockSpawned(
                            (block) => OnNewHeadBuilt((MyAttachableTopBlockBase)block.FatBlock),
                            (block) => block.BlockDefinition == sourceTop.BlockDefinition);
                    }
                    else
                    {
                        MyAPIGateway.Utilities.ShowMessage("Multigrid Projector",
                            $"Could not place top part {sourceTop.BlockDefinition.DisplayNameText}");
                    }
                };
            }

            // Register events
            if (OnOldHeadRemove != null)
            {
                Events.OnNextAttachedChanged(
                    destinationBase,
                    (_) => OnOldHeadRemove(),
                    (_) => destinationBase.TopBlock == null);
            }

            if (OnNewHeadAttach != null)
            {
                Events.OnNextAttachedChanged(
                    destinationBase,
                    (_) => OnNewHeadAttach(),
                    (_) => destinationBase.TopBlock != null);
            }
        }

        public static void UpdateBaseParts(MyAttachableTopBlockBase sourceTop, MyAttachableTopBlockBase destinationTop)
        {
            MyMechanicalConnectionBlockBase sourceBase = GetBasePart(sourceTop);
            MyMechanicalConnectionBlockBase destinationBase = GetBasePart(destinationTop);

            // Can happen if the part is decorative and has no base part
            if (sourceBase == null)
                return;

            // Pistons cannot be attached to their top parts so we can't update them
            if (sourceBase is IMyPistonBase)
                return;

            // This is not implemented as MGP will never need to call UpdateBaseParts in this way
            if (destinationBase != null)
            {
                PluginLog.Error("UpdateBaseParts used on existing base part");
                return;
            }

            MyBlockVisuals visuals = new MyBlockVisuals(
                sourceBase.SlimBlock.ColorMaskHSV.PackHSVToUint(),
                sourceBase.SlimBlock.SkinSubtypeId);

            if (TryPlaceBlock(sourceBase.BlockDefinition, sourceBase.WorldMatrix, visuals))
            {
                MyAPIGateway.Utilities.ShowMessage("Multigrid Projector",
                    "Base block has been placed offset due to its large hit box. " +
                    "Please move it to the head (top part) and attach it. " +
                    "Alternatively remove this base, the head part and weld from the base's direction (if applicable). " +
                    $"Base block: {sourceBase.BlockDefinition.DisplayNameText}");
                
                void OnNewBaseBuild(MyMechanicalConnectionBlockBase newBase)
                {
                    Events.OnNextAttachedChanged(
                        newBase,
                        (_) => Construction.GrindBlock(newBase.TopGrid, newBase.TopBlock.Position),
                        (_) => newBase.TopBlock != null);

                    UpdateBlock.CopyProperties(sourceBase, newBase);
                }
                
                Events.OnBlockSpawned(
                    (block) => OnNewBaseBuild((MyMechanicalConnectionBlockBase)block.FatBlock),
                    (block) => block.BlockDefinition == sourceBase.BlockDefinition);
            }
            else
            {
                MyAPIGateway.Utilities.ShowMessage("Multigrid Projector",
                    $"Could not place base part {sourceBase.BlockDefinition.DisplayNameText}");
            }
        }

        public static ConnectionType AnalyzeConnection(MyMechanicalConnectionBlockBaseDefinition baseDefinition, MyCubeBlockDefinition topDefinition)
        {
            if (topDefinition == null)
                return ConnectionType.None;

            MyCubeBlockDefinitionGroup topDefinitionGroup = MyDefinitionManager.Static.TryGetDefinitionGroup(baseDefinition.TopPart);

            MyCubeSize baseSize = baseDefinition.CubeSize;
            MyCubeSize topSize = topDefinition.CubeSize;

            if (baseSize == MyCubeSize.Large)
            {
                if (topDefinitionGroup[MyCubeSize.Large] == topDefinition)
                    return ConnectionType.Default;

                if (topDefinitionGroup[MyCubeSize.Small] == topDefinition)
                    return ConnectionType.SmallDefault;

                return ConnectionType.Special;
            }

            if (baseSize == MyCubeSize.Small)
            {
                if (topDefinitionGroup[MyCubeSize.Small] == topDefinition)
                    return ConnectionType.Default;

                if (topSize == MyCubeSize.Large)
                    return ConnectionType.Legacy;

                return ConnectionType.Special;
            }

            // This will never happen. It is just to keep the compiler happy.
            return ConnectionType.Special;
        }

        private static bool TryPlaceBlock(MyCubeBlockDefinition blockDefinition, MatrixD worldMatrix, MyBlockVisuals visuals)
        {
            if (!HasPCU(blockDefinition))
                return false;

            if (!(MySession.Static.CreativeMode ||
                MySession.Static.CreativeToolsEnabled(Sync.MyId) ||
                MySession.Static.LocalCharacter.CanStartConstruction(blockDefinition)))
            {
                return false;
            }

            MatrixD matrix = GetClosestPlaceableMatrix(blockDefinition, worldMatrix);
            if (matrix == MatrixD.Zero)
                return false;

            Construction.SpawnBlockOnGrid(blockDefinition, matrix, visuals);
            return true;
        }

        private static bool HasPCU(MyCubeBlockDefinition blockDefinition)
        {
            MySession session = MySession.Static;

            ulong steamId = MyAPIGateway.Session.Player.SteamUserId;
            bool creativeTools = session.CreativeToolsEnabled(steamId) || session.CreativeMode;
            int PCU = creativeTools ? blockDefinition.PCU : MyCubeBlockDefinition.PCU_CONSTRUCTION_STAGE_COST;

            return session.CheckLimitsAndNotify(session.LocalPlayerId, blockDefinition.BlockPairName, PCU, 1);
        }

        private static MatrixD OffsetMatrix(string blockSubType, MatrixD worldMatrix, double offset)
        {
            // The X-Axis is used to move something forward
            Vector3D moveVector = new Vector3D(offset, 0, 0);

            // Rotor parts face upwards so moving the part 'forwards' means moving the world matrix up
            if (blockSubType.Contains("Rotor"))
                moveVector = new Vector3D(0, offset, 0);

            // Rotor bases face upwards so moving the part 'forwards' also means moving the world matrix up
            // However we actually want to move the 'backwards' as they are pointing towards the head, so moving them
            // forwards just result in the block placement intersecting even more
            if (blockSubType.Contains("Stator"))
                moveVector = new Vector3D(0, -offset, 0);


            worldMatrix.Translation += Vector3D.Rotate(moveVector, worldMatrix);

            return worldMatrix;
        }

        private static MatrixD GetClosestPlaceableMatrix(MyCubeBlockDefinition blockDefinition, MatrixD worldMatrix, double maxOffset = 3, double epsilon = 0.001)
        {
            string blockSubType = blockDefinition.Id.SubtypeName;

            // This is essentially a binary search which finds the offset where a block can be placed within `epsilon` accuracy
            double min = 0;
            double mid = 0;
            double max = maxOffset;

            MatrixD newMatrix = worldMatrix;
            while (max - min > epsilon)
            {
                mid = min + (max - min) / 2;
                newMatrix = OffsetMatrix(blockSubType, worldMatrix, mid);

                if (Construction.CanPlaceBlock(blockDefinition, newMatrix))
                    max = mid;
                else
                    min = mid;

                // Leave the function if there is no valid offset within maxOffset
                if (mid > maxOffset - epsilon)
                    return MatrixD.Zero;
            }

            // Do a bit of post-processing where we make sure the block can actually be placed in this value
            while (!Construction.CanPlaceBlock(blockDefinition, newMatrix))
            {
                mid += epsilon;
                newMatrix = OffsetMatrix(blockSubType, worldMatrix, mid);

                // Prevent infinite loops
                if (mid > maxOffset)
                    return MatrixD.Zero;
            }

            // Play it safe so that the desync doesn't cause the placement to fail
            newMatrix = OffsetMatrix(blockSubType, worldMatrix, mid + epsilon * 10);

            return newMatrix;
        }
    }
}