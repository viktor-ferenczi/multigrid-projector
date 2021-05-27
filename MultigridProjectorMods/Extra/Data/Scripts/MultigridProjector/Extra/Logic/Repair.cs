using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;

// ReSharper disable once CheckNamespace
namespace MultigridProjector.Extra
{
    public static class Repair
    {
        public static void LoadMechanicalGroup(IMyTerminalBlock block)
        {
            var projector = block as IMyProjector;
            if (projector == null)
                return;

            var grids = CollectGrids(projector);
            var definitions = CreateBlueprint(projector, grids);
            LoadIntoProjector(projector, definitions);
        }

        private static List<IMyCubeGrid> CollectGrids(IMyProjector projector)
        {
            var grids = new List<IMyCubeGrid>();
            MyAPIGateway.GridGroups.GetGroup(projector.CubeGrid, GridLinkTypeEnum.Mechanical, grids);

            grids.Remove(projector.CubeGrid);
            grids.Insert(0, projector.CubeGrid);
            return grids;
        }

        private static MyObjectBuilder_Definitions CreateBlueprint(IMyProjector projector, List<IMyCubeGrid> grids)
        {
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
            return definitions;
        }

        private static void LoadIntoProjector(IMyProjector projector, MyObjectBuilder_Definitions definitions)
        {
            var filename = $"{projector.CubeGrid.EntityId}.sbc";
            try
            {
                var absolutePath = SaveBlueprint(filename, definitions);
                projector.LoadBlueprint(absolutePath);
            }
            finally
            {
                Cleanup(filename);
            }
        }

        private static string SaveBlueprint(string filename, MyObjectBuilder_Definitions definitions)
        {
            var absolutePath = Path.Combine(MyAPIGateway.Utilities.GamePaths.UserDataPath, "Storage", MyAPIGateway.Utilities.GamePaths.ModScopeName, filename);
            definitions.Save(absolutePath);
            return absolutePath;
        }

        private static void Cleanup(string filename)
        {
            MyAPIGateway.Utilities.DeleteFileInLocalStorage(filename, typeof(Repair));
            MyAPIGateway.Utilities.DeleteFileInLocalStorage(filename + "B5", typeof(Repair));
        }
    }
}