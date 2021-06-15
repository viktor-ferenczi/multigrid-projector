using System;
using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI;
using VRage.Game;
using VRageMath;

namespace MultigridProjectorPrograms.RobotArm
{
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
}