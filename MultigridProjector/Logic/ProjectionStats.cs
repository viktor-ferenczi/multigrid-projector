using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using MultigridProjector.Api;
using Sandbox.Definitions;
using Sandbox.Game.Entities.Cube;

namespace MultigridProjector.Logic
{
    public class ProjectionStats
    {
        public int TotalBlocks;
        public int RemainingBlocks;
        public int RemainingArmorBlocks;
        public int BuildableBlocks;

        public readonly Dictionary<MyCubeBlockDefinition, int> RemainingBlocksPerType = new Dictionary<MyCubeBlockDefinition, int>();

        public bool IsBuildCompleted => TotalBlocks > 0 && RemainingBlocks == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            TotalBlocks = 0;
            RemainingBlocks = 0;
            RemainingArmorBlocks = 0;
            BuildableBlocks = 0;

            RemainingBlocksPerType.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RegisterBlock(MySlimBlock slimBlock, BlockState blockState)
        {
            if (blockState == BlockState.FullyBuilt)
                return;

            RemainingBlocks++;

            var fatBlock = slimBlock.FatBlock;
            if (fatBlock == null)
            {
                RemainingArmorBlocks++;
                return;
            }

            var blockDefinition = fatBlock.BlockDefinition;
            RemainingBlocksPerType[blockDefinition] = RemainingBlocksPerType.GetValueOrDefault(blockDefinition) + 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(ProjectionStats other)
        {
            TotalBlocks += other.TotalBlocks;
            RemainingBlocks += other.RemainingBlocks;
            RemainingArmorBlocks += other.RemainingArmorBlocks;
            BuildableBlocks += other.BuildableBlocks;

            foreach (var (blockDefinition, count) in other.RemainingBlocksPerType)
            {
                RemainingBlocksPerType[blockDefinition] = RemainingBlocksPerType.GetValueOrDefault(blockDefinition) + count;
            }
        }
    }
}