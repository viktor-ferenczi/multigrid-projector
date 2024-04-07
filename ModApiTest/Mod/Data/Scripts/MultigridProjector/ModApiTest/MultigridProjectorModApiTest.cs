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
namespace MultigridProjector.ModApiTest
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Projector), true)]
    // ReSharper disable once UnusedType.Global
    public class MultigridProjectorModApiTest : MyGameLogicComponent
    {
        private static bool mgpVersionLogged;

        private IMyProjector projector;
        private IMultigridProjectorApi mgp;

        private List<MyObjectBuilder_CubeGrid> gridBuilders;
        private readonly List<ulong> subgridStateHashes = new List<ulong>();

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            projector = Entity as IMyProjector;
            if (projector == null || projector.Closed)
                return;

            var agent = new MultigridProjectorModAgent();
            mgp = agent.Available ? (IMultigridProjectorApi) agent : new MultigridProjectorModShim(projector);

            Entity.NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        public override void Close()
        {
            (mgp as IDisposable)?.Dispose();

            mgp = null;
            projector = null;
        }

        public override void UpdateBeforeSimulation100()
        {
            if (projector == null || projector.Closed || projector.OwnerId == 0)
                return;

            if (!mgpVersionLogged)
            {
                MyAPIGateway.Utilities.ShowMessage("Multigrid Projector", mgp is MultigridProjectorModAgent ? $"Plugin v{mgp.Version}" : $"Shim v{mgp.Version}");
                mgpVersionLogged = true;
            }

            var currentGridBuilders = mgp.GetOriginalGridBuilders(projector.EntityId);
            if (currentGridBuilders != gridBuilders)
            {
                gridBuilders = currentGridBuilders;
                GatherBlueprintDetails();
                return;
            }

            if (gridBuilders != null)
                LogAndChatSubgridStateChanges(projector.EntityId);
        }

        private void LogAndChatSubgridStateChanges(long projectorEntityId)
        {
            var projectorName = $"{projector.BlockDefinition.SubtypeName} {projector.CustomName ?? projector.DisplayNameText ?? projector.DisplayName} [{projectorEntityId}]";
            var scanNumber = mgp.GetScanNumber(projectorEntityId);
            var subgridCount = mgp.GetSubgridCount(projectorEntityId);
            for (var subgridIndex = 0; subgridIndex < subgridCount && subgridIndex < subgridStateHashes.Count; subgridIndex++)
            {
                var currentStateHash = mgp.GetStateHash(projectorEntityId, subgridIndex);
                if (currentStateHash != subgridStateHashes[subgridIndex])
                {
                    subgridStateHashes[subgridIndex] = currentStateHash;
                    var message = $"{projectorName} scan #{scanNumber} subgrid #{subgridIndex} hash {currentStateHash:x16}";
                    MyLog.Default.WriteLineAndConsole(message);
                    MyAPIGateway.Utilities.ShowMessage("Multigrid Projector", message);
                }
            }
        }

        private void GatherBlueprintDetails()
        {
            var projectorEntityId = projector.EntityId;
            var projectorName = $"{projector.BlockDefinition.SubtypeName} {projector.CustomName ?? projector.DisplayNameText ?? projector.DisplayName} [{projectorEntityId}]";
            var projectingGridName = $"{projector.CubeGrid.CustomName ?? projector.CubeGrid.DisplayName} [{projector.CubeGrid.EntityId}]";

            subgridStateHashes.Clear();

            MyLog.Default.WriteLineAndConsole("==================================================================");
            MyLog.Default.WriteLineAndConsole($"Multigrid Projector Mod API Test");
            MyLog.Default.WriteLineAndConsole("==================================================================");
            MyLog.Default.WriteLineAndConsole(projectorName);
            MyLog.Default.WriteLineAndConsole($"Projecting grid: {projectingGridName}");

            var scanNumber = mgp.GetScanNumber(projectorEntityId);
            MyLog.Default.WriteLineAndConsole($"Scan number: {scanNumber}");

            if (gridBuilders == null || scanNumber == 0)
            {
                MyAPIGateway.Utilities.ShowMessage("Multigrid Projector", $"{projectorName}: no blueprint loaded or disabled");
                MyLog.Default.WriteLineAndConsole("==================================================================");
                return;
            }

            var subgridCount = mgp.GetSubgridCount(projectorEntityId);
            MyLog.Default.WriteLineAndConsole($"Blueprint loaded:");
            MyLog.Default.WriteLineAndConsole($"Subgrid count: {subgridCount}");
            MyLog.Default.WriteLineAndConsole($"Subgrids in blueprint: {gridBuilders.Count}");

            MyAPIGateway.Utilities.ShowMessage("Multigrid Projector", $"{projectorName}: blueprint of {subgridCount} subgrids");

            for (var subgridIndex = 0; subgridIndex < subgridCount; subgridIndex++)
            {
                MyLog.Default.WriteLineAndConsole("-------------------");
                MyLog.Default.WriteLineAndConsole($"Subgrid #{subgridIndex}");
                MyLog.Default.WriteLineAndConsole("-------------------");

                var previewGrid = mgp.GetPreviewGrid(projectorEntityId, subgridIndex);
                MyLog.Default.WriteLineAndConsole($"Preview grid: {previewGrid.CustomName ?? previewGrid.DisplayName} [{previewGrid.EntityId}]");

                var builtGrid = mgp.GetBuiltGrid(projectorEntityId, subgridIndex);
                MyLog.Default.WriteLineAndConsole(builtGrid == null ? "No built grid for this subgrid" : $"Built grid: {builtGrid.CustomName ?? builtGrid.DisplayName} [{builtGrid.EntityId}]");

                MyLog.Default.WriteLineAndConsole("");

                MyLog.Default.WriteLineAndConsole($"Base connections:");
                foreach (var pair in mgp.GetBaseConnections(projectorEntityId, subgridIndex))
                {
                    MyLog.Default.WriteLineAndConsole($"  {pair.Key} => #{pair.Value.GridIndex} @ {pair.Value.Position}");
                }

                MyLog.Default.WriteLineAndConsole("");

                MyLog.Default.WriteLineAndConsole($"Top connections:");
                foreach (var pair in mgp.GetTopConnections(projectorEntityId, subgridIndex))
                {
                    MyLog.Default.WriteLineAndConsole($"  {pair.Key} => #{pair.Value.GridIndex} @ {pair.Value.Position}");
                }

                MyLog.Default.WriteLineAndConsole("");

                var stateHash = mgp.GetStateHash(projectorEntityId, subgridIndex);
                MyLog.Default.WriteLineAndConsole($"State hash: 0x{stateHash:x16}ul");

                var isComplete = mgp.IsSubgridComplete(projectorEntityId, subgridIndex);
                MyLog.Default.WriteLineAndConsole($"Complete: {isComplete}");

                // Force printing the initial hash once (if not zero)
                subgridStateHashes.Add(0);

                var blockStates = new Dictionary<Vector3I, BlockState>();
                mgp.GetBlockStates(blockStates, projectorEntityId, subgridIndex, new BoundingBoxI(Vector3I.MinValue, Vector3I.MaxValue), ~0);

                if (blockStates.Count > 0)
                    MyLog.Default.WriteLineAndConsole($"First block state: {mgp.GetBlockState(projectorEntityId, subgridIndex, blockStates.Keys.First())}");

                MyLog.Default.WriteLineAndConsole($"Block states:");
                foreach (var pair in blockStates)
                {
                    MyLog.Default.WriteLineAndConsole($"  {pair.Key} => {pair.Value}");
                }
            }

            MyLog.Default.WriteLineAndConsole("-------------------");
            MyLog.Default.WriteLineAndConsole($"YAML representation");
            MyLog.Default.WriteLineAndConsole("-------------------");

            var yaml = mgp.GetYaml(projectorEntityId);
            MyLog.Default.WriteLineAndConsole(yaml);

            MyLog.Default.WriteLineAndConsole("==================================================================");
        }
    }
}