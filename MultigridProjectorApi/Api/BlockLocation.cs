using VRageMath;

namespace MultigridProjector.Api
{
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
}