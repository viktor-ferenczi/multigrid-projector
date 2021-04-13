using MultigridProjector.Logic;
using MultigridProjector.Utilities;
using VRage.Game;
using VRage.Game.Components;

namespace MultigridProjectorClient
{
    // ReSharper disable once UnusedType.Global
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class MultigridProjectorSession : MySessionComponentBase
    {

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            if (MultigridProjection.Projections.Count != 0)
                PluginLog.Warn($"{MultigridProjection.Projections.Count} projections are active on loading session, there should be none!");
        }

        protected override void UnloadData()
        {
            if (MultigridProjection.Projections.Count != 0)
                PluginLog.Warn($"{MultigridProjection.Projections.Count} projections are active on unloading session, there should be none!");
        }

        public override void UpdateAfterSimulation()
        {
        }
    }
}