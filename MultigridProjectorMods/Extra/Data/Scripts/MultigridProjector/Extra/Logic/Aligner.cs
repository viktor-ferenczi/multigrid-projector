using System;
using Sandbox.ModAPI;
using VRage.Input;
using VRageMath;

// ReSharper disable once CheckNamespace
namespace MultigridProjector.Extra
{
    public class Aligner : IDisposable
    {
        #region Constants

        private const int FirstRepeatPeriod = 18;
        private const int RepeatPeriod = 6;

        private static readonly Vector3I MinOffset = new Vector3I(-50, -50, -50);
        private static readonly Vector3I MaxOffset = new Vector3I(+50, +50, +50);

        #endregion

        #region Keys

        private static readonly MyKeys[][] OffsetKeys =
        {
            new[] {MyKeys.W},
            new[] {MyKeys.S},
            new[] {MyKeys.A, MyKeys.Left},
            new[] {MyKeys.D, MyKeys.Right},
            new[] {MyKeys.Space, MyKeys.Up},
            new[] {MyKeys.C, MyKeys.Down},
        };

        private static readonly MyKeys[] RotationKeys =
        {
            MyKeys.Home,
            MyKeys.PageDown,
            MyKeys.PageUp,
            MyKeys.Insert,
            MyKeys.End,
            MyKeys.Delete,
        };

        #endregion

        #region Logic

        private static Aligner instance;

        private IMyProjector projector;
        private Vector3I offset;
        private Vector3I rotation;
        private MyKeys lastPressed;
        private int repeatCountdown;

        private bool Active => projector != null;

        public Aligner()
        {
            instance = this;
        }

        public void Dispose()
        {
            Release();
        }

        public void HandleInput()
        {
            if (!Active)
                return;

            if (MyAPIGateway.Gui.ChatEntryVisible ||
                MyAPIGateway.Gui.IsCursorVisible ||
                MyAPIGateway.Session.LocalHumanPlayer.Character.ControllerInfo.IsLocallyHumanControlled() ||
                !projector.IsProjecting)
            {
                Release();
                return;
            }

            if (lastPressed != MyKeys.None && --repeatCountdown > 0)
            {
                if (MyAPIGateway.Input.IsKeyPress(lastPressed))
                    return;

                lastPressed = MyKeys.None;
                repeatCountdown = 0;
            }

            if (MyAPIGateway.Session?.LocalHumanPlayer?.Character == null)
                return;

            var pressed = MyKeys.None;
            for (var directionIndex = 0; directionIndex < 6; directionIndex++)
            {
                foreach (var offsetKey in OffsetKeys[directionIndex])
                {
                    if (!MyAPIGateway.Input.IsKeyPress(offsetKey))
                        continue;

                    Move(directionIndex);
                    pressed = offsetKey;
                    break;
                }

                if (pressed != MyKeys.None)
                    break;

                var rotationKey = RotationKeys[directionIndex];
                if (MyAPIGateway.Input.IsKeyPress(rotationKey))
                {
                    Rotate(directionIndex);
                    pressed = rotationKey;
                    break;
                }
            }

            if (pressed == MyKeys.None)
            {
                lastPressed = MyKeys.None;
                return;
            }

            repeatCountdown = pressed == lastPressed ? RepeatPeriod : FirstRepeatPeriod;
            lastPressed = pressed;

            UpdateOffsetAndRotation();
        }

        private void Move(int directionIndex)
        {
            var direction = (Base6Directions.Direction) directionIndex;
            var directionVector = MyAPIGateway.Session.LocalHumanPlayer.Character.WorldMatrix.GetDirectionVector(direction);
            var closestDirectionOnProjector = projector.WorldMatrix.GetClosestDirection(directionVector);

            var step = Base6Directions.IntDirections[(int) closestDirectionOnProjector];
            var movedOffset = Vector3I.Max(MinOffset, Vector3I.Min(MaxOffset, offset - step));

            offset = Vector3I.Max(MinOffset, Vector3I.Min(MaxOffset, movedOffset));
        }

        private void Rotate(int directionIndex)
        {
            var direction = (Base6Directions.Direction) directionIndex;
            var directionVector = MyAPIGateway.Session.LocalHumanPlayer.Character.WorldMatrix.GetDirectionVector(direction);
            var closestProjectorDirection = projector.WorldMatrix.GetClosestDirection(directionVector);
            var projectorRotationAxis = Base6Directions.GetVector(closestProjectorDirection);

            var yawPitchRoll = rotation * 0.5 * Math.PI;
            var q = QuaternionD.CreateFromYawPitchRoll(yawPitchRoll.X, yawPitchRoll.Y, yawPitchRoll.Z);
            var w = QuaternionD.CreateFromAxisAngle(projectorRotationAxis, 0.5 * Math.PI);
            var m = MatrixD.CreateFromQuaternion(q * w);

            var forward = Base6Directions.GetClosestDirection(m.Forward);
            var up = Base6Directions.GetClosestDirection(m.Up);

            rotation = OrientationAlgebra.ProjectionRotationFromForwardAndUp(forward, up);
        }

        private void UpdateOffsetAndRotation()
        {
            if (projector.ProjectionOffset == offset &&
                projector.ProjectionRotation == rotation)
                return;

            projector.ProjectionOffset = offset;
            projector.ProjectionRotation = rotation;
            projector.UpdateOffsetAndRotation();
        }

        // ReSharper disable once ParameterHidesMember
        private void Assign(IMyProjector projector)
        {
            this.projector = projector;
            offset = projector.ProjectionOffset;
            rotation = projector.ProjectionRotation;
        }

        // Client only
        private void Release()
        {
            projector = null;
        }

        public static bool Getter(IMyTerminalBlock block)
        {
            return instance?.projector?.EntityId == block.EntityId && instance.projector.IsProjecting;
        }

        public static void Setter(IMyTerminalBlock block, bool enabled)
        {
            var projector = block as IMyProjector;
            if (projector == null)
                return;

            if (enabled && projector.IsProjecting)
                instance?.Assign(projector);
            else
                instance?.Release();
        }

        public static void Toggle(IMyTerminalBlock block)
        {
            Setter(block, !Getter(block));
        }

        #endregion
    }
}