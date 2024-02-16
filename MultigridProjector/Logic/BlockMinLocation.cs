using VRageMath;

namespace MultigridProjector.Logic
{
    public readonly struct BlockMinLocation
    {
        public readonly int GridIndex;
        public readonly Vector3I MinPosition;

        public BlockMinLocation(int gridIndex, Vector3I minPosition)
        {
            GridIndex = gridIndex;
            MinPosition = minPosition;
        }

        public bool Equals(in BlockMinLocation other) =>
            GridIndex == other.GridIndex &&
            MinPosition == other.MinPosition;

        public override int GetHashCode()
        {
            return ((GridIndex * 397 ^ MinPosition.X) * 397 ^ MinPosition.Y) * 397 ^ MinPosition.Z;
        }
    }
}