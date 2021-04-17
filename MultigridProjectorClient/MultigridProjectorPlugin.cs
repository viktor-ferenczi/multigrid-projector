using System;
using HarmonyLib;
using MultigridProjector.Logic;
using MultigridProjector.Utilities;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using VRage.Plugins;

namespace MultigridProjectorClient
{
    // ReSharper disable once UnusedType.Global
    public class MultigridProjectorPlugin : IPlugin
    {
        private static Harmony Harmony => new Harmony("com.spaceengineers.multigridprojector");

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

                Harmony.PatchAll();

                MySession.OnLoading += OnLoadSession;
                // MySession.OnUnloading += OnUnloading;
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
            MySession.OnLoading -= OnLoadSession;
            // MySession.OnUnloading -= OnUnloading;

            // NOTE: Unpatching caused problems for other plugins, so just keeping the plugin installed all the time, which is common practice with Plugin Loader
            // PluginLog.Info("Unloading the Multigrid Projector Client Plugin");
            // _harmony.UnpatchAll();

            PluginLog.Info("Unloaded client plugin");
        }

        private void OnLoadSession()
        {
            MyAPIGateway.Utilities.RegisterMessageHandler(MultigridProjectorApiProvider.ModApiRequestId, HandleModApiRequest);
        }

        // private void OnUnloading()
        // {
        //     MyAPIGateway.Utilities.UnregisterMessageHandler(MultigridProjectorApiProvider.ModApiRequestId, HandleModApiRequest);
        // }

        private void HandleModApiRequest(object obj)
        {
            try
            {
                MyAPIGateway.Utilities.SendModMessage(MultigridProjectorApiProvider.ModApiResponseId, MultigridProjectorApiProvider.ModApi);
            }
            catch (Exception e)
            {
                PluginLog.Error(e, "Failed to respond to Mod API request");
            }
        }

        public void Update()
        {
        }
    }
}