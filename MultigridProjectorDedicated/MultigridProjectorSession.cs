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
    public class MultigridProjectorSession : MySessionComponentBase
    {
        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            MyAPIGateway.Utilities.RegisterMessageHandler(MultigridProjectorApiProvider.ModApiRequestId, HandleModApiRequest);
        }

        protected override void UnloadData()
        {
            MyAPIGateway.Utilities.UnregisterMessageHandler(MultigridProjectorApiProvider.ModApiRequestId, HandleModApiRequest);
        }

        public override void UpdateAfterSimulation()
        {
        }

        private void HandleModApiRequest(object obj)
        {
            try
            {
                MyAPIGateway.Utilities.SendModMessage(MultigridProjectorApiProvider.ModApiResponseId, MultigridProjectorApiProvider.ModApi);
            }
            catch (Exception e)
            {
                PluginLog.Error(e, "Failed to respond to Mod API request");
            }
        }
    }
}