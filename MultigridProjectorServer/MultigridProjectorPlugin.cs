using HarmonyLib;
using MultigridProjector.Api;
using MultigridProjector.Logic;
using MultigridProjector.Utilities;
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

            PluginLog.Info("Loaded server plugin");
        }

        public override void Dispose()
        {
            PluginLog.Info("Unloading server plugin");

            PluginLog.Logger = null;
            Instance = null;

            base.Dispose();
        }
    }
}