using System;
using ClientPlugin.Settings;
using ClientPlugin.Settings.Layouts;
using HarmonyLib;
using MultigridProjector.Utilities;
using MultigridProjectorClient;
using MultigridProjectorServer.MultigridProjector.Utilities;
using Sandbox.Graphics.GUI;
using VRage.Plugins;

// Define assembly version when compiled by Pulsar
#if !DEV_BUILD
using System.Reflection;

[assembly: AssemblyVersion("0.9.4")]
[assembly: AssemblyFileVersion("0.9.4")]
#endif

namespace ClientPlugin;

// ReSharper disable once UnusedType.Global
public class Plugin : IPlugin
{
    public const string Name = "MultigridProjector";

    public static Plugin Instance { get; private set; }

    private SettingsGenerator settingsGenerator;

    private static Harmony Harmony => new Harmony("com.spaceengineers.multigridprojector");

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    public void Init(object gameInstance)
    {
        Instance = this;
        settingsGenerator = new SettingsGenerator();

        PluginLog.Logger = new PluginLogger();

        // Expose the client configuration to shared code through the shared interface
        MultigridProjector.Config.PluginConfig.Current = Config.Current;

        PluginLog.Info("Loading client plugin");

        var isOldDotNetFramework = Environment.Version.Major < 5;
        if (isOldDotNetFramework &&
            Environment.GetEnvironmentVariable("SE_PLUGIN_DISABLE_METHOD_VERIFICATION") == null &&
            !WineDetector.IsRunningInWineOrProton())
        {
            // It will throw NotSupportedException if the game code has changed,
            // Pulsar will catch this and show "Error" next to the plugin
            EnsureOriginal.VerifyAll();
        }

        PatchHelpers.PatchAll(Harmony);

        PluginLog.Info("Client plugin loaded");
    }

    // This is invoked by Pulsar
    // ReSharper disable once UnusedMember.Global
    public void OpenConfigDialog()
    {
        settingsGenerator.SetLayout<Simple>();
        MyGuiSandbox.AddScreen(settingsGenerator.Dialog);
    }

    public void Dispose()
    {
        // NOTE: Unpatching caused problems for other plugins, so just keeping the plugin installed
        // all the time, which is common practice with plugin loaders.
        PluginLog.Info("Unloaded client plugin");
        Instance = null;
    }

    public void Update()
    {
    }
}
