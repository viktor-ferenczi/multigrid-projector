﻿using System;
using System.Reflection;
using HarmonyLib;
using MultigridProjector.Utilities;
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
            try
            {
                try
                {
                    EnsureOriginal.VerifyAll();
                }
                catch (NotSupportedException e)
                {
                    PluginLog.Error(e, "Disabled the plugin due to potentially incompatible code changes in the game or plugin patch collisions. Please report the exception below on the SE Mods Discord (invite is on the Workshop page):");
                    return;
                }

                Harmony.PatchAll(Assembly.GetExecutingAssembly());
            }
            catch (Exception e)
            {
                PluginLog.Error(e, "Plugin initialization failed");
                throw;
            }
            PluginLog.Info("Client plugin loaded");
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