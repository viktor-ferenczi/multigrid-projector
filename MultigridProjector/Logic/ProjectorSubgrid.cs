using System.Runtime.CompilerServices;
using Sandbox.Game.Entities.Cube;
using VRageMath;

namespace MultigridProjector.Logic
{
    public class ProjectorSubgrid: Subgrid
    {
        private readonly Quaternion _projectorQuaternion;
        private readonly Quaternion _projectionRotationQuaternion;

        public ProjectorSubgrid(MultigridProjection projection) : base(projection, 0)
        {
            var projector = projection.Projector;

            projector.Orientation.GetQuaternion(out _projectorQuaternion);
            _projectionRotationQuaternion = projector.ProjectionRotationQuaternion;
        }

        public override bool IsConnectedSomewhere => true;
    }
}