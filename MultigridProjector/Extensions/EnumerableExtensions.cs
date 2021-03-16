using System.Collections.Generic;
using System.Linq;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
using VRage.Game;
using VRageMath;

namespace MultigridProjector.Extensions
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<(int Index, T Value)> Enumerate<T>(this IEnumerable<T> coll)
        {
            var index = 0;
            foreach (var value in coll)
                yield return (index++, value);
        }

        // Clones a blueprint (list of mechanically connected grids)
        public static List<MyObjectBuilder_CubeGrid> Clone(this IEnumerable<MyObjectBuilder_CubeGrid> gridBuilders)
        {
            return gridBuilders
                .Select(g => g.Clone())
                .Cast<MyObjectBuilder_CubeGrid>()
                .ToList();
        }

        // Need to zero out the position of the first subgrid in the blueprint, because it is damaged (zeroed out)
        // somewhere during client-server communication
        public static void NormalizeBlueprintPositionAndOrientation(this IEnumerable<MyObjectBuilder_CubeGrid> gridBuilders)
        {
            var correction = MatrixD.Identity;
            foreach (var (gridIndex, gridBuilder) in gridBuilders.Enumerate())
            {
                if (!gridBuilder.PositionAndOrientation.HasValue) continue;

                if (gridIndex == 0)
                {
                    gridBuilder.PositionAndOrientation.Value.ToMatrixD(out var firstWm);
                    correction = MatrixD.Invert(firstWm);
                }

                gridBuilder.PositionAndOrientation.Value.ToMatrixD(out var wm);
                gridBuilder.PositionAndOrientation = (wm * correction).ToPositionAndOrientation();
            }
        }

        public static void SetProjector(this IEnumerable<MyCubeGrid> grids, MyProjectorBase projector)
        {
            foreach (var grid in grids)
                grid.Projector = projector;
        }

        public static long GetBlockCount(this IEnumerable<MyObjectBuilder_CubeGrid> grids)
        {
            return grids.Sum(g => (long) g.CubeBlocks.Count);
        }

        public static bool TryCalculatePcu(this IEnumerable<MyObjectBuilder_CubeGrid> gridBuilders, out long totalPcu, out int unknownBlockCount)
        {
            totalPcu = 0;
            unknownBlockCount = 0;
            foreach (var builder in gridBuilders)
            {
                foreach (var block in builder.CubeBlocks)
                {
                    var blockDefinition = MyDefinitionManager.Static.GetCubeBlockDefinition(block);
                    if (blockDefinition == null)
                    {
                        unknownBlockCount++;
                        continue;
                    }
                    totalPcu += blockDefinition.PCU;
                }
            }
            return unknownBlockCount == 0;
        }

        public static void PrepareForProjection(this List<MyObjectBuilder_CubeGrid> gridBuilders)
        {
            foreach (var gridBuilder in gridBuilders)
                gridBuilder.PrepareForProjection();
        }

        public static void PrepareForConsoleProjection(this IEnumerable<MyObjectBuilder_CubeGrid> grids, MyProjectorClipboard clipboard)
        {
            foreach (var grid in grids)
                clipboard.ProcessCubeGrid(grid);
        }
    }
}