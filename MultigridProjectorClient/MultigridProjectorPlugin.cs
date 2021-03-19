using System;
using HarmonyLib;
using MultigridProjector.Logic;
using MultigridProjector.Utilities;
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
            // PluginLog.Info("Unloading the Multigrid Projector Client Plugin");
            // _harmony.UnpatchAll();
            PluginLog.Info("Unloaded client plugin");
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