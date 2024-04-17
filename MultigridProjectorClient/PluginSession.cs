using System.Linq;
using MultigridProjector.Logic;
using MultigridProjector.Utilities;
using MultigridProjectorClient.Utilities;
using MultigridProjectorClient.Extra;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using VRage.Game;
using VRage.Game.Components;
using IMyProjector = Sandbox.ModAPI.IMyProjector;

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

            Events.InvokeOnGameThread(InitializeActions, frames: 1);
        }

        private void InitializeActions()
        {
            MyAPIGateway.TerminalControls.CustomActionGetter += (block, actions) =>
            {
                if (block is IMyProjector &&
                    block.BlockDefinition.SubtypeId.Contains("Projector") &&
                    !block.HasAction("BlockHighlightToggle"))
                {
                    // FIXME: Don't run it repeatedly! It causes a red "too many actions" error on screen.
                    actions.AddRange(BlockHighlight.IterActions()
                        .Concat(ProjectorAligner.IterActions()));
                }
            };
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