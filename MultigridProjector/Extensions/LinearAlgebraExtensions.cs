using System;
using VRage;
using VRageMath;

namespace MultigridProjector.Extensions
{
    public static class LinearAlgebraExtensions
    {
        public static MyPositionAndOrientation ToPositionAndOrientation(this MatrixD matrix)
        {
            return new MyPositionAndOrientation(matrix.Translation, matrix.Forward, matrix.Up);
        }

        public static void ToMatrixD(this MyPositionAndOrientation po, out MatrixD matrix)
        {
            matrix = MatrixD.CreateWorld(po.Position, po.Orientation.Forward, po.Orientation.Up);
        }

        public static string FormatYaml(this Vector3I v)
        {
            return $"{v.X}, {v.Y}, {v.Z}";
        }
    }
}