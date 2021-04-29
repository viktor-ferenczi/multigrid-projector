using System;
using MultigridProjector.Logic;
using MultigridProjector.Utilities;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;

namespace MultigridProjectorDedicated
{
    // ReSharper disable once UnusedType.Global
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class PluginSession : MySessionComponentBase
    {
        private MultigridProjectorSession mgpSession;

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            mgpSession = new MultigridProjectorSession();
        }

        protected override void UnloadData()
        {
            if(mgpSession != null)
            {
                mgpSession.Dispose();
                mgpSession = null;
            }
        }

        public override void UpdateAfterSimulation()
        {
            mgpSession?.Update();
        }
    }
}