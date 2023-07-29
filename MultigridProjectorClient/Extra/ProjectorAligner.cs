using Entities.Blocks;
using HarmonyLib;
using MultigridProjector.Utilities;
using MultigridProjectorClient.Menus;
using MultigridProjectorClient.Utilities;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Gui;
using Sandbox.Game.SessionComponents.Clipboard;
using Sandbox.Graphics.GUI;
using Sandbox.ModAPI;
using System;
using VRage.Input;
using VRage.Utils;
using VRageMath;

namespace MultigridProjectorClient.Extra
{
    [HarmonyPatch(typeof(MyGuiScreenGamePlay))]
    [HarmonyPatch("HandleUnhandledInput")]
    //[EnsureOriginal("")] <-- TODO: Figure out what to put here
    public static class MyGuiScreenGamePlay_HandleUnhandledInput
    {
        [ClientOnly]
        private static bool Prefix()
        {
            // If ProjectorAligner is active then it will be handling input instead
            if (ProjectorAligner.Instance?.Active == true)
            {
                ProjectorAligner.Instance.HandleInput();
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(MyCubeBuilder))]
    [HarmonyPatch("HandleGameInput")]
    //[EnsureOriginal("")] <-- TODO: Figure out what to put here
    public static class MyCubeBuilder_HandleGameInput
    {
        [ClientOnly]
        private static bool Prefix()
        {
            // Disable input if ProjectorAligner is active (it is handling it instead)
            // This is updated every frame, so we update it back right before it is needed
            if (ProjectorAligner.Instance?.Active == true)
                MyGuiScreenGamePlay.DisableInput = true;

            return true;
        }
    }

    [HarmonyPatch(typeof(MyClipboardComponent))]
    [HarmonyPatch("HandleGameInput")]
    //[EnsureOriginal("")] <-- TODO: Figure out what to put here
    public static class MyClipboardComponent_HandleGameInput
    {
        [ClientOnly]
        private static bool Prefix()
        {
            // Disable input if ProjectorAligner is active (it is handling it instead)
            // This is updated every frame, so we update it back right before it is needed
            if (ProjectorAligner.Instance?.Active == true)
                MyGuiScreenGamePlay.DisableInput = true;

            return true;
        }
    }

    internal class ProjectorAligner : IDisposable
    {
        private static readonly MyStringId[][] OffsetControls =
        {
            new[] { MyControlsSpace.FORWARD },
            new[] { MyControlsSpace.BACKWARD },
            new[] { MyControlsSpace.STRAFE_LEFT, MyControlsSpace.ROTATION_LEFT },
            new[] { MyControlsSpace.STRAFE_RIGHT, MyControlsSpace.ROTATION_RIGHT },
            new[] { MyControlsSpace.JUMP, MyControlsSpace.ROTATION_UP },
            new[] { MyControlsSpace.CROUCH, MyControlsSpace.ROTATION_DOWN },
        };

        private static readonly MyStringId[] RotationControls =
        {
            MyControlsSpace.CUBE_ROTATE_ROLL_NEGATIVE,
            MyControlsSpace.CUBE_ROTATE_ROLL_POSITIVE,
            MyControlsSpace.CUBE_ROTATE_HORISONTAL_POSITIVE,
            MyControlsSpace.CUBE_ROTATE_HORISONTAL_NEGATIVE,
            MyControlsSpace.CUBE_ROTATE_VERTICAL_POSITIVE,
            MyControlsSpace.CUBE_ROTATE_VERTICAL_NEGATIVE,
        };

        private static readonly MyStringId SuspendKey = MyControlsSpace.SPRINT;

        public static ProjectorAligner Instance { get; private set; }
        public bool Active => projector != null && !MyControllerHelper.IsControl(MyStringId.NullOrEmpty, SuspendKey, MyControlStateType.PRESSED);

        private static readonly Vector3I MinOffset = new Vector3I(-50, -50, -50);
        private static readonly Vector3I MaxOffset = new Vector3I(+50, +50, +50);

        private IMyProjector projector;
        private Vector3I offset;
        private Vector3I rotation;

        public static void Initialize()
        {
            bool IsProjecting(MyProjectorBase block) => IsWorking(block) && block.ProjectedGrid != null;
            bool IsWorking(MyProjectorBase block) => block.CubeGrid?.Physics != null && block.IsWorking;

            MyTerminalControlButton<MySpaceProjector> alignProjection = new MyTerminalControlButton<MySpaceProjector>(
                "AlignProjection",
                MyStringId.GetOrCompute("Align Projection"),
                MyStringId.GetOrCompute("Manually align the projection using keys familiar from block placement."),
                ShowDialog)
            {
                Visible = (_) => true,
                Enabled = IsProjecting,
                SupportsMultipleBlocks = false
            };

            AddControl.AddControlAfter("Blueprint", alignProjection);

            Instance = new ProjectorAligner();
        }

        public static void ShowDialog(MyProjectorBase projector)
        {
            if (Config.CurrentConfig.ShowDialogs)
            {
                MyGuiScreenMessageBox alignerDialog = AlignerDialog.CreateDialog(() =>
                {
                    Setter(projector, true);
                });

                MyGuiSandbox.AddScreen(alignerDialog);
            }
            else
            {
                MyGuiScreenTerminal instance = (MyGuiScreenTerminal)Reflection.GetValue(typeof(MyGuiScreenTerminal), "m_instance");
                instance.DataUnloading += (_) => Setter(projector, true);
            }

            MyGuiScreenTerminal.Hide();
        }

        public void HandleInput()
        {
            if (!Active)
                return;

            if (MyAPIGateway.Gui.ChatEntryVisible ||
                MyAPIGateway.Gui.IsCursorVisible ||
                !projector.IsProjecting ||
                MyInput.Static.IsKeyPress(MyKeys.Escape))
            {
                MyAPIGateway.Utilities.ShowMessage("Multigrid Projector", "Alignment cancelled");
                Release();
                return;
            }

            for (int directionIndex = 0; directionIndex < 6; directionIndex++)
            {
                foreach (MyStringId offsetControl in OffsetControls[directionIndex])
                {
                    if (!MyControllerHelper.IsControl(MyStringId.NullOrEmpty, offsetControl, MyControlStateType.NEW_PRESSED_REPEATING))
                        continue;

                    Move(directionIndex);
                    break;
                }

                MyStringId rotationControl = RotationControls[directionIndex];
                if (MyControllerHelper.IsControl(MyStringId.NullOrEmpty, rotationControl, MyControlStateType.NEW_PRESSED_REPEATING))
                {
                    Rotate(directionIndex);
                    break;
                }
            }

            UpdateOffsetAndRotation();
        }

        private void UpdateOffsetAndRotation()
        {
            if (projector.ProjectionOffset == offset &&
                projector.ProjectionRotation == rotation)
                return;

            var isConsoleBlock = ((MyProjectorBase)projector).AllowScaling;
            projector.ProjectionOffset = offset;
            projector.ProjectionRotation = isConsoleBlock ? 90 * rotation : rotation;
            projector.UpdateOffsetAndRotation();
        }

        private void Move(int directionIndex)
        {
            Base6Directions.Direction direction = (Base6Directions.Direction)directionIndex;
            Vector3D directionVector = MyAPIGateway.Session.LocalHumanPlayer.Character.WorldMatrix.GetDirectionVector(direction);
            Base6Directions.Direction closestDirectionOnProjector = projector.WorldMatrix.GetClosestDirection(directionVector);

            Vector3I step = Base6Directions.IntDirections[(int)closestDirectionOnProjector];
            Vector3I movedOffset = Vector3I.Max(MinOffset, Vector3I.Min(MaxOffset, offset - step));

            offset = Vector3I.Max(MinOffset, Vector3I.Min(MaxOffset, movedOffset));
        }

        private void Rotate(int directionIndex)
        {
            Base6Directions.Direction direction = (Base6Directions.Direction)directionIndex;
            Vector3D directionVector = MyAPIGateway.Session.LocalHumanPlayer.Character.WorldMatrix.GetDirectionVector(direction);
            Base6Directions.Direction closestProjectorDirection = projector.WorldMatrix.GetClosestDirection(directionVector);
            Vector3 projectorRotationAxis = Base6Directions.GetVector(closestProjectorDirection);

            Vector3D yawPitchRoll = rotation * 0.5 * Math.PI;
            QuaternionD q = QuaternionD.CreateFromYawPitchRoll(yawPitchRoll.X, yawPitchRoll.Y, yawPitchRoll.Z);
            QuaternionD w = QuaternionD.CreateFromAxisAngle(projectorRotationAxis, 0.5 * Math.PI);
            MatrixD m = MatrixD.CreateFromQuaternion(w * q);

            Base6Directions.Direction forward = Base6Directions.GetClosestDirection(m.Forward);
            Base6Directions.Direction up = Base6Directions.GetClosestDirection(m.Up);

            OrientationAlgebra.ProjectionRotationFromForwardAndUp(forward, up, out rotation);
        }

        public static void Setter(IMyTerminalBlock block, bool enabled)
        {
            if (!(block is IMyProjector projector))
                return;

            if (enabled && projector.IsProjecting)
                Instance?.Assign(projector);
            else
                Instance?.Release();
        }

        private void Assign(IMyProjector projector)
        {
            this.projector = projector;
            offset = projector.ProjectionOffset;
            rotation = projector.ProjectionRotation;
        }

        private void Release()
        {
            projector = null;
        }

        public void Dispose()
        {
            Release();
        }
    }
}