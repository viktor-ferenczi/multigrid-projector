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
        private static MultigridProjectorModAgent mgp;
        private static MultigridProjectorModAgent Mgp => mgp ?? (mgp = new MultigridProjectorModAgent());
        private static bool mgpVersionLogged;

        private IMyProjector projector;
        private List<MyObjectBuilder_CubeGrid> gridBuilders;
        private readonly List<ulong> subgridStateHashes = new List<ulong>();

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            projector = Entity as IMyProjector;
            if (projector == null)
                return;

            if (projector.Closed)
                return;

            Entity.NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        public override void Close()
        {
            projector = null;
        }

        public override void UpdateBeforeSimulation100()
        {
            if (!mgpVersionLogged)
            {
                mgpVersionLogged = true;
                MyAPIGateway.Utilities.ShowMessage("Multigrid Projector", Mgp.Available ? $"Plugin v{Mgp.Version}" : $"Plugin is not available");
            }

            if (!Mgp.Available)
                return;

            if (projector == null || projector.Closed || projector.OwnerId == 0)
                return;

            var currentGridBuilders = Mgp.GetOriginalGridBuilders(projector.EntityId);
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
            var scanNumber = Mgp.GetScanNumber(projectorEntityId);
            var subgridCount = Mgp.GetSubgridCount(projectorEntityId);
            for (var subgridIndex = 0; subgridIndex < subgridCount && subgridIndex < subgridStateHashes.Count; subgridIndex++)
            {
                var currentStateHash = Mgp.GetStateHash(projectorEntityId, subgridIndex);
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

            var scanNumber = Mgp.GetScanNumber(projectorEntityId);
            MyLog.Default.WriteLineAndConsole($"Scan number: {scanNumber}");

            if (gridBuilders == null || scanNumber == 0)
            {
                MyAPIGateway.Utilities.ShowMessage("Multigrid Projector", $"{projectorName}: no blueprint loaded or disabled");
                MyLog.Default.WriteLineAndConsole("==================================================================");
                return;
            }

            var subgridCount = Mgp.GetSubgridCount(projectorEntityId);
            MyLog.Default.WriteLineAndConsole($"Blueprint loaded:");
            MyLog.Default.WriteLineAndConsole($"Subgrid count: {subgridCount}");
            MyLog.Default.WriteLineAndConsole($"Subgrids in blueprint: {gridBuilders.Count}");

            MyAPIGateway.Utilities.ShowMessage("Multigrid Projector", $"{projectorName}: blueprint of {subgridCount} subgrids");

            for (var subgridIndex = 0; subgridIndex < subgridCount; subgridIndex++)
            {
                MyLog.Default.WriteLineAndConsole("-------------------");
                MyLog.Default.WriteLineAndConsole($"Subgrid #{subgridIndex}");
                MyLog.Default.WriteLineAndConsole("-------------------");

                var previewGrid = Mgp.GetPreviewGrid(projectorEntityId, subgridIndex);
                MyLog.Default.WriteLineAndConsole($"Preview grid: {previewGrid.CustomName ?? previewGrid.DisplayName} [{previewGrid.EntityId}]");

                var builtGrid = Mgp.GetBuiltGrid(projectorEntityId, subgridIndex);
                MyLog.Default.WriteLineAndConsole(builtGrid == null ? "No built grid for this subgrid" : $"Built grid: {builtGrid.CustomName ?? builtGrid.DisplayName} [{builtGrid.EntityId}]");

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

                var stateHash = Mgp.GetStateHash(projectorEntityId, subgridIndex);
                MyLog.Default.WriteLineAndConsole($"State hash: 0x{stateHash:x16}ul");

                var isComplete = Mgp.IsSubgridComplete(projectorEntityId, subgridIndex);
                MyLog.Default.WriteLineAndConsole($"Complete: {isComplete}");

                // Force printing the initial hash once (if not zero)
                subgridStateHashes.Add(0);

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

            MyLog.Default.WriteLineAndConsole("-------------------");
            MyLog.Default.WriteLineAndConsole($"YAML representation");
            MyLog.Default.WriteLineAndConsole("-------------------");

            var yaml = Mgp.GetYaml(projectorEntityId);
            MyLog.Default.WriteLineAndConsole(yaml);

            MyLog.Default.WriteLineAndConsole("==================================================================");
        }
    }
}