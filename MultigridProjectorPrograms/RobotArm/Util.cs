using System.Text;
using Sandbox.ModAPI.Ingame;
using VRageMath;

namespace MultigridProjectorPrograms.RobotArm
{
    public static class Util
    {
        private static readonly StringBuilder LogBuilder = new StringBuilder();

        public static void ClearLog()
        {
            LogBuilder.Clear();
        }

        public static void Log(string message)
        {
            LogBuilder.Append($"{message}\r\n");
        }

        public static void ShowLog(IMyTextPanel lcd)
        {
            lcd?.WriteText(LogBuilder.Length == 0 ? "OK" : LogBuilder.ToString());
        }

        public static string Format(Vector3I v)
        {
            return $"[{v.X}, {v.Y}, {v.Z}]";
        }

        public static string Format(Vector3D v)
        {
            return $"[{v.X:0.000}, {v.Y:0.000}, {v.Z:0.000}]";
        }

        public static string Format(MatrixD m)
        {
            return $"\r\n  T: {Format(m.Translation)}\r\n  F: {Format(m.Forward)}\r\n  U: {Format(m.Up)}\r\n  S: {Format(m.Scale)}";
        }
    }
}