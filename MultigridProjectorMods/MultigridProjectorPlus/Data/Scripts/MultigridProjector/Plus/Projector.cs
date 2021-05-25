using System.Collections.Generic;
using System.IO;
using System.Linq;
using VRage.Game.Components;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;

// ReSharper disable once CheckNamespace
namespace MultigridProjector.Plus
{
    // ReSharper disable once UnusedType.Global
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Projector), true)]
    public class ProjectorLogicComponent : MyGameLogicComponent
    {
        private static bool initialized;
        private static readonly List<IMyProjector> Projectors = new List<IMyProjector>();
        private static readonly List<string> Blueprints = new List<string>();

        private static bool IsWorking(IMyTerminalBlock block) => IsValid(block) && block.IsWorking;
        private static bool IsValid(IMyTerminalBlock block) => block.CubeGrid?.Physics != null;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);

            if (initialized)
                return;

            initialized = true;

            CreateLoadRepairProjectionButton();

            Entity.NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        private static void CreateLoadRepairProjectionButton()
        {
            var btnBuild = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyProjector>("LoadRepairProjection");
            btnBuild.Enabled = IsWorking;
            btnBuild.Visible = IsValid;
            btnBuild.SupportsMultipleBlocks = false;
            btnBuild.Title = MyStringId.GetOrCompute("Load Repair Projection");
            btnBuild.Action = LoadRepairProjection;
            btnBuild.Tooltip = MyStringId.GetOrCompute("Loads the projector's own grid as a repair projection.");
            MyAPIGateway.TerminalControls.AddControl<IMyProjector>(btnBuild);
        }

        private static void LoadRepairProjection(IMyTerminalBlock block)
        {
            var projector = block as IMyProjector;
            if (projector == null)
                return;

            var grids = new List<IMyCubeGrid>();
            MyAPIGateway.GridGroups.GetGroup(projector.CubeGrid, GridLinkTypeEnum.Mechanical, grids);

            grids.Remove(projector.CubeGrid);
            grids.Insert(0, projector.CubeGrid);

            var gridBuilders = grids.Select(grid => grid.GetObjectBuilder()).Cast<MyObjectBuilder_CubeGrid>().ToArray();

            var bp = new MyObjectBuilder_ShipBlueprintDefinition
            {
                Id = new SerializableDefinitionId
                {
                    TypeId = new MyObjectBuilderType(typeof(MyObjectBuilder_ShipBlueprintDefinition)),
                    SubtypeName = projector.CubeGrid.CustomName ?? projector.CubeGrid.DisplayName ?? projector.CubeGrid.Name,
                },
                DisplayName = MyAPIGateway.Session.Player.DisplayName,
                OwnerSteamId = MyAPIGateway.Session.Player.SteamUserId,
                CubeGrids = gridBuilders,
            };

            var definitions = new MyObjectBuilder_Definitions {ShipBlueprints = new[] {bp}};

            var path = Path.Combine(MyAPIGateway.Utilities.GamePaths.UserDataPath, "Storage", MyAPIGateway.Utilities.GamePaths.ModScopeName, "RepairBlueprints", $"{projector.CubeGrid.EntityId}.sbc");
            definitions.Save(path);

            Projectors.Add(projector);
            Blueprints.Add(path);
        }

        public override void UpdateAfterSimulation100()
        {
            if (Projectors.Count == 0)
                return;

            var projector = Projectors.Pop();
            var path = Blueprints.Pop();

            if (IsWorking(projector))
                projector.LoadBlueprint(path);
        }
    }
}