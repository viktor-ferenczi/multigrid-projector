using MultigridProjector.Logic;
using MultigridProjector.Utilities;
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
            if (MultigridProjection.Projections.Count != 0)
                PluginLog.Warn($"{MultigridProjection.Projections.Count} projections are active on loading session, there should be none!");

            mgpSession = new MultigridProjectorSession();
        }

        protected override void UnloadData()
        {
            if(mgpSession != null)
            {
                mgpSession.Dispose();
                mgpSession = null;
            }

            if (MultigridProjection.Projections.Count != 0)
                PluginLog.Warn($"{MultigridProjection.Projections.Count} projections are active on unloading session, there should be none!");
        }

        public override void UpdateAfterSimulation()
        {
            mgpSession?.Update();
        }
    }
}