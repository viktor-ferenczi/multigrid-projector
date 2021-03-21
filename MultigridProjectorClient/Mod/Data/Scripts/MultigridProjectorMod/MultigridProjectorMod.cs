using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.Components;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRage.ObjectBuilders;
using VRage.ModAPI;
using MultigridProjector.Api;
using VRage.Game;
using VRage.Utils;
using VRageMath;

// ReSharper disable once CheckNamespace
namespace MultigridProjectorMod
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Projector), true)]
    // ReSharper disable once UnusedType.Global
    public class MultigridProjectorMod : MyGameLogicComponent
    {
        IMyProjector _projector;
        private MultigridProjectorModAgent _mgp;
        private bool _projectorLogged;
        private List<MyObjectBuilder_CubeGrid> _gridBuilders;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            _projector = Entity as IMyProjector;
            if (_projector == null)
            {
                MyLog.Default.WriteLineAndConsole($"MultigridProjectorMod: No projector");
                return;
            }

            if (_projector.Closed)
            {
                MyLog.Default.WriteLineAndConsole($"MultigridProjectorMod: Projector {_projector.DisplayName} [{_projector.EntityId}] skipped because it is closed");
                return;
            }

            MyLog.Default.WriteLineAndConsole($"MultigridProjectorMod: Projector {_projector.DisplayName} [{_projector.EntityId}] registered");

            _mgp = new MultigridProjectorModAgent();

            Entity.NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        public override void Close()
        {
            if(_projector == null)
                return;

            MyLog.Default.WriteLineAndConsole($"MultigridProjectorMod: Projector {_projector.DisplayName} [{_projector.EntityId}] closed");

            _mgp = null;
            
            _projector = null;

            base.Close();
        }

        public override void UpdateBeforeSimulation100()
        {
            if (_projector == null || _mgp == null || !_mgp.Available)
                return;
            
            var projectorEntityId = _projector.EntityId;
            
            if (!_projectorLogged)
            {
                _projectorLogged = true;
                LogProjector(projectorEntityId);
            }

            var gridBuilders = _mgp.GetOriginalGridBuilders(projectorEntityId);
            if (gridBuilders != _gridBuilders)
            {
                _gridBuilders = gridBuilders;
                LogBlueprintDetails(projectorEntityId, gridBuilders);
            }
        }

        private void LogProjector(long projectorEntityId)
        {
            MyLog.Default.WriteLineAndConsole($"MultigridProjectorMod: Projector {_projector.DisplayName} [{projectorEntityId}] MGP API connection is available");
            MyAPIGateway.Utilities.ShowMessage("MGP", $"Client plugin version {_mgp.Version}");
            MyAPIGateway.Utilities.ShowMessage("MGP", $"Projector {_projector.DisplayName} [{projectorEntityId}] is usable with multigrid blueprints!");
        }

        private void LogBlueprintDetails(long projectorEntityId, List<MyObjectBuilder_CubeGrid> gridBuilders)
        {
            MyLog.Default.WriteLineAndConsole("==================================================================");
            MyLog.Default.WriteLineAndConsole($"MultigridProjectorMod: Projector {_projector.DisplayName} [{projectorEntityId}]:");
            MyLog.Default.WriteLineAndConsole("==================================================================");
            MyLog.Default.WriteLineAndConsole($"Projector's grid ID: {_projector.CubeGrid?.EntityId}");

            if (gridBuilders == null)
            {
                MyLog.Default.WriteLineAndConsole($"No blueprint loaded.");
                MyLog.Default.WriteLineAndConsole("==================================================================");
                return;
            }

            MyLog.Default.WriteLineAndConsole($"Blueprint loaded:");
            
            var subgridCount = _mgp.GetSubgridCount(projectorEntityId);
            MyLog.Default.WriteLineAndConsole($"Subgrid count: {subgridCount}");
            MyLog.Default.WriteLineAndConsole($"Subgrids in blueprint: {gridBuilders.Count}");

            for (var subgridIndex = 0; subgridIndex < subgridCount; subgridIndex++)
            {
                MyLog.Default.WriteLineAndConsole("-------------------------");
                MyLog.Default.WriteLineAndConsole($"Subgrid #{subgridIndex}");
                MyLog.Default.WriteLineAndConsole("-------------------------");

                var previewGrid = _mgp.GetPreviewGrid(projectorEntityId, 0);
                MyLog.Default.WriteLineAndConsole($"Preview grid ID: {previewGrid?.EntityId}");

                var builtGrid = _mgp.GetBuiltGrid(projectorEntityId, 0);
                MyLog.Default.WriteLineAndConsole(builtGrid == null ? "No built grid for this subgrid" : $"Built grid ID: {builtGrid.EntityId}");

                MyLog.Default.WriteLineAndConsole("---");

                var blockStates = new Dictionary<Vector3I, BlockState>();
                _mgp.GetBlockStates(blockStates, projectorEntityId, 0, new BoundingBoxI(Vector3I.MinValue, Vector3I.MaxValue), ~0);

                if (blockStates.Count > 0)
                    MyLog.Default.WriteLineAndConsole($"First block state: {_mgp.GetBlockState(projectorEntityId, 0, blockStates.Keys.First())}");

                MyLog.Default.WriteLineAndConsole($"Block states:");
                foreach (var pair in blockStates)
                {
                    MyLog.Default.WriteLineAndConsole($"  {pair.Key} => {pair.Value}");
                }

                MyLog.Default.WriteLineAndConsole("---");

                MyLog.Default.WriteLineAndConsole($"Base connections:");
                foreach (var pair in _mgp.GetBaseConnections(projectorEntityId, 0))
                {
                    MyLog.Default.WriteLineAndConsole($"  {pair.Key} => #{pair.Value.GridIndex} @ {pair.Value.Position}");
                }

                MyLog.Default.WriteLineAndConsole("---");

                MyLog.Default.WriteLineAndConsole($"Base connections:");
                foreach (var pair in _mgp.GetTopConnections(projectorEntityId, 0))
                {
                    MyLog.Default.WriteLineAndConsole($"  {pair.Key} => #{pair.Value.GridIndex} @ {pair.Value.Position}");
                }
            }

            MyLog.Default.WriteLineAndConsole("==================================================================");
        }
    }
}