using System;
using System.Collections.Generic;
using VRageMath;
using Sandbox.ModAPI.Ingame;

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
}