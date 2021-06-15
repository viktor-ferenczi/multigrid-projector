using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI.Ingame;

namespace MultigridProjectorPrograms.RobotArm
{
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
}