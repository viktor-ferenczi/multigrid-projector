using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI;
using VRage.Game;

namespace MultigridProjectorPrograms.RobotArm
{
    public static class Shared
    {
        public static readonly Random Rng = new Random();

        // Maximum acceptable optimized cost for the arm to start targeting a block
        public static readonly double MaxAcceptableCost = Cfg.MaxWeldingDistanceLargeWelder * Cfg.MaxWeldingDistanceLargeWelder + 2 * (Cfg.DirectionCostWeight + Cfg.RollCostWeight) + Cfg.ActivationRegularization;

        // Stops optimizing the pose before the loop count limit if this cost level is reached
        public static readonly double GoodEnoughCost = 0.04 * Cfg.MaxWeldingDistanceSmallWelder * Cfg.MaxWeldingDistanceSmallWelder;

        public static double CalculatePoseCost(ref MatrixD target, ref MatrixD pose)
        {
            return Vector3D.DistanceSquared(pose.Translation, target.Translation) +
                   Vector3D.DistanceSquared(pose.Forward, target.Forward) * Cfg.DirectionCostWeight +
                   Vector3D.DistanceSquared(pose.Up, target.Up) * Cfg.RollCostWeight;
        }
    }

    public interface ISegment<out T> where T : IMyTerminalBlock
    {
        // Base block of the arm segment or the effector block
        T Block { get; }

        // Transformation from the Block to the effector's tip according to the current optimized pose
        MatrixD Transform { get; }

        // Returns the pose of the effector's tip according to the current physical position of the arm
        MatrixD EffectorTipPose { get; }

        // Returns the sum of activation costs for the current optimized arm pose
        double SumActivationCosts { get; }

        // Returns true if the whole arm is near to its initial position and not moving
        bool IsRetracted { get; }

        // Returns true if any part of the arm is moving
        bool IsMoving { get; }

        // Yields each block on the arm
        IEnumerable<IMyTerminalBlock> IterBlocks();

        // Sets the optimized pose to the current mechanical position for the optimization to converge from there
        void Init();

        // Changes the optimized activations to move the optimized effector tip towards the target pose,
        // returns the positive cost value for the optimized target pose, lower is better
        double Optimize(ref MatrixD wm, ref MatrixD target);

        // Simulation update step to control the mechanical bases of the arm
        void Update();

        // Retracts the arm to its default position, which is currently all zero activations
        void Retract();

        // Stops all mechanical bases in the arm (emergency stop)
        void Stop();
    }

    public class Effector<T> : ISegment<T> where T : IMyTerminalBlock
    {
        public Effector(T block, MatrixD tip)
        {
            Block = block;
            Transform = tip;
        }

        public T Block { get; }

        public MatrixD Transform { get; }

        public MatrixD EffectorTipPose => Transform * Block.WorldMatrix;

        public double SumActivationCosts => 0;

        public bool IsRetracted => true;

        public bool IsMoving => false;

        public IEnumerable<IMyTerminalBlock> IterBlocks()
        {
            yield return Block;
        }

        public void Init()
        {
        }

        public double Optimize(ref MatrixD wm, ref MatrixD target)
        {
            var pose = Transform * wm;
            // VerifyPose(Block.CustomName, Block.WorldMatrix, wm);
            // VerifyPose("Pose", Transform * Block.WorldMatrix, pose);
            return Shared.CalculatePoseCost(ref target, ref pose);
        }

        public void Update()
        {
        }

        public void Retract()
        {
        }

        public void Stop()
        {
        }
    }

    public abstract class Segment<T> : ISegment<T> where T : IMyMechanicalConnectionBlock
    {
        private readonly ISegment<IMyTerminalBlock> next;
        private readonly MatrixD topToNext;
        private readonly int segmentCount;
        private readonly double activationWeight;
        private double optimizedActivation;
        private double previousActivation;
        private double activationVelocity;
        protected readonly double InitialActivation;
        protected double ActivationRange;
        protected double MinActivationStep;

        protected Segment(T block, ISegment<IMyTerminalBlock> next)
        {
            Block = block;
            this.next = next;

            topToNext = next.Block.WorldMatrix * MatrixD.Invert(block.Top.WorldMatrix);
            previousActivation = GetPhysicalActivation();

            var nextSegment = this.next as Segment<IMyMechanicalConnectionBlock>;
            segmentCount = 1 + (nextSegment?.segmentCount ?? 0);
            activationWeight = Cfg.ActivationRegularization / segmentCount;

            double v;
            if (double.TryParse((block.CustomData ?? "").Trim(), out v))
                InitialActivation = ClampPosition(v);

            foreach (var s in (block.CustomName ?? "").Split(' '))
            {
                if (s.StartsWith("=") && double.TryParse(s.Substring(1), out v))
                    InitialActivation = ClampPosition(v);
            }
        }

        public T Block { get; }

        public MatrixD Transform
        {
            get
            {
                var nextTransform = next.Transform;
                return GetTipTransform(optimizedActivation, ref nextTransform);
            }
        }

        public MatrixD EffectorTipPose => next.EffectorTipPose;

        public double SumActivationCosts => ActivationCost(optimizedActivation) + next.SumActivationCosts;

        public bool IsRetracted => Math.Abs(GetPhysicalActivation() - InitialActivation) < 0.1 && Math.Abs(GetVelocity()) < 0.1 && next.IsRetracted;

        public bool IsMoving => Math.Abs(activationVelocity) >= 0.1 || next.IsMoving;

        public IEnumerable<IMyTerminalBlock> IterBlocks()
        {
            yield return Block;

            foreach (var block in next.IterBlocks())
                yield return block;
        }

        private double ActivationCost(double activation) => Math.Pow((activation - InitialActivation) / ActivationRange, 2);

        private MatrixD GetTipTransform(double activation, ref MatrixD nextTransform) => nextTransform * GetSegmentTransform(activation);

        private MatrixD GetSegmentTransform(double activation) => topToNext * GetBaseToTopTransform(activation);

        private MatrixD CalculatePose(ref MatrixD wm, double activation, ref MatrixD nextTransform) => GetTipTransform(activation, ref nextTransform) * wm;

        public void Init()
        {
            optimizedActivation = GetPhysicalActivation();
            next.Init();
        }

        public double Optimize(ref MatrixD wm, ref MatrixD target)
        {
            // VerifyPose(Block.CustomName, Block.WorldMatrix, wm);
            OptimizeActivation(ref wm, ref target);
            var nextWm = GetSegmentTransform(optimizedActivation) * wm;
            next.Optimize(ref nextWm, ref target);
            return OptimizeActivation(ref wm, ref target);
        }

        private double OptimizeActivation(ref MatrixD wm, ref MatrixD target)
        {
            var nextTransform = next.Transform;
            var nextSumActivationCosts = next.SumActivationCosts;

            var tipPose = CalculatePose(ref wm, optimizedActivation, ref nextTransform);
            var cost = Shared.CalculatePoseCost(ref target, ref tipPose) + (ActivationCost(optimizedActivation) + nextSumActivationCosts) * activationWeight;

            var step = ActivationRange * 0.5;
            while (cost > Shared.GoodEnoughCost && Math.Abs(step) > MinActivationStep)
            {
                var activation1 = ClampPosition(optimizedActivation - step);
                var pose1 = CalculatePose(ref wm, activation1, ref nextTransform);
                var cost1 = Shared.CalculatePoseCost(ref target, ref pose1) + (ActivationCost(activation1) + nextSumActivationCosts) * activationWeight;

                var activation2 = ClampPosition(optimizedActivation + step);
                var pose2 = CalculatePose(ref wm, activation2, ref nextTransform);
                var cost2 = Shared.CalculatePoseCost(ref target, ref pose2) + (ActivationCost(activation2) + nextSumActivationCosts) * activationWeight;

                if (cost1 < cost)
                {
                    optimizedActivation = activation1;
                    cost = cost1;
                }

                if (cost2 < cost)
                {
                    optimizedActivation = activation2;
                    cost = cost2;
                }

                step *= 0.5;
            }

            return cost;
        }

        public void Update()
        {
            var current = GetPhysicalActivation();
            activationVelocity = (current - previousActivation) * 6;
            previousActivation = current;
            var velocity = DetermineVelocity(current, this.optimizedActivation);
            SetVelocity(velocity);
            next.Update();
        }

        public void Retract()
        {
            optimizedActivation = InitialActivation;
            next.Retract();
        }

        public void Stop()
        {
            SetVelocity(0);
            next.Stop();
        }

        protected double DetermineVelocity(double current, double target, double speed = 2.0)
        {
            var delta = target - current;
            var sign = Math.Sign(delta);
            var absDelta = delta * 0.5 * sign;
            var velocity = ClampVelocity(speed * absDelta);
            return sign * velocity;
        }

        protected abstract double GetPhysicalActivation();
        protected abstract double ClampPosition(double p);
        protected abstract double ClampVelocity(double v);
        protected abstract double GetVelocity();
        protected abstract void SetVelocity(double velocity);
        protected abstract MatrixD GetBaseToTopTransform(double activation);
    }

    public class RotorSegment : Segment<IMyMotorStator>
    {
        private const double MinVelocity = Math.PI / 1440;

        private const double MaxVelocity = Math.PI / 8;

        public RotorSegment(IMyMotorStator block, ISegment<IMyTerminalBlock> next) : base(block, next)
        {
            MinActivationStep = Cfg.MinActivationStepRotor;
            ActivationRange = Math.Max(MinActivationStep, Math.Max(Block.UpperLimitRad - InitialActivation, InitialActivation - Block.LowerLimitRad));
        }

        protected override double GetPhysicalActivation() => Block.Angle;

        protected override double ClampPosition(double pos) => Math.Max(Block.LowerLimitRad, Math.Min(Block.UpperLimitRad, pos));

        protected override double ClampVelocity(double v) => v < MinVelocity ? 0 : Math.Min(MaxVelocity, v);

        protected override double GetVelocity() => Block.TargetVelocityRad;

        protected override void SetVelocity(double velocity) => Block.TargetVelocityRad = (float) velocity;

        protected override MatrixD GetBaseToTopTransform(double activation) => MatrixD.CreateTranslation(Vector3D.Up * (0.2 + Block.Displacement)) * MatrixD.CreateFromAxisAngle(Vector3D.Down, activation);
    }

    public class HingeSegment : Segment<IMyMotorStator>
    {
        private const double MinVelocity = Math.PI / 1440;
        private const double MaxVelocity = Math.PI / 8;

        public HingeSegment(IMyMotorStator block, ISegment<IMyTerminalBlock> next) : base(block, next)
        {
            MinActivationStep = Cfg.MinActivationStepHinge;
            ActivationRange = Math.Max(MinActivationStep, Math.Max(Block.UpperLimitRad - InitialActivation, InitialActivation - Block.LowerLimitRad));
        }

        protected override double GetPhysicalActivation() => Block.Angle;

        protected override double ClampPosition(double pos) => Math.Max(Block.LowerLimitRad, Math.Min(Block.UpperLimitRad, pos));

        protected override double ClampVelocity(double v) => v < MinVelocity ? 0 : Math.Min(MaxVelocity, v);

        protected override double GetVelocity() => Block.TargetVelocityRad;

        protected override void SetVelocity(double velocity) => Block.TargetVelocityRad = (float) velocity;

        protected override MatrixD GetBaseToTopTransform(double activation) => MatrixD.CreateFromAxisAngle(Vector3D.Down, activation);
    }

    public class PistonSegment : Segment<IMyPistonBase>
    {
        private const double MinVelocity = 0.0006;
        private const double MaxVelocity = 2.5;

        public PistonSegment(IMyPistonBase block, ISegment<IMyTerminalBlock> next) : base(block, next)
        {
            MinActivationStep = Cfg.MinActivationStepPiston;
            ActivationRange = Math.Max(MinActivationStep, Math.Max(Block.HighestPosition - InitialActivation, InitialActivation - Block.LowestPosition));
        }

        protected override double GetPhysicalActivation() => Block.CurrentPosition;

        protected override double ClampPosition(double pos) => Math.Max(Block.LowestPosition, Math.Min(Block.HighestPosition, pos));

        protected override double ClampVelocity(double v) => v < MinVelocity ? 0 : Math.Min(MaxVelocity, v);

        protected override double GetVelocity() => Block.Velocity;

        protected override void SetVelocity(double velocity) => Block.Velocity = (float) velocity;

        protected override MatrixD GetBaseToTopTransform(double activation) => MatrixD.CreateTranslation(Vector3D.Up * (1.4 + activation));
    }

    public class RobotArm
    {
        public readonly ISegment<IMyTerminalBlock> FirstSegment;
        protected IMyTerminalBlock EffectorBlock;

        public RobotArm(IMyTerminalBlock @base, Dictionary<long, HashSet<IMyTerminalBlock>> terminalBlocks)
        {
            FirstSegment = Discover(@base, terminalBlocks);
            FirstSegment.Init();
        }

        private ISegment<IMyTerminalBlock> Discover(IMyTerminalBlock block, Dictionary<long, HashSet<IMyTerminalBlock>> terminalBlocks)
        {
            var pistonBase = block as IMyPistonBase;
            if (pistonBase != null)
            {
                var tip = FindTip(pistonBase, terminalBlocks);
                var next = Discover(tip, terminalBlocks);
                return new PistonSegment(pistonBase, next);
            }

            var rotorBase = block as IMyMotorStator;
            if (rotorBase != null)
            {
                var tip = FindTip(rotorBase, terminalBlocks);
                var next = Discover(tip, terminalBlocks);
                if (rotorBase.BlockDefinition.SubtypeName.EndsWith("Hinge"))
                    return new HingeSegment(rotorBase, next);
                return new RotorSegment(rotorBase, next);
            }

            EffectorBlock = block;

            var welder = block as IMyShipWelder;
            if (welder != null)
            {
                var tipDistance = 0.7 * (welder.CubeGrid.GridSizeEnum == MyCubeSize.Large ? 2.5 : 1.5);
                var tip = MatrixD.CreateTranslation(tipDistance * Vector3D.Forward);
                return new Effector<IMyShipWelder>(welder, tip);
            }

            var grinder = block as IMyShipGrinder;
            if (grinder != null)
            {
                var tipDistance = 0.7 * (grinder.CubeGrid.GridSizeEnum == MyCubeSize.Large ? 2.5 : 1.5);
                var tip = MatrixD.CreateTranslation(tipDistance * Vector3D.Forward);
                return new Effector<IMyShipGrinder>(grinder, tip);
            }

            // TODO: Implement all relevant tip types, like landing gear

            return new Effector<IMyTerminalBlock>(block, MatrixD.Identity);
        }

        private IMyTerminalBlock FindTip(IMyMechanicalConnectionBlock baseBlock, Dictionary<long, HashSet<IMyTerminalBlock>> terminalBlocks)
        {
            HashSet<IMyTerminalBlock> blocks;
            if (terminalBlocks.TryGetValue(baseBlock.Top.CubeGrid.EntityId, out blocks))
            {
                foreach (var block in blocks)
                {
                    if (block is IMyCollector ||
                        block is IMyPistonBase ||
                        block is IMyMotorStator ||
                        block is IMyShipConnector ||
                        block is IMyShipGrinder ||
                        block is IMyShipWelder ||
                        block is IMyLandingGear)
                        return block;

                    if (block?.CustomName?.Contains("Arm Tip") == true)
                        return block;
                }
            }

            var message = $"Broken arm: {baseBlock.CustomName}";
            Util.Log(message);
            throw new Exception(message);
        }

        public double Target(MatrixD target)
        {
            FirstSegment.Init();
            var wm = FirstSegment.Block.WorldMatrix;
            var bestCost = double.PositiveInfinity;
            for (var i = 0; i < Cfg.OptimizationPasses; i++)
            {
                var cost = FirstSegment.Optimize(ref wm, ref target);
                if (cost > bestCost || cost < Shared.GoodEnoughCost)
                    return cost;

                bestCost = cost;
            }

            return bestCost;
        }

        public void Update()
        {
            FirstSegment.Update();
        }

        public void Retract()
        {
            FirstSegment.Retract();
        }

        public void Stop()
        {
            FirstSegment.Stop();
        }
    }

    public class CollisionDetectingRobotArm : RobotArm
    {
        private readonly List<IMySensorBlock> sensors = new List<IMySensorBlock>();

        public CollisionDetectingRobotArm(IMyTerminalBlock @base, Dictionary<long, HashSet<IMyTerminalBlock>> terminalBlocks) : base(@base, terminalBlocks)
        {
            foreach (var block in FirstSegment.IterBlocks())
                RegisterSensors(terminalBlocks, block);
        }

        private void RegisterSensors(Dictionary<long, HashSet<IMyTerminalBlock>> terminalBlocks, IMyTerminalBlock block)
        {
            HashSet<IMyTerminalBlock> blocks;
            if (!terminalBlocks.TryGetValue(block.CubeGrid.EntityId, out blocks))
                return;

            sensors.AddRange(blocks.Where(b => b is IMySensorBlock).Cast<IMySensorBlock>());
        }

        public bool HasCollision => sensors.Any(sensor => sensor.Enabled && sensor.IsActive);
    }

    public enum WelderArmState
    {
        // Arm is stopped
        Stopped,

        // The arm is retracting to its initial position
        Retracting,

        // Moving towards the projected block
        Moving,

        // Building the projected block or welding up the built block
        Welding,

        // Finished welding the block
        Finished,

        // The arm detected a collision on moving towards the projected block's position
        Collided,

        // Failed to build the target block
        Failed,

        // The arm fails to reach the target block
        Unreachable,
    }

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

        public void Reset(int countdown = 0)
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

    public class Subgrid
    {
        private static readonly BoundingBoxI MaxBox = new BoundingBoxI(Vector3I.MinValue, Vector3I.MaxValue);

        public readonly int Index;
        private readonly long projectorEntityId;
        private readonly MultigridProjectorProgrammableBlockAgent mgp;
        private readonly Dictionary<Vector3I, BlockState> BlockStates = new Dictionary<Vector3I, BlockState>();
        private readonly Dictionary<Vector3I, int> LayeredBlockPositions = new Dictionary<Vector3I, int>();
        private readonly List<int> LayerBlockCounts = new List<int>();
        private ulong latestStateHash;
        private MyCubeSize gridSize;
        public bool HasBuilt { get; private set; }
        public bool HasFinished { get; private set; }
        public int LayerIndex => LayerBlockCounts.Count;
        public int WeldedLayer { get; private set; }
        public bool IsValidLayer => WeldedLayer < LayerBlockCounts.Count;
        public int WeldedLayerBlockCount => LayerBlockCounts[WeldedLayer];
        public int BuildableBlockCount => LayeredBlockPositions.Count;

        public Subgrid(long projectorEntityId, MultigridProjectorProgrammableBlockAgent mgp, int index)
        {
            Index = index;
            this.projectorEntityId = projectorEntityId;
            this.mgp = mgp;

            var previewGrid = mgp.GetPreviewGrid(projectorEntityId, index);
            gridSize = previewGrid.GridSizeEnum;
        }

        public bool Update()
        {
            var stateHash = mgp.GetStateHash(projectorEntityId, Index);
            if (stateHash == latestStateHash)
            {
                if (BlockStates.Count > 0)
                    return false;

                if (mgp.IsSubgridComplete(projectorEntityId, Index))
                {
                    HasBuilt = true;
                    HasFinished = true;
                    return false;
                }
            }

            latestStateHash = stateHash;

            BlockStates.Clear();
            mgp.GetBlockStates(BlockStates, projectorEntityId, Index, MaxBox, (int) BlockState.Buildable | (int) BlockState.BeingBuilt);

            // Remove already built blocks (allocates memory, but it is hard to avoid here)
            var blocksToRemove = LayeredBlockPositions.Keys.Where(position => !BlockStates.ContainsKey(position)).ToList();
            foreach (var position in blocksToRemove)
            {
                LayerBlockCounts[LayeredBlockPositions[position]]--;
                LayeredBlockPositions.Remove(position);
            }

            // Store the new layer if any
            if (BlockStates.Count > 0)
            {
                HasBuilt = true;
                HasFinished = false;

                var blockCountBefore = LayeredBlockPositions.Count;

                foreach (var position in BlockStates.Keys)
                    if (!LayeredBlockPositions.ContainsKey(position))
                        LayeredBlockPositions[position] = LayerIndex;

                var layerBlockCount = LayeredBlockPositions.Count - blockCountBefore;
                if (layerBlockCount > 0)
                    LayerBlockCounts.Add(layerBlockCount);
            }
            else if (!HasFinished && mgp.IsSubgridComplete(projectorEntityId, Index))
            {
                HasFinished = true;
            }

            return true;
        }

        public int CountWeldableBlocks(out int lastLayerToWeld)
        {
            // Skip fully welded layers
            while (IsValidLayer && WeldedLayerBlockCount == 0)
                WeldedLayer++;

            // Find weldable layers
            lastLayerToWeld = WeldedLayer;
            var blockCount = LayerBlockCounts[lastLayerToWeld];
            var maxBlocksToWeld = gridSize == MyCubeSize.Large ? Cfg.MaxLargeBlocksToWeld : Cfg.MaxSmallBlocksToWeld;
            while (lastLayerToWeld + 1 < LayerBlockCounts.Count && blockCount + LayerBlockCounts[lastLayerToWeld + 1] <= maxBlocksToWeld)
                blockCount += LayerBlockCounts[lastLayerToWeld++];

            return blockCount;
        }

        public IEnumerable<Vector3I> IterWeldableBlockPositions()
        {
            int lastLayerToWeld;
            CountWeldableBlocks(out lastLayerToWeld);
            foreach (var position in LayeredBlockPositions.Where(p => p.Value <= lastLayerToWeld).Select(p => p.Key))
                yield return position;
        }

        public void Remove(Vector3I position)
        {
            BlockStates.Remove(position);
        }
    }

    public class RotorReverser
    {
        private readonly IMyMotorStator rotor;
        private float latestAngle;
        private int counter;
        private const int Timeout = 18;
        public event Action OnReverse;

        public RotorReverser(IMyMotorStator rotor)
        {
            this.rotor = rotor;
            latestAngle = rotor.Angle;
        }

        public void Update()
        {
            if (rotor == null)
                return;

            var velocity = rotor.TargetVelocityRad;
            if (Math.Abs(velocity) < 1e-3)
            {
                latestAngle = rotor.Angle;
                counter = 0;
                return;
            }

            // Log($"Projector rotor: {velocity:0.000} rad/s");
            // Log($"Latest angle: {latestAngle:000.0} rad");
            // Log($"Rotor angle: {rotor.Angle:000.0} rad");
            if (Math.Abs(rotor.Angle - latestAngle) < Math.Abs(velocity) * 0.1)
            {
                counter++;
                Util.Log($"Projector rotor is stuck {counter} / {Timeout}");
                if (counter >= Timeout)
                {
                    rotor.TargetVelocityRad = -velocity;
                    counter = 0;
                    OnReverse?.Invoke();
                }
            }

            latestAngle = rotor.Angle;
        }
    }

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
            var subgridCount = mgp.GetSubgridCount(projector.EntityId);
            if (!projector.Enabled || !mgp.Available || subgridCount < 1)
            {
                if (subgrids.Count == 0)
                    return;

                lcdDetails?.WriteText("Completed");
                lcdStatus?.WriteText("");

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