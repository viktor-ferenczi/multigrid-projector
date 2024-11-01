using System;
using System.Reflection;
using HarmonyLib;
using MultigridProjector.Utilities;
using MultigridProjectorServer.MultigridProjector.Utilities;
using VRage.Plugins;

namespace MultigridProjectorDedicated
{
    // ReSharper disable once UnusedType.Global
    public class MultigridProjectorPlugin : IPlugin
    {
        private static Harmony Harmony => new Harmony("com.spaceengineers.multigridprojector");

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public void Init(object gameInstance)
        {
            PluginLog.Logger = new PluginLogger();

            PluginLog.Info("Loading");
            try
            {
                var isOldDotNetFramework = Environment.Version.Major < 5;
                if (isOldDotNetFramework && !WineDetector.IsRunningInWineOrProton())
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
                }

                Harmony.PatchAll(Assembly.GetExecutingAssembly());
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