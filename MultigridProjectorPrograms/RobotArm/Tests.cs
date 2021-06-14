#if false
using VRageMath;
using Sandbox.ModAPI.Ingame;

namespace MultigridProjectorPrograms.RobotArm
{
    public static class Tests
    {
        private static void MechanicalConnectorTests(IMyGridTerminalSystem ts)
        {
            var rotor = ts.GetBlockWithName("Rotor Test") as IMyMotorStator;
            var hinge = ts.GetBlockWithName("Hinge Test") as IMyMotorStator;
            var piston = ts.GetBlockWithName("Piston Test") as IMyPistonBase;

            VerifyPose(rotor.CustomName, rotor.Top.WorldMatrix, GetRotorTransform(rotor.Angle, rotor.Displacement) * rotor.WorldMatrix);
            VerifyPose(hinge.CustomName, hinge.Top.WorldMatrix, GetHingeTransform(hinge.Angle) * hinge.WorldMatrix);
            VerifyPose(piston.CustomName, piston.Top.WorldMatrix, GetPistonTransform(piston.CurrentPosition) * piston.WorldMatrix);
        }

        private static void VerifyPose(string name, MatrixD physical, MatrixD calculated)
        {
            var deltaTranslation = calculated.Translation - physical.Translation;
            var deltaRight = calculated.Right - physical.Right;
            var deltaUp = calculated.Up - physical.Up;
            Util.Log($"{name}:");
            Util.Log($"  dTr {Util.Format(deltaTranslation)}");
            Util.Log($"  dRi {Util.Format(deltaRight)}");
            Util.Log($"  dUp {Util.Format(deltaUp)}");
        }

        private static MatrixD GetRotorTransform(double activation, double displacement)
        {
            return MatrixD.CreateTranslation(Vector3D.Up * (0.2 + displacement)) * MatrixD.CreateFromAxisAngle(Vector3D.Down, activation);
        }

        private static MatrixD GetHingeTransform(double activation)
        {
            return MatrixD.CreateFromAxisAngle(Vector3D.Down, activation);
        }

        private static MatrixD GetPistonTransform(double activation)
        {
            return MatrixD.CreateTranslation(Vector3D.Up * (1.4 + activation));
        }
    }
}
#endif