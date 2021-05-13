using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using IMyCubeGrid = VRage.Game.ModAPI.Ingame.IMyCubeGrid;

namespace MultigridProjectorPrograms.RobotArm
{
    class Program : MyGridProgram
    {
        #region Arm logic

        public interface ISegment<out T> where T : IMyTerminalBlock
        {
            // Base block of the arm segment or the effector block
            T Block { get; }

            // Transformation from the Block to the effector's tip
            MatrixD Transform { get; }

            // Sets the pose to the current mechanical position as the starting position
            void Init();

            // Changes the pose to move the effector's tip towards the target world position,
            // returns the squared distance (error) of the current pose
            double Optimize(MatrixD wm, Vector3D target);

            // Simulation update step to control the mechanical bases of the arm
            void Update();

            // Retract the arm to its default position
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

            public void Init()
            {
            }

            public double Optimize(MatrixD wm, Vector3D target)
            {
                var pose = Transform * wm;
                // VerifyPose(Block.CustomName, Block.WorldMatrix, wm);
                // VerifyPose("Tip", Transform * Block.WorldMatrix, pose);
                return Vector3D.DistanceSquared(pose.Translation, target);
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
            public readonly ISegment<IMyTerminalBlock> Next;
            public readonly MatrixD TopToNext;
            public double Pose { get; private set; }

            protected Segment(T baseBlock, ISegment<IMyTerminalBlock> next)
            {
                Block = baseBlock;
                Next = next;

                TopToNext = next.Block.WorldMatrix * MatrixD.Invert(Block.Top.WorldMatrix);
            }

            public T Block { get; }

            public MatrixD Transform => GetTipTransform(Pose);

            private MatrixD GetTipTransform(double pose) => Next.Transform * GetSegmentTransform(pose);

            private MatrixD GetSegmentTransform(double pose) => TopToNext * GetBaseToTopTransform(pose);

            private MatrixD GetTipPose(MatrixD wm, double pose) => GetTipTransform(pose) * wm;

            public void Init()
            {
                Next.Init();
                Pose = GetCurrent();
            }

            public double Optimize(MatrixD wm, Vector3D target)
            {
                // VerifyPose(Block.CustomName, Block.WorldMatrix, wm);
                OptimizeThis(wm, target);
                Next.Optimize(GetSegmentTransform(Pose) * wm, target);
                return OptimizeThis(wm, target);
            }

            private double OptimizeThis(MatrixD wm, Vector3D target)
            {
                var error = Vector3D.DistanceSquared(GetTipPose(wm, Pose).Translation, target);

                var step = 1.0;
                while (error > 1e-6 && Math.Abs(step) > 1e-3)
                {
                    var pose1 = ClampPosition(Pose - step);
                    var tipPose1 = GetTipPose(wm, pose1);
                    var error1 = Vector3D.DistanceSquared(tipPose1.Translation, target);

                    var pose2 = ClampPosition(Pose + step);
                    var tipPose2 = GetTipPose(wm, pose2);
                    var error2 = Vector3D.DistanceSquared(tipPose2.Translation, target);

                    if (error1 < error)
                    {
                        Pose = pose1;
                        error = error1;
                    }

                    if (error2 < error)
                    {
                        Pose = pose2;
                        error = error2;
                    }

                    step *= 0.5;
                }

                return error;
            }

            public void Update()
            {
                var current = GetCurrent();
                var velocity = DetermineVelocity(current, this.Pose);
                SetVelocity(velocity);
                Next.Update();
            }

            public void Stop()
            {
                SetVelocity(0);
                Next.Stop();
            }

            public void Retract()
            {
                Pose = 0;
                Next.Retract();
            }

            protected double DetermineVelocity(double current, double target, double speed = 2.0)
            {
                var delta = target - current;
                var sign = Math.Sign(delta);
                var absDelta = delta * sign;
                var velocity = ClampVelocity(speed * absDelta);
                return sign * velocity;
            }

            protected abstract double GetCurrent();
            protected abstract double ClampPosition(double p);
            protected abstract double ClampVelocity(double v);
            protected abstract void SetVelocity(double velocity);
            protected abstract MatrixD GetBaseToTopTransform(double pose);
        }

        public class RotorSegment : Segment<IMyMotorStator>
        {
            private const double MinVelocity = Math.PI / 1440;

            private const double MaxVelocity = Math.PI / 4;

            public RotorSegment(IMyMotorStator block, ISegment<IMyTerminalBlock> next) : base(block, next)
            {
            }

            protected override double GetCurrent() => Block.Angle;

            protected override double ClampPosition(double pos) => Math.Max(Block.LowerLimitRad, Math.Min(Block.UpperLimitRad, pos));

            protected override double ClampVelocity(double v) => v < MinVelocity ? 0 : Math.Min(MaxVelocity, v);

            protected override void SetVelocity(double velocity) => Block.TargetVelocityRad = (float) velocity;

            protected override MatrixD GetBaseToTopTransform(double pose) => MatrixD.CreateTranslation(Vector3D.Up * (0.2 + Block.Displacement)) * MatrixD.CreateFromAxisAngle(Vector3D.Down, pose);
        }

        public class HingeSegment : Segment<IMyMotorStator>
        {
            private const double MinVelocity = Math.PI / 1440;
            private const double MaxVelocity = Math.PI / 4;

            public HingeSegment(IMyMotorStator block, ISegment<IMyTerminalBlock> next) : base(block, next)
            {
            }

            protected override double GetCurrent() => Block.Angle;

            protected override double ClampPosition(double pos) => Math.Max(Block.LowerLimitRad, Math.Min(Block.UpperLimitRad, pos));

            protected override double ClampVelocity(double v) => v < MinVelocity ? 0 : Math.Min(MaxVelocity, v);

            protected override void SetVelocity(double velocity) => Block.TargetVelocityRad = (float) velocity;

            protected override MatrixD GetBaseToTopTransform(double pose) => MatrixD.CreateFromAxisAngle(Vector3D.Down, pose);
        }

        public class PistonSegment : Segment<IMyPistonBase>
        {
            private const double MinVelocity = 0.0006;
            private const double MaxVelocity = 5;

            public PistonSegment(IMyPistonBase block, ISegment<IMyTerminalBlock> next) : base(block, next)
            {
            }

            protected override double GetCurrent() => Block.CurrentPosition;

            protected override double ClampPosition(double pos) => Math.Max(Block.LowestPosition, Math.Min(Block.HighestPosition, pos));

            protected override double ClampVelocity(double v) => v < MinVelocity ? 0 : Math.Min(MaxVelocity, v);

            protected override void SetVelocity(double velocity) => Block.Velocity = (float) velocity;

            protected override MatrixD GetBaseToTopTransform(double pose) => MatrixD.CreateTranslation(Vector3D.Up * (1.4 + pose));
        }

        public class RobotArm
        {
            public readonly ISegment<IMyTerminalBlock> FirstSegment;
            public readonly List<IMyMechanicalConnectionBlock> BaseBlocks = new List<IMyMechanicalConnectionBlock>();
            public IMyTerminalBlock EffectorBlock;

            public RobotArm(IMyTerminalBlock @base, Dictionary<long, HashSet<IMyTerminalBlock>> terminalBlocks)
            {
                FirstSegment = Discover(@base, terminalBlocks);
                FirstSegment.Init();
            }

            public MatrixD EffectorPose => FirstSegment.Transform * FirstSegment.Block.WorldMatrix;

            private ISegment<IMyTerminalBlock> Discover(IMyTerminalBlock block, Dictionary<long, HashSet<IMyTerminalBlock>> terminalBlocks)
            {
                var pistonBase = block as IMyPistonBase;
                if (pistonBase != null)
                {
                    BaseBlocks.Add(pistonBase);
                    var tip = FindTip(pistonBase.Top, terminalBlocks);
                    var next = Discover(tip, terminalBlocks);
                    return new PistonSegment(pistonBase, next);
                }

                var rotorBase = block as IMyMotorStator;
                if (rotorBase != null)
                {
                    BaseBlocks.Add(rotorBase);
                    var tip = FindTip(rotorBase.Top, terminalBlocks);
                    var next = Discover(tip, terminalBlocks);
                    if (rotorBase.BlockDefinition.SubtypeName.EndsWith("Hinge"))
                        return new HingeSegment(rotorBase, next);
                    return new RotorSegment(rotorBase, next);
                }

                EffectorBlock = block;

                var welder = block as IMyShipWelder;
                if (welder != null)
                {
                    var tip = MatrixD.CreateTranslation(2 * welder.CubeGrid.GridSize * Vector3D.Forward);
                    return new Effector<IMyShipWelder>(welder, tip);
                }

                var grinder = block as IMyShipGrinder;
                if (grinder != null)
                {
                    var tip = MatrixD.CreateTranslation(2 * grinder.CubeGrid.GridSize * Vector3D.Forward);
                    return new Effector<IMyShipGrinder>(grinder, tip);
                }

                // TODO: Implement all relevant tip types, like landing gear

                return new Effector<IMyTerminalBlock>(block, MatrixD.Identity);
            }

            private IMyTerminalBlock FindTip(IMyAttachableTopBlock topBlock, Dictionary<long, HashSet<IMyTerminalBlock>> terminalBlocks)
            {
                HashSet<IMyTerminalBlock> blocks;
                if (!terminalBlocks.TryGetValue(topBlock.CubeGrid.EntityId, out blocks))
                    return null;

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

                    if (block.DisplayName.Contains("Arm Tip"))
                        return block;
                }

                return null;
            }

            public virtual double Target(Vector3D target)
            {
                return FirstSegment.Optimize(FirstSegment.Block.WorldMatrix, target);
            }

            public virtual void Update()
            {
                FirstSegment.Update();
            }

            public virtual void Retract()
            {
                FirstSegment.Retract();
            }

            public virtual void Stop()
            {
                FirstSegment.Stop();
            }
        }

        public class CollisionSafeRobotArm : RobotArm
        {
            private readonly List<IMySensorBlock> sensors = new List<IMySensorBlock>();
            private int retracting;

            public CollisionSafeRobotArm(IMyTerminalBlock @base, Dictionary<long, HashSet<IMyTerminalBlock>> terminalBlocks) : base(@base, terminalBlocks)
            {
                foreach (var block in BaseBlocks)
                    RegisterSensors(terminalBlocks, block);

                RegisterSensors(terminalBlocks, EffectorBlock);
            }

            private void RegisterSensors(Dictionary<long, HashSet<IMyTerminalBlock>> terminalBlocks, IMyTerminalBlock block)
            {
                HashSet<IMyTerminalBlock> blocks;
                if (!terminalBlocks.TryGetValue(block.CubeGrid.EntityId, out blocks))
                    return;

                sensors.AddRange(blocks.Where(b => b is IMySensorBlock).Cast<IMySensorBlock>());
            }

            public bool HasCollision => sensors.Any(sensor => sensor.Enabled && sensor.IsActive);
            public bool IsRetracting => retracting > 0;

            public override void Update()
            {
                if (HasCollision)
                {
                    retracting = 10;
                    Retract();
                }

                if (IsRetracting)
                    retracting--;

                base.Update();
            }

            public override double Target(Vector3D target)
            {
                if (IsRetracting)
                    return -1;

                return base.Target(target);
            }
        }

        public class WelderArm : CollisionSafeRobotArm
        {
            public WelderArm(IMyTerminalBlock @base, Dictionary<long, HashSet<IMyTerminalBlock>> terminalBlocks) : base(@base, terminalBlocks)
            {
            }

            private static readonly BoundingBoxI MaxBox = new BoundingBoxI(Vector3I.MinValue, Vector3I.MaxValue);
            private static readonly Dictionary<Vector3I, BlockState> BlockStates = new Dictionary<Vector3I, BlockState>();
            public static IEnumerable<Vector3D> IterBuildableBlocks(MultigridProjectorProgrammableBlockAgent mgp, long projectorEntityId)
            {
                var subgridCount = mgp.GetSubgridCount(projectorEntityId);
                for (var subgridIndex = 0; subgridIndex < subgridCount; subgridIndex++)
                {
                    BlockStates.Clear();
                    if (!mgp.GetBlockStates(BlockStates, projectorEntityId, subgridIndex, MaxBox, (int) BlockState.Buildable))
                        continue;

                    if (BlockStates.Count == 0)
                        continue;

                    var previewGrid = mgp.GetPreviewGrid(projectorEntityId, subgridIndex);
                    if (previewGrid == null)
                        continue;

                    foreach (var position in BlockStates.Keys)
                        yield return previewGrid.GridIntegerToWorld(position);
                }
            }

            public bool TargetNearest(IEnumerable<Vector3D> buildableBlockPositions)
            {
                var tipPosition = EffectorPose.Translation;
                var nearestTarget = Vector3D.Zero;
                var nearestSquareDistance = double.PositiveInfinity;
                var foundTarget = false;

                foreach (var blockPosition in buildableBlockPositions)
                {
                    var squareDistance = Vector3D.DistanceSquared(blockPosition, tipPosition);
                    if (squareDistance >= nearestSquareDistance)
                        continue;

                    nearestSquareDistance = squareDistance;
                    nearestTarget = blockPosition;
                    foundTarget = true;
                }

                if (foundTarget)
                {
                    var error = Target(nearestTarget);
                    Log($"Target: {Format(nearestTarget)}");
                    Log($"Squared error: {error:0.000000}");

                    if (error > 100)
                    {
                        Log("Out of reach");
                        Retract();
                    }
                }
                else
                {
                    Log("No target");
                    Retract();
                }

                return foundTarget;
            }
        }

        #endregion

        #region Program

        private readonly IMyTextPanel lcd;
        private readonly IMyProjector projector;
        private readonly List<WelderArm> arms = new List<WelderArm>();
        private readonly MultigridProjectorProgrammableBlockAgent mgp;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;

            var pbSurface = Me.GetSurface(0);
            pbSurface.ContentType = ContentType.TEXT_AND_IMAGE;
            pbSurface.Alignment = TextAlignment.CENTER;
            pbSurface.FontColor = Color.DarkGreen;
            pbSurface.FontSize = 3.2f;
            pbSurface.WriteText("Robotic Arm\r\nController");

            lcd = GridTerminalSystem.GetBlockWithName("LCD") as IMyTextPanel;
            if (lcd != null)
            {
                lcd.ContentType = ContentType.TEXT_AND_IMAGE;
                lcd.Alignment = TextAlignment.LEFT;
                lcd.FontColor = Color.DarkGreen;
                lcd.FontSize = 1f;
                lcd.WriteText("");
            }

            projector = GridTerminalSystem.GetBlockWithName("Projector") as IMyProjector;
            mgp = new MultigridProjectorProgrammableBlockAgent(Me);

            var terminalBlocks = FindAllTerminalBlocks();

            arms.Add(new WelderArm(GridTerminalSystem.GetBlockWithName("Arm 1 Rotor") as IMyMotorStator, terminalBlocks));
            arms.Add(new WelderArm(GridTerminalSystem.GetBlockWithName("Arm 2 Rotor") as IMyMotorStator, terminalBlocks));
        }

        private Dictionary<long, HashSet<IMyTerminalBlock>> FindAllTerminalBlocks()
        {
            var terminalBlocks = new Dictionary<long, HashSet<IMyTerminalBlock>>();
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyFunctionalBlock>(blocks);
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

            return terminalBlocks;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            ClearLog();
            try
            {
                if (((int) updateSource & (int) UpdateType.Update10) > 0)
                    Update10();
            }
            finally
            {
                ShowLog();
            }
        }

        public void Save()
        {
            foreach(var arm in arms)
                arm.Stop();
        }

        private void Update10()
        {
            var buildableBlockPositions = WelderArm.IterBuildableBlocks(mgp, projector.EntityId).ToList();
            foreach (var arm in arms)
            {
                arm.TargetNearest(buildableBlockPositions);
                arm.Update();
            }
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

        private static MatrixD GetRotorTransform(double pose, double displacement)
        {
            return MatrixD.CreateTranslation(Vector3D.Up * (0.2 + displacement)) * MatrixD.CreateFromAxisAngle(Vector3D.Down, pose);
        }

        private static MatrixD GetHingeTransform(double pose)
        {
            return MatrixD.CreateFromAxisAngle(Vector3D.Down, pose);
        }

        private static MatrixD GetPistonTransform(double pose)
        {
            return MatrixD.CreateTranslation(Vector3D.Up * (1.4 + pose));
        }

        #endregion

        #region Utility

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
            lcd?.WriteText(LogBuilder.ToString());
        }

        private static string Format(Vector3D v)
        {
            return $"[{v.X:0.000}, {v.Y:0.000}, {v.Z:0.000}]";
        }

        private static string Format(MatrixD m)
        {
            return $"\r\n  T: {Format(m.Translation)}\r\n  F: {Format(m.Forward)}\r\n  U: {Format(m.Up)}\r\n  S: {Format(m.Scale)}";
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