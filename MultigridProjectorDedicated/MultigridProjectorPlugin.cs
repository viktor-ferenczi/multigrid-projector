﻿using System;
using HarmonyLib;
using MultigridProjector.Utilities;
using VRage.Plugins;

namespace MultigridProjectorDedicated
{
    // ReSharper disable once UnusedType.Global
    public class MultigridProjectorPlugin : IPlugin
    {
        private static Harmony Harmony => new Harmony("com.spaceengineers.multigridprojector");

        public void Init(object gameInstance)
        {
            PluginLog.Info("Loading");
            try
            {
                PluginLog.Logger = new PluginLogger();
                PluginLog.Prefix = "";

                EnsureOriginal.VerifyAll();
                Harmony.PatchAll();
            }
            catch (Exception e)
            {
                PluginLog.Error(e, "Failed to load server plugin");
                throw;
            }

            PluginLog.Info("Loaded");
        }

        public void Dispose()
        {
            if (PluginLog.Logger == null)
                return;

            PluginLog.Info("Unloaded");
            PluginLog.Logger = null;
        }

        public void Update()
        {
        }
    }
}