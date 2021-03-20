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
    }
}