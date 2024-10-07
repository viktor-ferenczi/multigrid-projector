using System;
using System.Reflection;
using HarmonyLib;
using MultigridProjector.Utilities;
using MultigridProjectorClient.Utilities;
using Sandbox.Graphics.GUI;
using VRage.Plugins;

namespace MultigridProjectorClient
{
    // ReSharper disable once UnusedType.Global
    public class MultigridProjectorPlugin : IPlugin
    {
        private static Harmony Harmony => new Harmony("com.spaceengineers.multigridprojector");

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public void Init(object gameInstance)
        {
            PluginLog.Logger = new PluginLogger();

            PluginLog.Info("Loading client plugin");
            if (Environment.GetEnvironmentVariable("SE_PLUGIN_DISABLE_METHOD_VERIFICATION") == null)
            {
                // It will throw NotSupportedException if the game code has changed,
                // Plugin Loader will catch this and show "Error" next to the plugin
                EnsureOriginal.VerifyAll();
            }
            
            Harmony.PatchAll(Assembly.GetExecutingAssembly());

            PluginLog.Info("Loading config");
            Config.LoadConfig();

            PluginLog.Info("Client plugin loaded");
        }

        // This is invoked by Plugin Loader
        // ReSharper disable once UnusedMember.Global
        public void OpenConfigDialog()
        {
            MyGuiSandbox.AddScreen(Menus.ConfigMenu.CreateDialog());
        }

        public void Dispose()
        {
            // NOTE: Unpatching caused problems for other plugins, so just keeping the plugin installed all the time, which is common practice with Plugin Loader
            // PluginLog.Info("Unloading the Multigrid Projector Client Plugin");
            // Harmony.UnpatchAll();

            PluginLog.Info("Unloaded client plugin");
        }

        public void Update()
        {
        }
    }
}