using System.Collections.Generic;
using System.Linq;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;
using VRageMath;

namespace MultigridProjector.Extra
{
    public class ProjectionAlignment
    {
        public static void AlignToRepairProjector(IMyProjector projector, MyObjectBuilder_CubeGrid gridBuilder)
        {
            // Find the projector itself in the self repair projection
            var projectorBuilder = gridBuilder
                .CubeBlocks
                .OfType<MyObjectBuilder_Projector>()
                .FirstOrDefault(b =>
                    b.SubtypeId.ToString() == projector.BlockDefinition.SubtypeId &&
                    (Vector3I) b.Min == projector.Min &&
                    (MyBlockOrientation) b.BlockOrientation == projector.Orientation &&
                    (b.CustomName ?? b.Name) == (projector.CustomName ?? projector.Name));

            if (projectorBuilder == null) return;

            Quaternion gridToProjectorQuaternion;
            projector.Orientation.GetQuaternion(out gridToProjectorQuaternion);
            var projectorToGridQuaternion = Quaternion.Inverse(gridToProjectorQuaternion);
            var projectionRotation = OrientationAlgebra.ProjectionRotationFromForwardAndUp(
                Base6Directions.GetDirection(projectorToGridQuaternion.Forward),
                Base6Directions.GetDirection(projectorToGridQuaternion.Up));

            var blocks = new List<IMySlimBlock>();
            projector.CubeGrid.GetBlocks(blocks, block => blocks.Count == 0);
            if (blocks.Count == 0)
                return;

            var projectorPosition = projector.Position - blocks[0].Position;
            var projectionOffset = new Vector3I(Vector3.Round(projectorToGridQuaternion * projectorPosition));
            projectionOffset = Vector3I.Clamp(projectionOffset, new Vector3I(-50), new Vector3I(50));

            projector.ProjectionOffset = projectionOffset;
            projector.ProjectionRotation = projectionRotation;
            projector.UpdateOffsetAndRotation();
        }
    }
}