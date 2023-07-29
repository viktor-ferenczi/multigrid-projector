using MultigridProjector.Logic;
using MultigridProjector.Utilities;
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
        private Comms comms;

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            MultigridProjection.EnsureNoProjections();

            mgpSession = new MultigridProjectorSession();
            comms = new Comms();

            Events.InvokeOnGameThread(() =>
            {
                if (Config.CurrentConfig.AlignProjection)
                    ProjectorAligner.Initialize();

                if (Config.CurrentConfig.RepairProjection)
                    RepairProjection.Initialize();

                if (Config.CurrentConfig.BlockHighlight)
                    BlockHighlight.Initialize();
            });
        }

        protected override void UnloadData()
        {
            if(mgpSession != null)
            {
                mgpSession.Dispose();
                mgpSession = null;
            }

            MultigridProjection.EnsureNoProjections();

            comms?.Dispose();
            comms = null;

            if (Config.CurrentConfig.AlignProjection)
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