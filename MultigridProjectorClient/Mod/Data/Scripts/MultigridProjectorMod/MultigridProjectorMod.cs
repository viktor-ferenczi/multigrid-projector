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
        private static MultigridProjectorModAgent _mgp;
        private static MultigridProjectorModAgent Mgp => _mgp ?? (_mgp = new MultigridProjectorModAgent());
        private static bool MgpVersionLogged;
        
        IMyProjector _projector;
        private List<MyObjectBuilder_CubeGrid> _gridBuilders;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            _projector = Entity as IMyProjector;
            if (_projector == null)
                return;

            if (_projector.Closed)
                return;

            Entity.NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        public override void Close()
        {
            _projector = null;
            base.Close();
        }

        public override void UpdateBeforeSimulation100()
        {
            if (!MgpVersionLogged)
            {
                MgpVersionLogged = true;
                MyAPIGateway.Utilities.ShowMessage("MGP", Mgp.Available ? $"Plugin v{Mgp.Version}" : $"Plugin not available");
            }

            if (!Mgp.Available)
                return;

            if (_projector == null || _projector.Closed || _projector.OwnerId == 0)
                return;

            var projectorEntityId = _projector.EntityId;
            var gridBuilders = Mgp.GetOriginalGridBuilders(projectorEntityId);
            if (gridBuilders == _gridBuilders) 
                return;
            
            _gridBuilders = gridBuilders;
            LogBlueprintDetails(projectorEntityId, gridBuilders);
        }
        
        private void LogBlueprintDetails(long projectorEntityId, List<MyObjectBuilder_CubeGrid> gridBuilders)
        {
            MyLog.Default.WriteLineAndConsole("==================================================================");
            MyLog.Default.WriteLineAndConsole($"MultigridProjectorMod: {_projector.BlockDefinition.SubtypeName} {_projector.DisplayName} [{projectorEntityId}] blueprint info");
            MyLog.Default.WriteLineAndConsole("==================================================================");
            MyLog.Default.WriteLineAndConsole($"Projecting grid: {_projector.CubeGrid.DisplayName} [{_projector.CubeGrid.EntityId}]");

            if (gridBuilders == null)
            {
                MyLog.Default.WriteLineAndConsole($"No blueprint loaded.");
                MyLog.Default.WriteLineAndConsole("==================================================================");
                return;
            }

            MyLog.Default.WriteLineAndConsole($"Blueprint loaded:");
            
            var subgridCount = Mgp.GetSubgridCount(projectorEntityId);
            MyLog.Default.WriteLineAndConsole($"Subgrid count: {subgridCount}");
            MyLog.Default.WriteLineAndConsole($"Subgrids in blueprint: {gridBuilders.Count}");

            for (var subgridIndex = 0; subgridIndex < subgridCount; subgridIndex++)
            {
                MyLog.Default.WriteLineAndConsole("---------------");
                MyLog.Default.WriteLineAndConsole($"Subgrid #{subgridIndex}");
                MyLog.Default.WriteLineAndConsole("---------------");

                var previewGrid = Mgp.GetPreviewGrid(projectorEntityId, subgridIndex);
                MyLog.Default.WriteLineAndConsole($"Preview grid: {previewGrid.DisplayName} [{previewGrid.EntityId}]");

                var builtGrid = Mgp.GetBuiltGrid(projectorEntityId, subgridIndex);
                MyLog.Default.WriteLineAndConsole(builtGrid == null ? "No built grid for this subgrid" : $"Built grid: {builtGrid.DisplayName} [{builtGrid.EntityId}]");

                MyLog.Default.WriteLineAndConsole("");

                MyLog.Default.WriteLineAndConsole($"Base connections:");
                foreach (var pair in Mgp.GetBaseConnections(projectorEntityId, subgridIndex))
                {
                    MyLog.Default.WriteLineAndConsole($"  {pair.Key} => #{pair.Value.GridIndex} @ {pair.Value.Position}");
                }

                MyLog.Default.WriteLineAndConsole("");

                MyLog.Default.WriteLineAndConsole($"Top connections:");
                foreach (var pair in Mgp.GetTopConnections(projectorEntityId, subgridIndex))
                {
                    MyLog.Default.WriteLineAndConsole($"  {pair.Key} => #{pair.Value.GridIndex} @ {pair.Value.Position}");
                }
                
                MyLog.Default.WriteLineAndConsole("");

                var blockStates = new Dictionary<Vector3I, BlockState>();
                Mgp.GetBlockStates(blockStates, projectorEntityId, subgridIndex, new BoundingBoxI(Vector3I.MinValue, Vector3I.MaxValue), ~0);

                if (blockStates.Count > 0)
                    MyLog.Default.WriteLineAndConsole($"First block state: {Mgp.GetBlockState(projectorEntityId, subgridIndex, blockStates.Keys.First())}");

                MyLog.Default.WriteLineAndConsole($"Block states:");
                foreach (var pair in blockStates)
                {
                    MyLog.Default.WriteLineAndConsole($"  {pair.Key} => {pair.Value}");
                }

            }

            MyLog.Default.WriteLineAndConsole("==================================================================");
        }
    }
}