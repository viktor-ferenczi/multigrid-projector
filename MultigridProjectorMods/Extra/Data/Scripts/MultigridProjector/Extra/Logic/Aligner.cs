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
            new[] {MyKeys.S},
            new[] {MyKeys.W},
            new[] {MyKeys.D, MyKeys.Right},
            new[] {MyKeys.A, MyKeys.Left},
            new[] {MyKeys.C, MyKeys.Down},
            new[] {MyKeys.Space, MyKeys.Up},
        };

        private static readonly MyKeys[] RotationKeys =
        {
            MyKeys.PageDown,
            MyKeys.Delete,
            MyKeys.PageUp,
            MyKeys.Insert,
            MyKeys.Home,
            MyKeys.End,
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

                    var increment = GetIncrementByDirection(directionIndex);
                    offset = NormalizeProjectorOffset(offset + increment);
                    pressed = offsetKey;
                    break;
                }

                if (pressed != MyKeys.None)
                    break;

                var rotationKey = RotationKeys[directionIndex];
                if (MyAPIGateway.Input.IsKeyPress(rotationKey))
                {
                    var increment = GetIncrementByDirection(directionIndex);
                    rotation = NormalizeProjectorRotation(rotation + increment);
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

        private static Vector3I NormalizeProjectorOffset(Vector3I offset) => Vector3I.Max(MinOffset, Vector3I.Min(MaxOffset, offset));

        private static Vector3I NormalizeProjectorRotation(Vector3I r) => new Vector3I(NormalizeRotationValue(r.X), NormalizeRotationValue(r.Y), NormalizeRotationValue(r.Z));

        private static int NormalizeRotationValue(int v) => ((v + 1) & 3) - 1;

        private Vector3I GetIncrementByDirection(int directionIndex)
        {
            var direction = (Base6Directions.Direction) directionIndex;
            var directionVector = MyAPIGateway.Session.LocalHumanPlayer.Character.WorldMatrix.GetDirectionVector(direction);
            var closestProjectorDirection = projector.WorldMatrix.GetClosestDirection(directionVector);
            return Base6Directions.IntDirections[(int) closestProjectorDirection];
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