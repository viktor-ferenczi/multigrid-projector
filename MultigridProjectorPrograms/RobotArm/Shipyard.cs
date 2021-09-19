using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.ModAPI.Ingame;
using VRageMath;

namespace MultigridProjectorPrograms.RobotArm
{
    public class Shipyard
    {
        private readonly IMyGridTerminalSystem gridTerminalSystem;
        private readonly IMyProjector projector;
        private readonly MultigridProjectorProgrammableBlockAgent mgp;
        private readonly List<WelderArm> arms = new List<WelderArm>();
        private readonly List<Subgrid> subgrids = new List<Subgrid>();
        private int totalTicks;

        public Shipyard(IMyGridTerminalSystem gridTerminalSystem, IMyProjector projector, MultigridProjectorProgrammableBlockAgent mgp)
        {
            this.gridTerminalSystem = gridTerminalSystem;
            this.projector = projector;
            this.mgp = mgp;

            var armBases = new List<IMyMechanicalConnectionBlock>();
            gridTerminalSystem.GetBlockGroupWithName(Cfg.WelderArmsGroupName)?.GetBlocksOfType(armBases);
            if (armBases.Count == 0)
            {
                Util.Log("Add all arm base blocks to the Welder Arms group!");
                return;
            }

            var terminalBlocks = FindAllTerminalBlocks();
            foreach (var armBaseBlock in armBases)
                arms.Add(new WelderArm(projector, mgp, armBaseBlock, terminalBlocks));
        }

        private Dictionary<long, HashSet<IMyTerminalBlock>> FindAllTerminalBlocks()
        {
            var terminalBlocks = new Dictionary<long, HashSet<IMyTerminalBlock>>();

            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            gridTerminalSystem.GetBlocksOfType<IMyMechanicalConnectionBlock>(blocks);
            RegisterTerminalBlocks(terminalBlocks, blocks);

            blocks.Clear();
            gridTerminalSystem.GetBlocksOfType<IMyShipToolBase>(blocks);
            RegisterTerminalBlocks(terminalBlocks, blocks);

            return terminalBlocks;
        }

        private static void RegisterTerminalBlocks(Dictionary<long, HashSet<IMyTerminalBlock>> terminalBlocks, List<IMyTerminalBlock> blocks)
        {
            foreach (var block in blocks)
            {
                var gridId = block.CubeGrid.EntityId;
                HashSet<IMyTerminalBlock> gridBlocks;
                if (!terminalBlocks.TryGetValue(gridId, out gridBlocks))
                {
                    gridBlocks = new HashSet<IMyTerminalBlock>();
                    terminalBlocks[gridId] = gridBlocks;
                }

                gridBlocks.Add(block);
            }
        }

        public void Stop()
        {
            foreach (var arm in arms)
                arm.Stop();
        }

        private void Reset()
        {
            subgrids.Clear();
            RetractAll();
        }

        public void RetractAll()
        {
            foreach (var arm in arms)
                arm.Reset();
        }

        public void Update(IMyTextPanel lcdDetails, IMyTextPanel lcdStatus, IMyTextPanel lcdTimer)
        {
            if (!mgp.Available)
                return;

            var subgridCount = mgp.GetSubgridCount(projector.EntityId);
            if (!projector.Enabled || subgridCount < 1)
            {
                if (totalTicks > 0)
                {
                    // Build finished
                    lcdDetails?.WriteText("Completed");
                    lcdStatus?.WriteText("");
                    totalTicks = 0;
                }

                Reset();

                foreach (var arm in arms)
                    arm.Update();

                return;
            }

            if (subgrids.Count != subgridCount)
            {
                totalTicks = 0;
                subgrids.Clear();
                for (var subgridIndex = 0; subgridIndex < subgridCount; subgridIndex++)
                    subgrids.Add(new Subgrid(projector.EntityId, mgp, subgridIndex));
            }

            // Update buildable block states on at most one arm
            foreach (var subgrid in subgrids)
                subgrid.Update();

            // Retarget arms
            foreach (var arm in arms)
                Assign(arm);

            // Control all arms on every tick, this must be smooth
            foreach (var arm in arms)
                arm.Update();

            ShowStatus(lcdStatus);

            var info = projector.DetailedInfo;
            var index = info.IndexOf("Build progress:", StringComparison.InvariantCulture);
            lcdDetails?.WriteText(index >= 0 ? info.Substring(index) : "");

            var seconds = ++totalTicks / 6;
            lcdTimer?.WriteText($"{seconds / 60:00}:{seconds % 60:00}");
        }

        private void Assign(WelderArm arm)
        {
            switch (arm.State)
            {
                case WelderArmState.Failed:
                    if (++arm.FailureCount >= Cfg.ResetArmAfterFailedWeldingAttempts)
                        arm.Reset();
                    else
                        AssignNextBlock(arm, arm.FirstSegment.EffectorTipPose.Translation);
                    break;

                case WelderArmState.Stopped:
                    AssignNextBlock(arm, arm.FirstSegment.Block.WorldMatrix.Translation);
                    break;

                case WelderArmState.Finished:
                    arm.FailureCount = 0;
                    AssignNextBlock(arm, arm.FirstSegment.EffectorTipPose.Translation);
                    break;

                case WelderArmState.Collided:
                    arm.Reset(Cfg.MaxRetractionTimeAfterCollision);
                    break;

                case WelderArmState.Unreachable:
                    arm.Reset(Cfg.MaxRetractionTimeAfterUnreachable);
                    break;
            }
        }

        private void AssignNextBlock(WelderArm arm, Vector3D referencePosition)
        {
            arm.Reset();

            Subgrid subgridToWeld = null;
            var nearestDistanceSquared = double.PositiveInfinity;
            var positionToWeld = Vector3I.Zero;

            var subgridIndex = arm.SubgridIndex % subgrids.Count;

            var checkAllSubgrids = arm.SubgridIndex == -1;
            if (checkAllSubgrids)
                subgridIndex = 0;

            for (var i = 0; i < subgrids.Count; i++)
            {
                var subgrid = subgrids[subgridIndex];

                if (++subgridIndex >= subgrids.Count)
                    subgridIndex = 0;

                if (!subgrid.HasBuilt)
                    continue;

                if (subgrid.HasFinished)
                {
                    arm.SubgridsWorked.Remove(subgrid.Index);
                    continue;
                }

                foreach (var position in subgrid.IterWeldableBlockPositions())
                {
                    var previewGrid = mgp.GetPreviewGrid(projector.EntityId, subgrid.Index);
                    var worldCoords = previewGrid.GridIntegerToWorld(position);
                    var distanceSquared = Vector3D.DistanceSquared(referencePosition, worldCoords);
                    if (distanceSquared < nearestDistanceSquared)
                    {
                        subgridToWeld = subgrid;
                        nearestDistanceSquared = distanceSquared;
                        positionToWeld = position;
                    }
                }

                if (subgridToWeld != null && !checkAllSubgrids)
                    break;
            }

            if (subgridToWeld == null)
                return;

            arm.SubgridIndex = subgridToWeld.Index;

            var location = new BlockLocation(subgridToWeld.Index, positionToWeld);
            var approach = (Base6Directions.Direction) (Shared.Rng.Next() % 6);
            arm.Target(location, approach);
            subgridToWeld.Remove(positionToWeld);
        }

        private void ShowStatus(IMyTextPanel lcdStatus)
        {
            if (lcdStatus == null)
                return;

            var sb = new StringBuilder();
            sb.Append("Sub Block position    Cost State\r\n");
            sb.Append("--- --------------    ---- -----\r\n");
            foreach (var arm in arms)
            {
                var active = arm.State == WelderArmState.Moving || arm.State == WelderArmState.Welding;
                var subgridIndexText = (active ? arm.TargetLocation.GridIndex.ToString() : "-").PadLeft(3);
                var positionText = (active ? Util.Format(arm.TargetLocation.Position) : "").PadRight(14);
                var costText = (active ? (arm.Cost < 1000 ? $"{arm.Cost:0.000}" : "-") : "").PadLeft(7);
                sb.Append($"{subgridIndexText} {positionText} {costText} {arm.State}\r\n");
            }

            sb.Append("\r\n");
            sb.Append("Sub Blocks Layers Welding Blocks\r\n");
            sb.Append("--- ------ ------ ------- ------\r\n");
            foreach (var subgrid in subgrids)
            {
                if (!subgrid.HasBuilt || subgrid.HasFinished)
                    continue;

                int lastLayerToWeld;
                var subgridIndexText = subgrid.Index.ToString().PadLeft(3);
                var blockCountText = subgrid.BuildableBlockCount.ToString().PadLeft(6);
                var layerCountText = subgrid.LayerIndex.ToString().PadLeft(6);
                var layerBlockCountText = subgrid.CountWeldableBlocks(out lastLayerToWeld).ToString().PadLeft(5);
                var weldedLayerText = $"{1 + subgrid.WeldedLayer}-{1 + lastLayerToWeld}".PadLeft(7);
                sb.Append($"{subgridIndexText} {blockCountText} {layerCountText} {weldedLayerText} {layerBlockCountText}\r\n");
            }

            lcdStatus.WriteText(sb.ToString());
        }
    }
}