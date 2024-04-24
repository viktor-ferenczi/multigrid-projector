using MultigridProjector.Logic;
using MultigridProjectorClient.Utilities;
using MultigridProjectorClient.Extra;
using VRage.Game;
using VRage.Game.Components;

namespace MultigridProjectorClient
{
    // ReSharper disable once UnusedType.Global
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class PluginSession : MySessionComponentBase
    {
        private MultigridProjectorSession mgpSession;

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            MultigridProjection.EnsureNoProjections();

            mgpSession = new MultigridProjectorSession();

            ProjectorAligner.Initialize();
        }

        protected override void UnloadData()
        {
            if (mgpSession != null)
            {
                mgpSession.Dispose();
                mgpSession = null;
            }

            MultigridProjection.EnsureNoProjections();

            if (Config.CurrentConfig.ProjectorAligner)
            {
                ProjectorAligner.Instance?.Dispose();
            }
        }

        public override void UpdateAfterSimulation()
        {
            mgpSession?.Update();

            if (Config.CurrentConfig.BlockHighlight)
                BlockHighlight.HighlightLoop();

            if (Config.CurrentConfig.ShipWelding)
                ShipWelding.WeldLoop();
        }
    }
}