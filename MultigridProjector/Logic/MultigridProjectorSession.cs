using System;
using MultigridProjector.Utilities;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.Entity;

namespace MultigridProjector.Logic
{
    public class MultigridProjectorSession: IDisposable
    {
        private bool pbPresent;
        private bool pbPrepared;

        public MultigridProjectorSession()
        {
            MyEntities.OnEntityCreate += OnEntityCreate;
            MyAPIGateway.Utilities.RegisterMessageHandler(MultigridProjectorApiProvider.ModApiRequestId, HandleModApiRequest);
        }

        public void Dispose()
        {
            MyAPIGateway.Utilities.UnregisterMessageHandler(MultigridProjectorApiProvider.ModApiRequestId, HandleModApiRequest);
            MyEntities.OnEntityCreate -= OnEntityCreate;
        }

        private void OnEntityCreate(MyEntity myEntity)
        {
            if (pbPresent)
                return;

            if (myEntity is IMyProgrammableBlock)
                pbPresent = true;
        }

        public void Update()
        {
            if (!pbPresent)
                return;

            if (pbPrepared)
                return;

            MultigridProjectorApiProvider.RegisterProgrammableBlockApi();
            pbPrepared = true;
        }

        private static void HandleModApiRequest(object obj)
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