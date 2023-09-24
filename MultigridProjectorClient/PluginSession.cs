using Entities.Blocks;
using MultigridProjector.Logic;
using MultigridProjector.Utilities;
using MultigridProjectorClient.Utilities;
using MultigridProjectorClient.Extra;
using Sandbox.Game.Gui;
using Sandbox.Game.World;
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
                // Prevent infinite loop since controls will not be created if they haven't been yet
                if (MySession.Static.IsUnloading) 
                    return; 

                Events.InvokeOnGameThread(InitializeDialogs, frames: 1);
                return;
            }

            ProjectorAligner.Initialize();
            RepairProjection.Initialize();
            BlockHighlight.Initialize();
            CraftProjection.Initialize();
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