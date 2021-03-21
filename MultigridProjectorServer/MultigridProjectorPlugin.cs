using HarmonyLib;
using MultigridProjector.Api;
using MultigridProjector.Logic;
using MultigridProjector.Utilities;
using Sandbox.ModAPI;
using Torch;
using Torch.API;

namespace MultigridProjectorServer
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class MultigridProjectorPlugin : TorchPluginBase
    {
        private static string PluginName => "Multigrid Projector";
        public static MultigridProjectorPlugin Instance { get; private set; }

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
            
            MyAPIGateway.Utilities.RegisterMessageHandler(MultigridProjectorApiProvider.ModApiRequestId, HandleModMessage);

            PluginLog.Info("Loaded server plugin");
        }
        
        public override void Dispose()
        {
            PluginLog.Info("Unloading server plugin");
            
            MyAPIGateway.Utilities.UnregisterMessageHandler(MultigridProjectorApiProvider.ModApiRequestId, HandleModMessage);

            PluginLog.Logger = null;
            Instance = null;

            base.Dispose();
        }
        
        private void HandleModMessage(object obj)
        {
            MyAPIGateway.Utilities.SendModMessage(MultigridProjectorApiProvider.ModApiResponseId , MultigridProjectorApiProvider.ModApi);
        }
    }
}