using Entities.Blocks;
using MultigridProjector.Logic;
using MultigridProjector.Utilities;
using MultigridProjectorClient.Utilities;
using MultigridProjectorClient.Extra;
using Sandbox.Game.Gui;
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

            Events.InvokeOnGameThread(InitializeDialogs, frames: 1);
        }

        private static void InitializeDialogs()
        {
            if (!MyTerminalControlFactory.AreControlsCreated<MySpaceProjector>())
            {
                Events.InvokeOnGameThread(InitializeDialogs, frames: 1);
                return;
            }
            
            if (Config.CurrentConfig.AlignProjection)
                ProjectorAligner.Initialize();

            if (Config.CurrentConfig.RepairProjection)
                RepairProjection.Initialize();

            if (Config.CurrentConfig.BlockHighlight)
                BlockHighlight.Initialize();
        }

        protected override void UnloadData()
        {
            if(mgpSession != null)
            {
                mgpSession.Dispose();
                mgpSession = null;
            }

            MultigridProjection.EnsureNoProjections();

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