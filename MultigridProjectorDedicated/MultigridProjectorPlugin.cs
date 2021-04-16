using System;
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
            PluginLog.Logger = new PluginLogger();

            PluginLog.Info("Loading");
            try
            {
                try
                {
                    EnsureOriginal.VerifyAll();
                }
                catch (NotSupportedException e)
                {
                    PluginLog.Error("Found incompatible code changes in the game or plugin patch collisions. Please report the exception below on the SE Mods Discord (invite is on the Workshop page):");
                    throw;
                }

                Harmony.PatchAll();
            }
            catch (Exception e)
            {
                PluginLog.Error(e, "Failed to load");
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