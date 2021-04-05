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

        public static Vector3D ToRollPitchYaw(this Quaternion q)
        {
            Vector3D angles;

            // Roll (x-axis rotation)
            var sc = 2 * (q.W * q.X + q.Y * q.Z);
            var cc = 1 - 2 * (q.X * q.X + q.Y * q.Y);
            angles.X = Math.Atan2(sc, cc);

            // Pitch (y-axis rotation)
            var sp = 2 * (q.W * q.Y - q.Z * q.X);
            if (Math.Abs(sp) >= 1)
                // Use 90 degrees if out of range
                angles.Y = Math.Sign(sp) * Math.PI / 2;
            else
                angles.Y = Math.Asin(sp);

            // Yaw (z-axis rotation)
            sc = 2 * (q.W * q.Z + q.X * q.Y);
            cc = 1 - 2 * (q.Y * q.Y + q.Z * q.Z);
            angles.Z = Math.Atan2(sc, cc);

            return angles;
        }
    }
}