using System;
using HarmonyLib;
using MultigridProjector.Api;
using MultigridProjector.Logic;
using MultigridProjector.Utilities;
using Sandbox.ModAPI;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Session;
using Torch.Session;

namespace MultigridProjectorServer
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class MultigridProjectorPlugin : TorchPluginBase
    {
        private static string PluginName => "Multigrid Projector";
        public static MultigridProjectorPlugin Instance { get; private set; }
        private TorchSessionManager _sessionManager;

        // Retrieved by MultigridProjectorTorchAgent via reflection
        // ReSharper disable once UnusedMember.Global
        public IMultigridProjectorApi Api => MultigridProjectorApiProvider.Api;

        // ReSharper disable once UnusedMember.Local
        // private readonly MultigridProjectorCommands _commands = new MultigridProjectorCommands();

        public override void Init(ITorchBase torch)
        {
            base.Init(torch);
            Instance = this;

            PluginLog.Logger = new PluginLogger(PluginName);
            PluginLog.Prefix = "";

            EnsureOriginal.VerifyAll();
            new Harmony("com.spaceengineers.multigridprojector").PatchAll();

            _sessionManager = torch.CurrentSession.Managers.GetManager<TorchSessionManager>();
            _sessionManager.SessionStateChanged += SessionStateChanged;
            
            PluginLog.Info("Loaded server plugin");
        }

        private void SessionStateChanged(ITorchSession session, TorchSessionState newstate)
        {
            switch (newstate)
            {
                case TorchSessionState.Loading:
                    MyAPIGateway.Utilities.RegisterMessageHandler(MultigridProjectorApiProvider.ModApiRequestId, HandleModApiRequest);
                    break;
                case TorchSessionState.Loaded:
                    break;
                case TorchSessionState.Unloading:
                    break;
                case TorchSessionState.Unloaded:
                    MyAPIGateway.Utilities.UnregisterMessageHandler(MultigridProjectorApiProvider.ModApiRequestId, HandleModApiRequest);
                    break;
            }
        }

        public override void Dispose()
        {
            PluginLog.Info("Unloading server plugin");
            
            _sessionManager.SessionStateChanged -= SessionStateChanged;
            
            PluginLog.Logger = null;
            Instance = null;

            base.Dispose();
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