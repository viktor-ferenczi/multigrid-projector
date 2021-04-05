using VRageMath;

namespace MultigridProjector.Utilities
{
    public static class OrientationAlgebra
    {
        private static readonly bool[] ValidOrientations =
        {
            false,
            false,
            true,
            true,
            true,
            true,
            false,
            false,
            true,
            true,
            true,
            true,
            true,
            true,
            false,
            false,
            true,
            true,
            true,
            true,
            false,
            false,
            true,
            true,
            true,
            true,
            true,
            true,
            false,
            false,
            true,
            true,
            true,
            true,
            false,
            false,
        };

        private static readonly Vector3I[] ProjectionRotations =
        {
            new Vector3I(0, 0, 0),
            new Vector3I(0, 0, 0),
            new Vector3I(-2, -2, -1),
            new Vector3I(-2, -2, 1),
            new Vector3I(-2, -2, -2),
            new Vector3I(-2, -2, 0),
            new Vector3I(0, 0, 0),
            new Vector3I(0, 0, 0),
            new Vector3I(-2, 0, -1),
            new Vector3I(-2, 0, 1),
            new Vector3I(-2, 0, 0),
            new Vector3I(-2, 0, -2),
            new Vector3I(-1, -2, 1),
            new Vector3I(-1, -2, -1),
            new Vector3I(0, 0, 0),
            new Vector3I(0, 0, 0),
            new Vector3I(-1, -2, -2),
            new Vector3I(-1, -2, 0),
            new Vector3I(-1, 0, 1),
            new Vector3I(-1, 0, -1),
            new Vector3I(0, 0, 0),
            new Vector3I(0, 0, 0),
            new Vector3I(-1, 0, 0),
            new Vector3I(-1, 0, -2),
            new Vector3I(-2, 1, 0),
            new Vector3I(-2, 1, -2),
            new Vector3I(-2, 1, -1),
            new Vector3I(-2, 1, 1),
            new Vector3I(0, 0, 0),
            new Vector3I(0, 0, 0),
            new Vector3I(-2, -1, -2),
            new Vector3I(-2, -1, 0),
            new Vector3I(-2, -1, -1),
            new Vector3I(-2, -1, 1),
            new Vector3I(0, 0, 0),
            new Vector3I(0, 0, 0),
        };

        public static bool ProjectionRotationFromForwardAndUp(Base6Directions.Direction forward, Base6Directions.Direction up, out Vector3I rotation)
        {
            var index = ((int) forward * 6 + (int) up) % 36;
            if (!ValidOrientations[index])
            {
                rotation = Vector3I.Zero;
                return false;
            }

            rotation = ProjectionRotations[index];
            return true;
        }
    }
}