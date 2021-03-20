using VRageMath;

namespace MultigridProjector.Logic
{
    public struct BlockMinLocation
    {
        public readonly int GridIndex;
        public readonly Vector3I MinPosition;

        public BlockMinLocation(int gridIndex, Vector3I minPosition)
        {
            GridIndex = gridIndex;
            MinPosition = minPosition;
        }
    }
}