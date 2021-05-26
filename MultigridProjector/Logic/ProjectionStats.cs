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
        public int TotalArmorBlocks;
        public int RemainingBlocks;
        public int RemainingArmorBlocks;
        public int BuildableBlocks;

        public readonly Dictionary<MyCubeBlockDefinition, int> RemainingBlocksPerType = new Dictionary<MyCubeBlockDefinition, int>();

        public bool Valid => TotalBlocks > 0;
        public bool IsBuildCompleted => Valid && RemainingBlocks == 0;
        public int BuiltBlocks => TotalBlocks - RemainingBlocks;
        public int BuiltArmorBlocks => TotalArmorBlocks - RemainingArmorBlocks;
        public bool BuiltOnlyArmorBlocks => Valid && BuiltBlocks == BuiltArmorBlocks;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            TotalBlocks = 0;
            TotalArmorBlocks = 0;

            BuildableBlocks = 0;

            RemainingBlocks = 0;
            RemainingArmorBlocks = 0;

            RemainingBlocksPerType.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RegisterBlock(MySlimBlock slimBlock, BlockState blockState)
        {
            TotalBlocks++;

            var armor = slimBlock.FatBlock == null;
            if (armor)
                TotalArmorBlocks++;

            switch (blockState)
            {
                case BlockState.Buildable:
                case BlockState.BeingBuilt:
                    BuildableBlocks++;
                    break;

                case BlockState.FullyBuilt:
                    return;
            }

            RemainingBlocks++;

            if (armor)
            {
                RemainingArmorBlocks++;
                return;
            }

            var blockDefinition = slimBlock.FatBlock.BlockDefinition;
            RemainingBlocksPerType[blockDefinition] = RemainingBlocksPerType.GetValueOrDefault(blockDefinition) + 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(ProjectionStats other)
        {
            if (!other.Valid)
                return;

            TotalBlocks += other.TotalBlocks;
            TotalArmorBlocks += other.TotalArmorBlocks;

            BuildableBlocks += other.BuildableBlocks;

            RemainingBlocks += other.RemainingBlocks;
            RemainingArmorBlocks += other.RemainingArmorBlocks;

            foreach (var (blockDefinition, count) in other.RemainingBlocksPerType)
                RemainingBlocksPerType[blockDefinition] = RemainingBlocksPerType.GetValueOrDefault(blockDefinition) + count;
        }
    }
}