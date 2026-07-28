using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MultigridProjector.Utilities;
using MultigridProjectorDedicated;
using MultigridProjectorServer.MultigridProjector.Utilities;
using Sandbox;
using VRage.FileSystem;
using VRage.Plugins;
using ConfigStorage = PluginSdk.Config.ConfigStorage;

// Define assembly version when compiled by Magnetar
#if !DEV_BUILD
using System.Reflection;

[assembly: AssemblyVersion("0.9.5")]
[assembly: AssemblyFileVersion("0.9.5")]
#endif

namespace ServerPlugin;

// ReSharper disable once UnusedType.Global
public class Plugin : IPlugin
{
    public const string Name = "MultigridProjector";

    public static Plugin Instance { get; private set; }

    private const string HarmonyId = "com.spaceengineers.multigridprojector";
    private static Harmony Harmony => new Harmony(HarmonyId);

    // Server configuration. Loaded early (before world load) from EarlyStartup, so it is held in a
    // static field that does not depend on the IPlugin instance existing yet. Exposed as an instance
    // property so Quasar's remote config UI can still discover it on the plugin instance.
    // ReSharper disable once UnusedMember.Global
    public PluginConfig Config => config;
    private static PluginConfig config;
    private static string configPath;
    private static readonly string ConfigFileName = "MultigridProjector.cfg";

    private static bool earlyStarted;
    private static bool failed;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Init(object gameInstance)
    {
        Instance = this;

        // On the dedicated server the world (including grids that build from projectors) is loaded
        // before IPlugin.Init runs, so the patches are normally applied much earlier, bootstrapped
        // from the Preloader's Finish() hook (see InstallEarlyBootstrap). Run EarlyStartup here as a
        // fallback for the case the preloader path did not execute (e.g. Magnetar safe mode).
        // Idempotent: a no-op if it already ran from the InvokeBeforeRun hook.
        var alreadyAppliedEarly = earlyStarted;
        EarlyStartup();
        if (failed)
            return;

        if (!alreadyAppliedEarly)
            PluginLog.Warn("Patches were applied late, from IPlugin.Init, because the early bootstrap did not run (expected only in Magnetar safe mode). Functionality relying on projectors during world load may not work for this session.");

        // Apply any patches deferred to the "Late" category. By now the world/session has loaded, so
        // their target assemblies would be available. Currently no patch uses this category, so this
        // applies zero patches; it keeps the two-phase mechanism in place for future patches whose
        // target assembly is not loaded yet at the early bootstrap point.
        try
        {
            PatchHelpers.PatchCategory(Harmony, PatchHelpers.LateCategory);
        }
        catch (Exception e)
        {
            failed = true;
            PluginLog.Error(e, "Failed to apply late patches");
            return;
        }

        PluginLog.Info("Loaded");
    }

    // Called from the Preloader's Finish() hook, before the game starts. Installs a Harmony postfix
    // on MyInitializer.InvokeBeforeRun so the patches are applied as soon as the game has finished
    // its core initialization — still well before world-load compilation.
    //
    // InvokeBeforeRun is the earliest safe trigger: its body calls InitFileSystem (so
    // MyFileSystem.UserDataPath becomes available) AND assigns MyLog.Default (so the game logger
    // works). A postfix therefore runs with both ready. This method itself runs even earlier, before
    // MyLog.Default exists, so it cannot use PluginLog; it falls back to the console for the rare
    // failure cases.
    // ReSharper disable once UnusedMember.Global
    public static void InstallEarlyBootstrap()
    {
        try
        {
            var target = AccessTools.Method(typeof(MyInitializer), nameof(MyInitializer.InvokeBeforeRun));
            if (target == null)
            {
                Console.WriteLine("Multigrid Projector: Early bootstrap could not find MyInitializer.InvokeBeforeRun; patches will be applied later, from Init");
                return;
            }

            var postfix = new HarmonyMethod(AccessTools.Method(typeof(Plugin), nameof(OnGameInitialized)));
            new Harmony($"{HarmonyId}.bootstrap").Patch(target, postfix: postfix);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Multigrid Projector: Early bootstrap failed to install the MyInitializer.InvokeBeforeRun hook: {e}");
        }
    }

    // Harmony postfix on MyInitializer.InvokeBeforeRun. Public so Harmony can resolve it;
    // intentionally NOT decorated with [HarmonyPatch], so the patch scan never re-applies it. Runs
    // once the game's filesystem and logging are ready, but before any world is loaded (and
    // therefore before grids build from projectors during load). EarlyStartup catches its own
    // exceptions, so this postfix never throws back into the game's startup.
    // ReSharper disable once UnusedMember.Global
    public static void OnGameInitialized()
    {
        EarlyStartup();
    }

    // One-shot early initialization: set up the logger, load config, publish it to shared code,
    // verify the patched game methods are unchanged, then apply the uncategorized patches — all
    // before world-load compilation. Called from the InvokeBeforeRun postfix (normal dedicated
    // server path) and from Init (fallback). Both run on the main thread, so a plain flag keeps it
    // one-shot. NoInlining: EnsureOriginal.VerifyAll() resolves the plugin assembly from this
    // method's stack frame, so it must remain a distinct frame.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void EarlyStartup()
    {
        if (earlyStarted)
            return;
        earlyStarted = true;

        try
        {
            // The game logger is ready by now (InvokeBeforeRun assigned MyLog.Default), so PluginLog
            // works from here on.
            PluginLog.Logger = new PluginLogger();
            PluginLog.Info("Loading");

            // UserDataPath is available now (InvokeBeforeRun called InitFileSystem).
            configPath = Path.Combine(MyFileSystem.UserDataPath, ConfigFileName);
            config = LoadConfig(configPath);
            config.PropertyChanged += OnConfigPropertyChanged;

            // Expose the server configuration to shared code through the shared interface, so the
            // patches read the configured values from the moment they run (including during world
            // load), not the default that PluginConfig.Current holds until now.
            MultigridProjector.Config.PluginConfig.Current = config;

            var isOldDotNetFramework = Environment.Version.Major < 5;
            if (isOldDotNetFramework && !WineDetector.IsRunningInWineOrProton())
            {
                try
                {
                    EnsureOriginal.VerifyAll();
                }
                catch (NotSupportedException e)
                {
                    failed = true;
                    PluginLog.Error(e, "Disabled the plugin due to potentially incompatible code changes in the game or plugin patch collisions. Please report the exception below on the SE Mods Discord (invite is on the Workshop page):");
                    return;
                }
            }

            // Apply the uncategorized patches now, before world-load compilation. Any patch carrying
            // the "Late" category is deferred to Init (currently none).
            PatchHelpers.PatchUncategorized(Harmony);
        }
        catch (Exception e)
        {
            failed = true;
            if (PluginLog.Logger != null)
                PluginLog.Error(e, "Early startup failed");
            else
                Console.WriteLine($"Multigrid Projector: Early startup failed: {e}");
        }
    }

    public void Dispose()
    {
        try
        {
            if (config != null)
            {
                config.PropertyChanged -= OnConfigPropertyChanged;
                SaveConfig();
            }
        }
        catch (Exception e)
        {
            PluginLog.Error(e, "Dispose failed");
        }

        if (PluginLog.Logger != null)
        {
            PluginLog.Info("Unloaded");
            PluginLog.Logger = null;
        }

        Instance = null;
    }

    public void Update()
    {
    }

    private static PluginConfig LoadConfig(string path)
    {
        try
        {
            var loaded = ConfigStorage.LoadXml<PluginConfig>(path);
            ConfigStorage.SaveXml(loaded, path);
            return loaded;
        }
        catch (Exception ex)
        {
            PluginLog.Error(ex, $"Failed to load configuration file: {path}");

            var cfg = new PluginConfig();
            try
            {
                ConfigStorage.SaveXml(cfg, path);
            }
            catch (Exception)
            {
                // Ignored
            }

            return cfg;
        }
    }

    private static void SaveConfig()
    {
        if (config != null && configPath != null)
            ConfigStorage.SaveXml(config, configPath);
    }

    private static void OnConfigPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        try
        {
            SaveConfig();
        }
        catch (Exception ex)
        {
            PluginLog.Error(ex, $"Failed to save configuration file: {configPath}");
        }
    }
}
