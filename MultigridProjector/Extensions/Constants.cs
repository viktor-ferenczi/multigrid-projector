using VRageMath;

namespace MultigridProjector.Extensions
{
    public struct Constants
    {
        public static readonly BoundingBoxI MaxBoundingBoxI = new BoundingBoxI(Vector3I.MinValue, Vector3I.MaxValue);
        public static readonly BoundingBoxI InvalidBoundingBoxI = BoundingBoxI.CreateInvalid();
    }
}