﻿using System;
using HarmonyLib;
using MultigridProjector.Logic;
using MultigridProjector.Utilities;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Plugins;

namespace MultigridProjectorClient
{
    // ReSharper disable once UnusedType.Global
    public class MultigridProjectorPlugin : IPlugin
    {
        private readonly Harmony _harmony = new Harmony("com.spaceengineers.multigridprojector");

        public void Init(object gameInstance)
        {
            PluginLog.Logger = new PluginLogger();

            PluginLog.Info("Loading client plugin");
            try
            {
                EnsureOriginal.VerifyAll();
                _harmony.PatchAll();

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

        // ReSharper disable once UnusedType.Global
        [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
        public class MultigridProjectorSession : MySessionComponentBase
        {

            public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
            {
                if (MultigridProjection.Projections.Count != 0)
                    PluginLog.Warn($"{MultigridProjection.Projections.Count} projections are active on loading session, there should be none!");
            }

            protected override void UnloadData()
            {
                if (MultigridProjection.Projections.Count != 0)
                    PluginLog.Warn($"{MultigridProjection.Projections.Count} projections are active on unloading session, there should be none!");
            }

            public override void UpdateAfterSimulation()
            {
            }
        }
    }
}