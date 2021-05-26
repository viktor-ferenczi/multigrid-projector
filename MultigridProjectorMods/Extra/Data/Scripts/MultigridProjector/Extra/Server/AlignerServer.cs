using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Input;
using VRageMath;

// ReSharper disable once CheckNamespace
namespace MultigridProjector.Extra
{
    // public class AlignerServer
    // {
    //     private const int FirstRepeatPeriod = 18;
    //     private const int RepeatPeriod = 6;
    //
    //     private static readonly Vector3I MinOffset = new Vector3I(-50, -50, -50);
    //     private static readonly Vector3I MaxOffset = new Vector3I(+50, +50, +50);
    //
    //     private static AlignerClient instance;
    //
    //     private IMyProjector projector;
    //     private Vector3I offset;
    //     private Vector3I rotation;
    //     private MyKeys lastPressed;
    //     private int repeatCountdown;
    //
    //     private bool Active => projector != null;
    //
    //     public AlignerServer()
    //     {
    //         instance = this;
    //     }
    //
    //     private readonly Dictionary<Base6Directions.Direction, MyKeys> offsetKeys = new Dictionary<Base6Directions.Direction, MyKeys>
    //     {
    //         {Base6Directions.Direction.Left, MyKeys.D},
    //         {Base6Directions.Direction.Right, MyKeys.A},
    //         {Base6Directions.Direction.Down, MyKeys.C},
    //         {Base6Directions.Direction.Up, MyKeys.Space},
    //         {Base6Directions.Direction.Backward, MyKeys.W},
    //         {Base6Directions.Direction.Forward, MyKeys.S},
    //     };
    //
    //     private readonly Dictionary<Base6Directions.Direction, MyKeys> rotationKeys = new Dictionary<Base6Directions.Direction, MyKeys>
    //     {
    //         {Base6Directions.Direction.Forward, MyKeys.End},
    //         {Base6Directions.Direction.Backward, MyKeys.Home},
    //         {Base6Directions.Direction.Left, MyKeys.Delete},
    //         {Base6Directions.Direction.Right, MyKeys.PageDown},
    //         {Base6Directions.Direction.Down, MyKeys.Insert},
    //         {Base6Directions.Direction.Up, MyKeys.PageUp},
    //     };
    //
    //     private void Assign(IMyProjector projector)
    //     {
    //         this.projector = projector;
    //         offset = projector.ProjectionOffset;
    //         rotation = projector.ProjectionRotation;
    //     }
    //
    //     private void Release()
    //     {
    //         projector = null;
    //     }
    //
    //     public void HandleInput()
    //     {
    //         if (!Active)
    //             return;
    //
    //         if (MyAPIGateway.Gui.ChatEntryVisible ||
    //             MyAPIGateway.Gui.IsCursorVisible ||
    //             MyAPIGateway.Input.IsKeyPress(MyKeys.Escape))
    //         {
    //             Release();
    //             return;
    //         }
    //
    //         if (lastPressed != MyKeys.None && --repeatCountdown > 0)
    //         {
    //             if (MyAPIGateway.Input.IsKeyPress(lastPressed))
    //                 return;
    //
    //             lastPressed = MyKeys.None;
    //             repeatCountdown = 0;
    //         }
    //
    //         if (MyAPIGateway.Session?.LocalHumanPlayer?.Character == null)
    //             return;
    //
    //         var pm = projector.WorldMatrix;
    //         var cm = MyAPIGateway.Session.LocalHumanPlayer.Character.WorldMatrix;
    //
    //         MyKeys pressed = MyKeys.None;
    //
    //         foreach (var pair in offsetKeys)
    //         {
    //             if (MyAPIGateway.Input.IsKeyPress(pair.Value))
    //             {
    //                 pressed = pair.Value;
    //                 offset += Base6Directions.IntDirections[(int) cm.GetClosestDirection(pm.GetDirectionVector(pair.Key))];
    //             }
    //         }
    //
    //         foreach (var pair in rotationKeys)
    //         {
    //             if (MyAPIGateway.Input.IsKeyPress(pair.Value))
    //             {
    //                 pressed = pair.Value;
    //                 rotation += Base6Directions.IntDirections[(int) pair.Key];
    //             }
    //         }
    //
    //         offset = Vector3I.Max(MinOffset, Vector3I.Min(MaxOffset, offset));
    //         rotation = new Vector3I(rotation.X & 3, rotation.Y & 3, rotation.Z & 3);
    //
    //         if (projector.ProjectionOffset == offset &&
    //             projector.ProjectionRotation == rotation)
    //             return;
    //
    //         projector.ProjectionOffset = offset;
    //         projector.ProjectionRotation = rotation;
    //
    //         if (pressed == MyKeys.None)
    //         {
    //             lastPressed = MyKeys.None;
    //             return;
    //         }
    //
    //         repeatCountdown = pressed == lastPressed ? RepeatPeriod : FirstRepeatPeriod;
    //         lastPressed = pressed;
    //     }
    //
    //     public static bool Getter(IMyTerminalBlock block)
    //     {
    //         return instance?.Active ?? false;
    //     }
    //
    //     public static void Setter(IMyTerminalBlock block, bool enabled)
    //     {
    //         var projector = block as IMyProjector;
    //         if (projector == null)
    //             return;
    //
    //         if (enabled)
    //             instance?.Assign(projector);
    //         else
    //             instance?.Release();
    //     }
    //
    //     public static void Toggle(IMyTerminalBlock block)
    //     {
    //         Setter(block, !Getter(block));
    //     }
    // }
}