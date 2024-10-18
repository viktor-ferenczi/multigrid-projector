using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Windows.Controls;
using HarmonyLib;
using MultigridProjector.Api;
using MultigridProjector.Logic;
using MultigridProjector.Utilities;
using MultigridProjectorServer.MultigridProjector.Utilities;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Plugins;
using Torch.API.Session;
using Torch.Session;

namespace MultigridProjectorServer
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class MultigridProjectorPlugin : TorchPluginBase, IWpfPlugin
    {
        public static MultigridProjectorPlugin Instance { get; private set; }
        private TorchSessionManager _sessionManager;
        private bool Initialized => _sessionManager != null;

        // Retrieved by MultigridProjectorTorchAgent via reflection
        // ReSharper disable once UnusedMember.Global
        public IMultigridProjectorApi Api => MultigridProjectorApiProvider.Api;

        // ReSharper disable once UnusedMember.Local
        // private readonly MultigridProjectorCommands _commands = new MultigridProjectorCommands();

        // ReSharper disable once UnusedMember.Global
        public UserControl GetControl() => control ?? (control = new ConfigView());
        private ConfigView control;

        private Persistent<MultigridProjectorConfig> config;
        public MultigridProjectorConfig Config => config?.Data;

        private MultigridProjectorSession mgpSession;

        private static Harmony Harmony => new Harmony("com.spaceengineers.multigridprojector");

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public override void Init(ITorchBase torch)
        {
            base.Init(torch);
            Instance = this;

            PluginLog.Logger = new PluginLogger(PluginLog.Prefix);
            PluginLog.Prefix = "";

            var configPath = Path.Combine(StoragePath, MultigridProjectorConfig.ConfigFilePath);
            config = Persistent<MultigridProjectorConfig>.Load(configPath);
            config.Data.PropertyChanged += OnPropertyChanged;

            if (!WineDetector.IsRunningInWineOrProton())
            {
                try
                {
                    EnsureOriginal.VerifyAll();
                    EnsureOriginalTorch.VerifyAll();
                }
                catch (NotSupportedException e)
                {
                    PluginLog.Error(e, "Disabled the plugin due to potentially incompatible code changes in the game or plugin patch collisions. Please report the exception below on the SE Mods Discord (invite is on the Workshop page):");
                    return;
                }
            }

            Harmony.PatchAll(Assembly.GetExecutingAssembly());

            _sessionManager = torch.Managers.GetManager<TorchSessionManager>();
            _sessionManager.SessionStateChanged += SessionStateChanged;

            PluginLog.Info("Loaded server plugin");
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // FIXME: Hacking the config there, replace with proper plugin config
            MultigridProjection.SetPreviewBlockVisuals = Config.SetPreviewBlockVisuals;
        }

        private void SessionStateChanged(ITorchSession session, TorchSessionState newstate)
        {
            switch (newstate)
            {
                case TorchSessionState.Loading:
                    break;
                case TorchSessionState.Loaded:

                    // FIXME: Hacking the config there, replace with proper plugin config
                    MultigridProjection.SetPreviewBlockVisuals = Config.SetPreviewBlockVisuals;

                    mgpSession = new MultigridProjectorSession();
                    break;

                case TorchSessionState.Unloading:
                    if (mgpSession != null)
                    {
                        mgpSession.Dispose();
                        mgpSession = null;
                    }
                    break;
                case TorchSessionState.Unloaded:
                    break;
            }
        }

        public override void Update()
        {
            mgpSession?.Update();
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
    }
}