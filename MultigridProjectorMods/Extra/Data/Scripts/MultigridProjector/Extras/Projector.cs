using System.Collections.Generic;
using System.IO;
using System.Linq;
using VRage.Game.Components;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;

// ReSharper disable once CheckNamespace
namespace MultigridProjector.Extra
{
    // ReSharper disable once UnusedType.Global
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Projector), false)]
    public class ProjectorLogicComponent : MyGameLogicComponent
    {
        private static volatile bool initialized;

        private static bool IsWorkingButNotProjecting(IMyTerminalBlock block) => IsValid(block) && block.IsWorking && (block as IMyProjector)?.IsProjecting == false;
        private static bool IsWorking(IMyTerminalBlock block) => IsValid(block) && block.IsWorking;
        private static bool IsValid(IMyTerminalBlock block) => block.CubeGrid?.Physics != null;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);

            if (initialized)
                return;

            initialized = true;

            CreateLoadRepairProjectionButton();

            // Entity.NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        private static void CreateLoadRepairProjectionButton()
        {
            var btnBuild = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyProjector>("LoadRepairProjection");
            btnBuild.Enabled = IsWorking;
            btnBuild.Visible = IsWorkingButNotProjecting;
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

            var filename = $"{projector.CubeGrid.EntityId}.sbc";
            var absolutePath = Path.Combine(MyAPIGateway.Utilities.GamePaths.UserDataPath, "Storage", MyAPIGateway.Utilities.GamePaths.ModScopeName, filename);
            definitions.Save(absolutePath);

            projector.LoadBlueprint(absolutePath);

            MyAPIGateway.Utilities.DeleteFileInLocalStorage(filename, typeof(ProjectorLogicComponent));
            MyAPIGateway.Utilities.DeleteFileInLocalStorage(filename + "B5", typeof(ProjectorLogicComponent));
        }
    }
}