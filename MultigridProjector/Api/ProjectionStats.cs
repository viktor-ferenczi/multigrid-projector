using System.Collections.Generic;
using VRage.Game;

namespace MultigridProjector.Api
{
    public class ProjectionStats
    {
        public int TotalBlocks;
        public int TotalArmorBlocks;
        public int RemainingBlocks;
        public int RemainingArmorBlocks;
        public int BuildableBlocks;

        public readonly Dictionary<MyDefinitionId, int> RemainingBlocksPerType = new Dictionary<MyDefinitionId, int>();

        public bool Valid => TotalBlocks > 0;
        public bool IsBuildCompleted => Valid && RemainingBlocks == 0;
        public int BuiltBlocks => TotalBlocks - RemainingBlocks;
        public int BuiltArmorBlocks => TotalArmorBlocks - RemainingArmorBlocks;

        public void Clear()
        {
            TotalBlocks = 0;
            TotalArmorBlocks = 0;

            BuildableBlocks = 0;

            RemainingBlocks = 0;
            RemainingArmorBlocks = 0;

            RemainingBlocksPerType.Clear();
        }

        public void Add(ProjectionStats other)
        {
            if (!other.Valid)
                return;

            TotalBlocks += other.TotalBlocks;
            TotalArmorBlocks += other.TotalArmorBlocks;

            BuildableBlocks += other.BuildableBlocks;

            RemainingBlocks += other.RemainingBlocks;
            RemainingArmorBlocks += other.RemainingArmorBlocks;

            foreach (var pair in other.RemainingBlocksPerType)
                RemainingBlocksPerType[pair.Key] = RemainingBlocksPerType.GetValueOrDefault(pair.Key) + pair.Value;
        }
    }
}