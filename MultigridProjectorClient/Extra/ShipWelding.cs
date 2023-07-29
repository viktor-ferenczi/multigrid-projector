using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Entities;
using SpaceEngineers.Game.Entities.Blocks;
using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using Sandbox.Definitions;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.GameSystems;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.World;
using VRage.Game;
using VRage;
using VRageMath;
using MultigridProjector.Logic;
using MultigridProjectorClient.Utilities;
using VRage.Game.Entity;

namespace MultigridProjectorClient.Extra
{
    internal class ShipWelding
    {
        public static void WeldLoop()
        {
            // Get the currently piloted grid
            if (!(MyAPIGateway.Session?.Player?.Controller?.ControlledEntity?.Entity is MyShipController shipController))
                return;

            foreach (MyShipWelder welder in GetWelders(shipController.CubeGrid, true))
            {
                foreach (MySlimBlock block in GetWeldTargets(welder))
                {
                    MyProjectorBase projector = block.CubeGrid.Projector;

                    if (projector.CanBuild(block, true) != BuildCheckResult.OK)
                        continue;

                    if (!ConnectSubgrids.TryGetSubgrid(block, out Subgrid subgrid))
                        continue;

                    TryWeldPreviewBlock(subgrid, block.Position, welder);
                }
            }
        }

        private static bool TryWeldPreviewBlock(Subgrid subgrid, Vector3I position, MyShipWelder welder)
        {
            MySlimBlock block = subgrid.PreviewGrid.GetCubeBlock(position);
            MyProjectorBase projector = block.CubeGrid.Projector;

            // Check if the ship welder has access to the materials needed to place the block
            // This must be the SHIP welder. Otherwise the block will fail to weld up after being placed due to a lack of materials.
            MyDefinitionId firstComponent = block.BlockDefinition.Components[0].Definition.Id;
            MyGridConveyorSystem welderConveyor = welder.CubeGrid.GridSystems.ConveyorSystem;

            if (!MySession.Static.CreativeMode)
            {
                // This returns the amount of items pulled. If it is greater then zero it was a success and we have pulled the resource.
                // We can kill two birds with one stone by destroying the item as it is pulled to the welder. Not only do we know if the welder would have had access to the item, but it is now 'used up'.
                // ClientLogic.PlacePreviewBlocks will work without any materials and will default to one of the 'bottom' materials when the block is placed. As a result there is no need to move the resource back to the player so that it can be placed.
                bool itemPulled = welderConveyor.PullItem(firstComponent, (MyFixedPoint)1, welder, MyEntityExtensions.GetInventory(welder), true, true) > 0;

                if (!itemPulled)
                    return false;
            }

            // Sanity checks are done by the game for normal welding, but for ship welders we need to do our own sanity checks
            ulong steamId = MyAPIGateway.Session.Player.SteamUserId;
            if (!projector.AllowWelding || !MySession.Static.GetComponent<MySessionComponentDLC>().HasDefinitionDLC(block.BlockDefinition, steamId))
                return false;

            int pcu;
            if (MySession.Static.CreativeMode)
                pcu = block.BlockDefinition.PCU;
            else
                pcu = MyCubeBlockDefinition.PCU_CONSTRUCTION_STAGE_COST;

            if (!welder.IsWithinWorldLimits(projector, block.BlockDefinition.BlockPairName, pcu))
                return false;

            // Build the block
            projector.Build(block, welder.OwnerId, welder.EntityId, builtBy: welder.BuiltBy);

            return true;
        }

        private static HashSet<MyShipWelder> GetWelders(IMyCubeGrid targetGrid, bool recursive = false)
        {
            HashSet<MyShipWelder> welders = new HashSet<MyShipWelder>();
            HashSet<IMyCubeGrid> grids = new HashSet<IMyCubeGrid>();

            if (recursive)
                MyAPIGateway.GridGroups.GetGroup(targetGrid, GridLinkTypeEnum.Mechanical, grids);
            else
                grids.Add(targetGrid);

            foreach (MyCubeGrid grid in grids.Cast<MyCubeGrid>())
            {
                HashSet<MySlimBlock> blocks = grid.GetBlocks();

                welders.UnionWith(blocks
                    .Where(block => block?.FatBlock is IMyShipWelder)
                    .Select(block => block.FatBlock)
                    .OfType<MyShipWelder>());
            }

            return welders;
        }

        private static HashSet<MySlimBlock> GetWeldTargets(MyShipWelder welder)
        {
            HashSet<MySlimBlock> targets = new HashSet<MySlimBlock>();

            // Disabled welders have no targets
            if (welder.Enabled == false)
                return targets;

            BoundingSphere detectorSphere = (BoundingSphere)Reflection.GetValue(welder, "m_detectorSphere");
            BoundingSphereD boundingSphere = new BoundingSphereD(
                Vector3D.Transform(detectorSphere.Center, welder.CubeGrid.WorldMatrix),
                detectorSphere.Radius);

            List<MyEntity> entities = MyEntities.GetEntitiesInSphere(ref boundingSphere);
            List<MyCubeGrid> projectedGrids = entities
                .OfType<MyCubeGrid>()
                .Where(grid => grid.Projector != null)
                .ToList();

            HashSet<MySlimBlock> blocks = new HashSet<MySlimBlock>();
            foreach (MyCubeGrid grid in projectedGrids)
            {
                grid.GetBlocksInsideSphere(ref boundingSphere, blocks);

                // Only keep weldable blocks
                blocks.RemoveWhere(block => grid.Projector.CanBuild(block, checkHavokIntersections: true) != BuildCheckResult.OK);

                targets.UnionWith(blocks);
                blocks.Clear();
            }

            // The functions writing to these variables require they are cleared after use
            entities.Clear();
            blocks.Clear();

            return targets;
        }
    }
}