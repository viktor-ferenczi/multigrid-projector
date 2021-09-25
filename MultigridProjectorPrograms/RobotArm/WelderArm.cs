using System;
using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;
using VRage.Game;
using VRageMath;

namespace MultigridProjectorPrograms.RobotArm
{
    public class WelderArm : CollisionDetectingRobotArm
    {
        private readonly IMyProjector projector;
        private readonly MultigridProjectorProgrammableBlockAgent mgp;
        private readonly IMyShipWelder welder;
        public double Cost { get; private set; }

        private WelderArmState state;
        private int countdownToStopRetracting;
        private double bestCostForThisTarget;
        public int FailureCount;
        public int SubgridIndex;
        public readonly HashSet<int> SubgridsWorked = new HashSet<int>();

        public WelderArmState State
        {
            get { return state; }
            set
            {
                if (state == value)
                    return;

                var oldState = state;
                state = value;

                OnStateChanged(oldState);
            }
        }

        // Location (grid index and position) of the preview block to build and weld up
        public BlockLocation TargetLocation { get; private set; }

        // Direction to approach the target from
        public Base6Directions.Direction TargetApproach;

        // Timer to detect non-progressing move,
        // reset to zero each time the arm starts to move to a target block,
        // incremented on moving the arm until welding starts,
        // finishes moving on reaching MovingTimeout
        private int movingTimer;

        // Timer to detect non-progressing welding,
        // reset to zero each time the welding progresses,
        // incremented during the welding process,
        // finishes welding on reaching WeldingTimeout
        private int weldingTimer;

        public WelderArm(IMyProjector projector,
            MultigridProjectorProgrammableBlockAgent mgp,
            IMyTerminalBlock @base,
            Dictionary<long, HashSet<IMyTerminalBlock>> terminalBlocks) : base(@base, terminalBlocks)
        {
            this.projector = projector;
            this.mgp = mgp;

            welder = (IMyShipWelder) EffectorBlock;
            welder.Enabled = false;
        }

        public void Reset(int countdown = int.MaxValue)
        {
            State = WelderArmState.Retracting;
            TargetLocation = new BlockLocation();
            movingTimer = 0;
            weldingTimer = 0;
            FailureCount = 0;
            countdownToStopRetracting = countdown;
        }

        public void Target(BlockLocation location, Base6Directions.Direction approach)
        {
            State = WelderArmState.Moving;
            TargetLocation = location;
            TargetApproach = approach;
            movingTimer = 0;
            weldingTimer = 0;
        }

        private void OnStateChanged(WelderArmState oldState)
        {
            welder.Enabled = State == WelderArmState.Welding;

            switch (State)
            {
                case WelderArmState.Stopped:
                    Stop();
                    SubgridsWorked.Remove(SubgridIndex);
                    SubgridIndex = ChooseAnotherSubgrid();
                    break;

                case WelderArmState.Moving:
                    bestCostForThisTarget = double.PositiveInfinity;
                    break;

                case WelderArmState.Welding:
                case WelderArmState.Finished:
                    SubgridsWorked.Add(SubgridIndex);
                    break;

                case WelderArmState.Failed:
                    SubgridsWorked.Remove(SubgridIndex);
                    SubgridIndex = ChooseAnotherSubgrid();
                    break;

                case WelderArmState.Collided:
                    SubgridIndex = ChooseAnotherSubgrid();
                    Retract();
                    Cost = 0;
                    break;

                case WelderArmState.Retracting:
                    Retract();
                    Cost = 0;
                    break;

                case WelderArmState.Unreachable:
                    Retract();
                    Cost = 0;
                    SubgridsWorked.Remove(SubgridIndex);
                    SubgridIndex = ChooseAnotherSubgrid();
                    break;
            }
        }

        private int ChooseAnotherSubgrid()
        {
            if (SubgridsWorked.Count == 0)
                return -1;

            var index = Shared.Rng.Next() % SubgridsWorked.Count;
            foreach (var subgridIndex in SubgridsWorked)
            {
                if (index-- == 0)
                    return subgridIndex;
            }

            return -1;
        }

        public new void Update()
        {
            switch (State)
            {
                case WelderArmState.Retracting:
                    if (FirstSegment.IsRetracted || countdownToStopRetracting > 0 && --countdownToStopRetracting == 0)
                    {
                        State = WelderArmState.Stopped;
                        movingTimer = 0;
                    }
                    else if (!FirstSegment.IsMoving)
                    {
                        movingTimer++;
                        if (movingTimer > 6)
                            Util.Log($"Retracting arm is stuck for {movingTimer / 6.0:0.0}s");
                    }
                    else
                    {
                        movingTimer = 0;
                    }

                    break;

                case WelderArmState.Moving:
                    Track();
                    break;

                case WelderArmState.Welding:
                    Track();
                    break;

                default:
                    return;
            }

            base.Update();
        }

        private void Track()
        {
            var blockState = mgp.GetBlockState(projector.EntityId, TargetLocation.GridIndex, TargetLocation.Position);
            if (blockState == BlockState.FullyBuilt)
            {
                State = WelderArmState.Finished;
                return;
            }

            if (blockState != BlockState.Buildable && blockState != BlockState.BeingBuilt)
            {
                State = WelderArmState.Failed;
                return;
            }

            var previewGrid = mgp.GetPreviewGrid(projector.EntityId, TargetLocation.GridIndex);
            var previewBlockCoordinates = previewGrid.GridIntegerToWorld(TargetLocation.Position);
            var armBaseWm = FirstSegment.Block.WorldMatrix;
            var direction = previewGrid.WorldMatrix.GetDirectionVector(TargetApproach);
            var target = MatrixD.CreateFromDir(direction, armBaseWm.Up);
            var previewBlockHalfSize = 0.5 * (previewGrid.GridSizeEnum == MyCubeSize.Large ? 2.5 : 1.5);
            var keepDistance = welder.WorldMatrix.Forward * 1.5 * previewBlockHalfSize;
            target.Translation += previewBlockCoordinates - keepDistance;
            Cost = Target(target);
            if (Cost >= Shared.MaxAcceptableCost || Cost > bestCostForThisTarget + Cfg.MovingCostIncreaseLimit)
            {
                State = WelderArmState.Unreachable;
                return;
            }

            bestCostForThisTarget = Math.Min(bestCostForThisTarget, Cost);

            if (HasCollision)
            {
                State = WelderArmState.Collided;
                return;
            }

            var distanceSquared = Vector3D.DistanceSquared(FirstSegment.EffectorTipPose.Translation, previewBlockCoordinates);
            var maxWeldingDistance = previewBlockHalfSize + (welder.CubeGrid.GridSizeEnum == MyCubeSize.Large ? Cfg.MaxWeldingDistanceLargeWelder : Cfg.MaxWeldingDistanceSmallWelder);
            var maxWeldingDistanceSquared = maxWeldingDistance * maxWeldingDistance;
            var welding = distanceSquared <= maxWeldingDistanceSquared;
            // Log($"dsq={distanceSquared:0.000}");
            // Log($"max={maxWeldingDistanceSquared:0.000}");
            if (!welding)
            {
                State = WelderArmState.Moving;

                if (++movingTimer >= Cfg.MovingTimeout)
                    State = WelderArmState.Unreachable;

                return;
            }

            if (++weldingTimer >= Cfg.WeldingTimeout)
            {
                State = WelderArmState.Failed;
                return;
            }

            State = WelderArmState.Welding;
        }
    }
}