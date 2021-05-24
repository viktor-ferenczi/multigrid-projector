using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI;
using VRage.Game;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;

namespace MultigridProjectorPrograms.RobotArm
{
    class Program : MyGridProgram
    {
        #region Configuration

        // Name of the projector to receive the projection information from via MGP's PB API (required)
        private const string ProjectorName = "Shipyard Projector";

        // Name of the rotor rotating the projector (optional),
        // the program makes sure to reverse this rotor if it becomes stuck due to an arm in the way
        private const string ProjectorRotorName = "Shipyard Projector Rotor";

        // Name of the block group containing the first mechanical bases of each arm (required)
        private const string WelderArmsGroupName = "Welder Arms";

        // Name of the block group containing LCD panels to show completion statistics and debug information (optional)
        // Names should contains: Timer, Details, Status, Log
        private const string TextPanelsGroupName = "Shipyard Text Panels";

        // Weight of the direction component of the optimized effector pose in the cost, higher value prefers more precise effector direction
        private const double DirectionCostWeight = 0.5; // Turn the welder arm towards the preview grid's center

        // Weight of the roll component of the optimized effector pose, higher value prefers more precise roll control
        private const double RollCostWeight = 0.0; // Welders don't care about roll, therefore no need to optimize for that

        // L2 regularization of mechanical base activations, higher value prefers simpler arm poses closer to the initial activations
        private const double ActivationRegularization = 2.0;

        // Maximum distance from the effector's tip to weld blocks, determined by the welder,
        // outside this distance the welder is turned off to prevent building blocks out-of-order
        // which may block the arm out from regions not fully welded up yet (prevents most of the blind spots)
        private const double MaxWeldingDistance = 3.5; // [m]

        // Maximum number of full forward-backward optimization passes along the arm segments each tick
        private const int OptimizationPasses = 1;

        // Maximum time to retract the arm after a collision on moving the arm to the target block or during welding
        private const int MaxRetractionTimeAfterCollision = 3;  // [Ticks] (1/6 seconds, due to Update10)

        // Maximum time to retract the arm after a block proved to be unreachable after the arm tried to reach it
        private const int MaxRetractionTimeAfterUnreachable = 9;  // [Ticks] (1/6 seconds, due to Update10)

        // If the arm moves the wrong direction then consider the target as unreachable
        private const double MovingCostIncreaseLimit = 10.0;

        // Timeout moving the arm near the target block, counted until welding range
        private const int MovingTimeout = 18; // [Ticks] (1/6 seconds, due to Update10)

        // Timeout for welding a block
        private const int WeldingTimeout = 6; // [Ticks] (1/6 seconds, due to Update10)

        // Resets the arm after this many subsequent failed welding attempts
        private const int ResetArmAfterFailedWeldingAttempts = 5;

        // Minimum meaningful activation steps during optimization
        private const double MinActivationStepPiston = 0.001; // [m]
        private const double MinActivationStepRotor = 0.001; // [rad]
        private const double MinActivationStepHinge = 0.001; // [rad]

        // Maximum number of blocks to weld at the same time
        private const int MaxLargeBlocksToWeld = 7;
        private const int MaxSmallBlocksToWeld = 70;

        #endregion

        #region Arm logic

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
                return CalculatePoseCost(ref target, ref pose);
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

        private const double MaxWeldingDistanceSquared = MaxWeldingDistance * MaxWeldingDistance; // [m*m]

        // Maximum acceptable optimized cost for the arm to start targeting a block
        private static readonly double MaxAcceptableCost = MaxWeldingDistanceSquared + 2 * (DirectionCostWeight + RollCostWeight) + ActivationRegularization;

        // Stops optimizing the pose before the loop count limit if this cost level is reached
        private static readonly double GoodEnoughCost = Math.Pow(0.033 * MaxWeldingDistance, 2);

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
                activationWeight = ActivationRegularization / segmentCount;

                double v;
                if (double.TryParse((block.CustomData ?? "").Trim(), out v))
                    InitialActivation = ClampPosition(v);
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
                var cost = CalculatePoseCost(ref target, ref tipPose) + (ActivationCost(optimizedActivation) + nextSumActivationCosts) * activationWeight;

                var step = ActivationRange * 0.5;
                while (cost > GoodEnoughCost && Math.Abs(step) > MinActivationStep)
                {
                    var activation1 = ClampPosition(optimizedActivation - step);
                    var pose1 = CalculatePose(ref wm, activation1, ref nextTransform);
                    var cost1 = CalculatePoseCost(ref target, ref pose1) + (ActivationCost(activation1) + nextSumActivationCosts) * activationWeight;

                    var activation2 = ClampPosition(optimizedActivation + step);
                    var pose2 = CalculatePose(ref wm, activation2, ref nextTransform);
                    var cost2 = CalculatePoseCost(ref target, ref pose2) + (ActivationCost(activation2) + nextSumActivationCosts) * activationWeight;

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
                MinActivationStep = MinActivationStepRotor;
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
                MinActivationStep = MinActivationStepHinge;
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
                MinActivationStep = MinActivationStepPiston;
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
                    var tipDistance = welder.CubeGrid.GridSizeEnum == MyCubeSize.Large ? 1.5 * 2.5 : 1.5 * 1.5;
                    var tip = MatrixD.CreateTranslation(tipDistance * Vector3D.Forward);
                    return new Effector<IMyShipWelder>(welder, tip);
                }

                var grinder = block as IMyShipGrinder;
                if (grinder != null)
                {
                    var tipDistance = grinder.CubeGrid.GridSizeEnum == MyCubeSize.Large ? 1.5 * 2.5 : 1.5 * 1.5;
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
                Log(message);
                throw new Exception(message);
            }

            public double Target(MatrixD target)
            {
                FirstSegment.Init();
                var wm = FirstSegment.Block.WorldMatrix;
                var bestCost = double.PositiveInfinity;
                for (var i = 0; i < OptimizationPasses; i++)
                {
                    var cost = FirstSegment.Optimize(ref wm, ref target);
                    if (cost > bestCost || cost < GoodEnoughCost)
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

            // Timer to detect non-progressing move,
            // reset to zero each time the arm starts to move to a target block,
            // incremented on moving the arm until welding starts,
            // finishes moving on reaching MovingTimeout
            private int MovingTimer;

            // Timer to detect non-progressing welding,
            // reset to zero each time the welding progresses,
            // incremented during the welding process,
            // finishes welding on reaching WeldingTimeout
            private int WeldingTimer;

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

            public void Reset(int countdown=0)
            {
                State = WelderArmState.Retracting;
                TargetLocation = new BlockLocation();
                MovingTimer = 0;
                WeldingTimer = 0;
                FailureCount = 0;
                countdownToStopRetracting = countdown;
            }

            public void Target(BlockLocation location)
            {
                State = WelderArmState.Moving;
                TargetLocation = location;
                MovingTimer = 0;
                WeldingTimer = 0;
            }

            private void OnStateChanged(WelderArmState oldState)
            {
                welder.Enabled = State == WelderArmState.Welding;

                switch (State)
                {
                    case WelderArmState.Stopped:
                        Stop();
                        break;

                    case WelderArmState.Moving:
                        bestCostForThisTarget = double.PositiveInfinity;
                        break;

                    case WelderArmState.Collided:
                    case WelderArmState.Retracting:
                    case WelderArmState.Unreachable:
                        Retract();
                        Cost = 0;
                        break;
                }
            }

            public new void Update()
            {
                switch (State)
                {
                    case WelderArmState.Retracting:
                        if (FirstSegment.IsRetracted || countdownToStopRetracting > 0 && --countdownToStopRetracting == 0)
                        {
                            State = WelderArmState.Stopped;
                            MovingTimer = 0;
                        }
                        else if (!FirstSegment.IsMoving)
                        {
                            MovingTimer++;
                            if (MovingTimer > 6)
                                Log($"Retracting arm is stuck for {MovingTimer / 6.0:0.0}s");
                        }
                        else
                        {
                            MovingTimer = 0;
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
                var gridCenter = previewGrid.WorldAABB.Center;
                var direction = Vector3D.Normalize(gridCenter - previewBlockCoordinates);
                var target = MatrixD.CreateFromDir(direction, armBaseWm.Up);
                target.Translation += previewBlockCoordinates;
                Cost = Target(target);
                if (Cost >= MaxAcceptableCost || Cost > bestCostForThisTarget + MovingCostIncreaseLimit)
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

                var welding = Vector3D.DistanceSquared(welder.WorldMatrix.Translation, previewBlockCoordinates) <= MaxWeldingDistanceSquared;
                if (!welding)
                {
                    State = WelderArmState.Moving;

                    if (++MovingTimer >= MovingTimeout)
                        State = WelderArmState.Unreachable;

                    return;
                }

                if (++WeldingTimer >= WeldingTimeout)
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
                var maxBlocksToWeld = gridSize == MyCubeSize.Large ? MaxLargeBlocksToWeld : MaxSmallBlocksToWeld;
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
                    Log($"Projector rotor is stuck {counter} / {Timeout}");
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
                gridTerminalSystem.GetBlockGroupWithName(WelderArmsGroupName)?.GetBlocksOfType(armBases);
                if (armBases.Count == 0)
                {
                    Log("Add all arm base blocks to the Welder Arms group!");
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

            public void Update()
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

                ShowStatus();

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
                        if (++arm.FailureCount >= ResetArmAfterFailedWeldingAttempts)
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
                        arm.Reset(MaxRetractionTimeAfterCollision);
                        break;

                    case WelderArmState.Unreachable:
                        arm.Reset(MaxRetractionTimeAfterUnreachable);
                        break;
                }
            }

            private void AssignNextBlock(WelderArm arm, Vector3D referencePosition)
            {
                arm.Reset();

                Subgrid subgridToWeld = null;
                var nearestDistanceSquared = double.PositiveInfinity;
                var positionToWeld = Vector3I.Zero;

                foreach (var subgrid in subgrids)
                {
                    if (!subgrid.HasBuilt || subgrid.HasFinished)
                        continue;

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
                }

                if (subgridToWeld == null)
                    return;

                var location = new BlockLocation(subgridToWeld.Index, positionToWeld);
                arm.Target(location);
                subgridToWeld.Remove(positionToWeld);
            }

            private void ShowStatus()
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
                    var positionText = (active ? Format(arm.TargetLocation.Position) : "").PadRight(14);
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
                    var weldedLayerText = $"{subgrid.WeldedLayer}-{lastLayerToWeld}".PadLeft(7);
                    sb.Append($"{subgridIndexText} {blockCountText} {layerCountText} {weldedLayerText} {layerBlockCountText}\r\n");
                }

                lcdStatus.WriteText(sb.ToString());
            }
        }

        #endregion

        #region Program

        private static IMyTextPanel lcdTimer;
        private static IMyTextPanel lcdDetails;
        private static IMyTextPanel lcdStatus;
        private static IMyTextPanel lcdLog;
        private readonly Shipyard shipyard;
        private readonly RotorReverser rotorReverser;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;

            PrepareDisplay();
            FindTextPanels();

            ClearLog();
            try
            {
                try
                {
                    var mgp = new MultigridProjectorProgrammableBlockAgent(Me);
                    var projector = GridTerminalSystem.GetBlockWithName(ProjectorName) as IMyProjector;
                    shipyard = new Shipyard(GridTerminalSystem, projector, mgp);

                    var projectorRotor = GridTerminalSystem.GetBlockWithName(ProjectorRotorName) as IMyMotorStator;
                    rotorReverser = new RotorReverser(projectorRotor);
                    rotorReverser.OnReverse += shipyard.RetractAll;
                }
                catch (Exception e)
                {
                    Log(e.ToString());
                    throw;
                }
            }
            finally
            {
                ShowLog();
            }
        }

        private void PrepareDisplay()
        {
            var pbSurface = Me.GetSurface(0);
            pbSurface.ContentType = ContentType.TEXT_AND_IMAGE;
            pbSurface.Alignment = TextAlignment.CENTER;
            pbSurface.FontColor = Color.DarkGreen;
            pbSurface.Font = "DEBUG";
            pbSurface.FontSize = 3f;
            pbSurface.WriteText("Robotic Arm\r\nController");
        }

        private void FindTextPanels()
        {
            var lcdGroup = GridTerminalSystem.GetBlockGroupWithName(TextPanelsGroupName);
            var textPanels = new List<IMyTextPanel>();

            lcdGroup.GetBlocksOfType(textPanels);

            foreach (var textPanel in textPanels)
            {
                textPanel.ContentType = ContentType.TEXT_AND_IMAGE;
                textPanel.Alignment = TextAlignment.LEFT;
                textPanel.FontColor = Color.Cyan;
                textPanel.Font = "DEBUG";
                textPanel.FontSize = 1.2f;
                textPanel.WriteText("");
            }

            lcdTimer = textPanels.FirstOrDefault(p => p.CustomName.Contains("Timer"));
            lcdDetails = textPanels.FirstOrDefault(p => p.CustomName.Contains("Details"));
            lcdStatus = textPanels.FirstOrDefault(p => p.CustomName.Contains("Status"));
            lcdLog = textPanels.FirstOrDefault(p => p.CustomName.Contains("Log"));

            if (lcdTimer != null)
            {
                lcdTimer.Font = "Monospace";
                lcdTimer.FontSize = 2.8f;
                lcdTimer.Alignment = TextAlignment.CENTER;
                lcdTimer.TextPadding = 10;
            }

            if (lcdStatus != null)
            {
                lcdStatus.Font = "Monospace";
                lcdStatus.FontSize = 0.8f;
                lcdStatus.TextPadding = 0;
            }
        }

        public void Main(string argument, UpdateType updateSource)
        {
            ClearLog();
            try
            {
                try
                {
                    if (((int) updateSource & (int) UpdateType.Update10) > 0)
                    {
                        shipyard.Update();
                        rotorReverser.Update();
                        // MechanicalConnectorTests();
                    }
                }
                catch (Exception e)
                {
                    Log(e.ToString());
                    shipyard.Stop();
                    throw;
                }
            }
            finally
            {
                ShowLog();
            }
        }

        public void Save()
        {
            shipyard.Stop();
        }

        #endregion

        #region Tests

        private void MechanicalConnectorTests()
        {
            var rotor = GridTerminalSystem.GetBlockWithName("Rotor Test") as IMyMotorStator;
            var hinge = GridTerminalSystem.GetBlockWithName("Hinge Test") as IMyMotorStator;
            var piston = GridTerminalSystem.GetBlockWithName("Piston Test") as IMyPistonBase;

            VerifyPose(rotor.CustomName, rotor.Top.WorldMatrix, GetRotorTransform(rotor.Angle, rotor.Displacement) * rotor.WorldMatrix);
            VerifyPose(hinge.CustomName, hinge.Top.WorldMatrix, GetHingeTransform(hinge.Angle) * hinge.WorldMatrix);
            VerifyPose(piston.CustomName, piston.Top.WorldMatrix, GetPistonTransform(piston.CurrentPosition) * piston.WorldMatrix);
        }

        private static void VerifyPose(string name, MatrixD physical, MatrixD calculated)
        {
            var deltaTranslation = calculated.Translation - physical.Translation;
            var deltaRight = calculated.Right - physical.Right;
            var deltaUp = calculated.Up - physical.Up;
            Log($"{name}:");
            Log($"  dTr {Format(deltaTranslation)}");
            Log($"  dRi {Format(deltaRight)}");
            Log($"  dUp {Format(deltaUp)}");
        }

        private static MatrixD GetRotorTransform(double activation, double displacement)
        {
            return MatrixD.CreateTranslation(Vector3D.Up * (0.2 + displacement)) * MatrixD.CreateFromAxisAngle(Vector3D.Down, activation);
        }

        private static MatrixD GetHingeTransform(double activation)
        {
            return MatrixD.CreateFromAxisAngle(Vector3D.Down, activation);
        }

        private static MatrixD GetPistonTransform(double activation)
        {
            return MatrixD.CreateTranslation(Vector3D.Up * (1.4 + activation));
        }

        #endregion

        #region Utilities

// FIXME: Make logging non-static at the end

        private static readonly StringBuilder LogBuilder = new StringBuilder();

        private static void ClearLog()
        {
            LogBuilder.Clear();
        }

        private static void Log(string message)
        {
            LogBuilder.Append($"{message}\r\n");
        }

        private void ShowLog()
        {
            lcdLog?.WriteText(LogBuilder.Length == 0 ? "OK" : LogBuilder.ToString());
        }

        private static string Format(Vector3I v)
        {
            return $"[{v.X}, {v.Y}, {v.Z}]";
        }

        private static string Format(Vector3D v)
        {
            return $"[{v.X:0.000}, {v.Y:0.000}, {v.Z:0.000}]";
        }

        private static string Format(MatrixD m)
        {
            return $"\r\n  T: {Format(m.Translation)}\r\n  F: {Format(m.Forward)}\r\n  U: {Format(m.Up)}\r\n  S: {Format(m.Scale)}";
        }

        private static double CalculatePoseCost(ref MatrixD target, ref MatrixD pose)
        {
            return Vector3D.DistanceSquared(pose.Translation, target.Translation) +
                   Vector3D.DistanceSquared(pose.Forward, target.Forward) * DirectionCostWeight +
                   Vector3D.DistanceSquared(pose.Up, target.Up) * RollCostWeight;
        }

        #endregion

        #region MGP API Agent

        public struct BlockLocation
        {
            public readonly int GridIndex;
            public readonly Vector3I Position;

            public BlockLocation(int gridIndex, Vector3I position)
            {
                GridIndex = gridIndex;
                Position = position;
            }

            public override int GetHashCode()
            {
                return (((((GridIndex * 397) ^ Position.X) * 397) ^ Position.Y) * 397) ^ Position.Z;
            }
        }

        public enum BlockState
        {
            // Block state is still unknown, not determined by the background worker yet
            Unknown = 0,

            // The block is not buildable due to lack of connectivity or colliding objects
            NotBuildable = 1,

            // The block has not built yet and ready to be built (side connections are good and no colliding objects)
            Buildable = 2,

            // The block is being built, but not to the level required by the blueprint (needs more welding)
            BeingBuilt = 4,

            // The block has been built to the level required by the blueprint or more
            FullyBuilt = 8,

            // There is mismatching block in the place of the projected block with a different definition than required by the blueprint
            Mismatch = 128
        }

        public class MultigridProjectorProgrammableBlockAgent
        {
            private const string CompatibleMajorVersion = "0.";

            private readonly Delegate[] api;

            public bool Available { get; }
            public string Version { get; }

            // Returns the number of subgrids in the active projection, returns zero if there is no projection
            public int GetSubgridCount(long projectorId)
            {
                if (!Available)
                    return 0;

                var fn = (Func<long, int>) api[1];
                return fn(projectorId);
            }

            // Returns the preview grid (aka hologram) for the given subgrid, it always exists if the projection is active, even if fully built
            public IMyCubeGrid GetPreviewGrid(long projectorId, int subgridIndex)
            {
                if (!Available)
                    return null;

                var fn = (Func<long, int, IMyCubeGrid>) api[2];
                return fn(projectorId, subgridIndex);
            }

            // Returns the already built grid for the given subgrid if there is any, null if not built yet (the first subgrid is always built)
            public IMyCubeGrid GetBuiltGrid(long projectorId, int subgridIndex)
            {
                if (!Available)
                    return null;

                var fn = (Func<long, int, IMyCubeGrid>) api[3];
                return fn(projectorId, subgridIndex);
            }

            // Returns the build state of a single projected block
            public BlockState GetBlockState(long projectorId, int subgridIndex, Vector3I position)
            {
                if (!Available)
                    return BlockState.Unknown;

                var fn = (Func<long, int, Vector3I, int>) api[4];
                return (BlockState) fn(projectorId, subgridIndex, position);
            }

            // Writes the build state of the preview blocks into blockStates in a given subgrid and volume of cubes with the given state mask
            public bool GetBlockStates(Dictionary<Vector3I, BlockState> blockStates, long projectorId, int subgridIndex, BoundingBoxI box, int mask)
            {
                if (!Available)
                    return false;

                var blockIntStates = new Dictionary<Vector3I, int>();
                var fn = (Func<Dictionary<Vector3I, int>, long, int, BoundingBoxI, int, bool>) api[5];
                if (!fn(blockIntStates, projectorId, subgridIndex, box, mask))
                    return false;

                foreach (var pair in blockIntStates)
                    blockStates[pair.Key] = (BlockState) pair.Value;

                return true;
            }

            // Returns the base connections of the blueprint: base position => top subgrid and top part position (only those connected in the blueprint)
            public Dictionary<Vector3I, BlockLocation> GetBaseConnections(long projectorId, int subgridIndex)
            {
                if (!Available)
                    return null;

                var basePositions = new List<Vector3I>();
                var gridIndices = new List<int>();
                var topPositions = new List<Vector3I>();
                var fn = (Func<long, int, List<Vector3I>, List<int>, List<Vector3I>, bool>) api[6];
                if (!fn(projectorId, subgridIndex, basePositions, gridIndices, topPositions))
                    return null;

                var baseConnections = new Dictionary<Vector3I, BlockLocation>();
                for (var i = 0; i < basePositions.Count; i++)
                    baseConnections[basePositions[i]] = new BlockLocation(gridIndices[i], topPositions[i]);

                return baseConnections;
            }

            // Returns the top connections of the blueprint: top position => base subgrid and base part position (only those connected in the blueprint)
            public Dictionary<Vector3I, BlockLocation> GetTopConnections(long projectorId, int subgridIndex)
            {
                if (!Available)
                    return null;

                var topPositions = new List<Vector3I>();
                var gridIndices = new List<int>();
                var basePositions = new List<Vector3I>();
                var fn = (Func<long, int, List<Vector3I>, List<int>, List<Vector3I>, bool>) api[7];
                if (!fn(projectorId, subgridIndex, topPositions, gridIndices, basePositions))
                    return null;

                var topConnections = new Dictionary<Vector3I, BlockLocation>();
                for (var i = 0; i < topPositions.Count; i++)
                    topConnections[topPositions[i]] = new BlockLocation(gridIndices[i], basePositions[i]);

                return topConnections;
            }

            // Returns the grid scan sequence number, incremented each time the preview grids/blocks change in any way in any of the subgrids.
            // Reset to zero on loading a blueprint or clearing (or turning OFF) the projector.
            public long GetScanNumber(long projectorId)
            {
                if (!Available)
                    return 0;

                var fn = (Func<long, long>) api[8];
                return fn(projectorId);
            }

            // Returns YAML representation of all information available via API functions.
            // Returns an empty string if the grid scan sequence number is zero (see above).
            // The format may change in incompatible ways only on major version increases.
            // New fields may be introduced without notice with any MGP release as the API changes.
            public string GetYaml(long projectorId)
            {
                if (!Available)
                    return "";

                var fn = (Func<long, string>) api[9];
                return fn(projectorId);
            }

            // Returns the hash of all block states of a subgrid, updated when the scan number increases.
            // Changes only if there is any block state change. Can be used to monitor for state changes efficiently.
            // Reset to zero on loading a blueprint or clearing (or turning OFF) the projector.
            public ulong GetStateHash(long projectorId, int subgridIndex)
            {
                if (!Available)
                    return 0;

                var fn = (Func<long, int, ulong>) api[10];
                return fn(projectorId, subgridIndex);
            }

            // Returns true if the subgrid is fully built (completed)
            public bool IsSubgridComplete(long projectorId, int subgridIndex)
            {
                if (!Available)
                    return false;

                var fn = (Func<long, int, bool>) api[11];
                return fn(projectorId, subgridIndex);
            }

            public MultigridProjectorProgrammableBlockAgent(IMyProgrammableBlock programmableBlock)
            {
                api = programmableBlock.GetProperty("MgpApi")?.As<Delegate[]>().GetValue(programmableBlock);
                if (api == null || api.Length < 12)
                    return;

                var getVersion = api[0] as Func<string>;
                if (getVersion == null)
                    return;

                Version = getVersion();
                if (Version == null || !Version.StartsWith(CompatibleMajorVersion))
                    return;

                Available = true;
            }
        }

        #endregion
    }
}