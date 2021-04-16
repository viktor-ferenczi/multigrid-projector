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
        public static MultigridProjectorPlugin Instance { get; private set; }
        private TorchSessionManager _sessionManager;
        private bool Initialized => _sessionManager != null;

        // Retrieved by MultigridProjectorTorchAgent via reflection
        // ReSharper disable once UnusedMember.Global
        public IMultigridProjectorApi Api => MultigridProjectorApiProvider.Api;

        // ReSharper disable once UnusedMember.Local
        // private readonly MultigridProjectorCommands _commands = new MultigridProjectorCommands();

        private static Harmony Harmony => new Harmony("com.spaceengineers.multigridprojector");

        public override void Init(ITorchBase torch)
        {
            base.Init(torch);
            Instance = this;

            PluginLog.Logger = new PluginLogger(PluginLog.Prefix);
            PluginLog.Prefix = "";

            try
            {
                EnsureOriginal.VerifyAll();
                EnsureOriginalTorch.VerifyAll();
            }
            catch (NotSupportedException e)
            {
                PluginLog.Error("Found incompatible code changes in the game or plugin patch collisions. Please report the exception below on the SE Mods Discord (invite is on the Workshop page):");
                throw;
            }

            Harmony.PatchAll();

            _sessionManager = torch.Managers.GetManager<TorchSessionManager>();
            _sessionManager.SessionStateChanged += SessionStateChanged;
            
            PluginLog.Info("Loaded server plugin");
        }

        private void SessionStateChanged(ITorchSession session, TorchSessionState newstate)
        {
            switch (newstate)
            {
                case TorchSessionState.Loading:
                    break;
                case TorchSessionState.Loaded:
                    MyAPIGateway.Utilities.RegisterMessageHandler(MultigridProjectorApiProvider.ModApiRequestId, HandleModApiRequest);
                    break;
                case TorchSessionState.Unloading:
                    MyAPIGateway.Utilities.UnregisterMessageHandler(MultigridProjectorApiProvider.ModApiRequestId, HandleModApiRequest);
                    break;
                case TorchSessionState.Unloaded:
                    break;
            }
        }

        public override void Dispose()
        {
            if (!Initialized)
                return;

            PluginLog.Info("Unloading server plugin");
            
            _sessionManager.SessionStateChanged -= SessionStateChanged;
            _sessionManager = null;
            
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