using System;
using Sandbox.ModAPI.Ingame;

namespace MultigridProjectorPrograms.RobotArm
{
    public class RotorReverser
    {
        private readonly IMyMotorStator rotor;
        private float latestAngle;
        private int counter;
        private const int Timeout = 18;
        public event Action OnReverse;

        public RotorReverser(IMyMotorStator rotor)
        {
            this.rotor = rotor;
            latestAngle = rotor.Angle;
        }

        public void Update()
        {
            if (rotor == null)
                return;

            var velocity = rotor.TargetVelocityRad;
            if (Math.Abs(velocity) < 1e-3)
            {
                latestAngle = rotor.Angle;
                counter = 0;
                return;
            }

            // Log($"Projector rotor: {velocity:0.000} rad/s");
            // Log($"Latest angle: {latestAngle:000.0} rad");
            // Log($"Rotor angle: {rotor.Angle:000.0} rad");
            if (Math.Abs(rotor.Angle - latestAngle) < Math.Abs(velocity) * 0.1)
            {
                counter++;
                Util.Log($"Projector rotor is stuck {counter} / {Timeout}");
                if (counter >= Timeout)
                {
                    rotor.TargetVelocityRad = -velocity;
                    counter = 0;
                    OnReverse?.Invoke();
                }
            }

            latestAngle = rotor.Angle;
        }
    }
}