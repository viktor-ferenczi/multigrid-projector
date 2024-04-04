using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using HarmonyLib;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using VRageMath;

namespace MultigridProjector.Patches
{
    [HarmonyPatch(typeof(MyCubeGrid))]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "InvalidXmlDocComment")]
    public class MyCubeGridPatch
    {
        private static readonly PropertyInfo cacheNeighborBlocksField = AccessTools.Property(typeof(MyCubeGrid), "m_cacheNeighborBlocks");
        private static readonly PropertyInfo cacheMountPointsAField = AccessTools.Property(typeof(MyCubeGrid), "m_cacheMountPointsA");

        /// <summary>
        /// Performs check whether cube block given by its definition, rotation and position is connected to some other
        /// block in a given grid.
        /// </summary>
        /// <param name="grid">Grid in which the check is performed.</param>
        /// <param name="def"></param>
        /// <param name="mountPoints"></param>
        /// <param name="rotation">Rotation of the cube block within grid.</param>
        /// <param name="position">Position of the cube block within grid.</param>
        /// <returns>True when there is a connectable neighbor connected by a mount point, otherwise false.</returns>
        [HarmonyPrefix]
        [HarmonyPatch("CheckConnectivity")]
        public static bool CheckConnectivityPrefix(
            IMyGridConnectivityTest grid,
            MyCubeBlockDefinition def,
            MyCubeBlockDefinition.MountPoint[] mountPoints,
            ref Quaternion rotation,
            ref Vector3I position,
            out bool __result)
        {
            var m_cacheNeighborBlocks = (Dictionary<Vector3I, ConnectivityResult>)cacheNeighborBlocksField.GetValue(null);
            var m_cacheMountPointsA = (List<MyCubeBlockDefinition.MountPoint>)cacheMountPointsAField.GetValue(null);

            try
            {
                if (mountPoints == null)
                {
                    __result = false;
                    return false;
                }
                Vector3I center = def.Center;
                Vector3I size = def.Size;
                Vector3I.Transform(ref center, ref rotation, out Vector3I _);
                Vector3I.Transform(ref size, ref rotation, out Vector3I _);
                for (int index = 0; index < mountPoints.Length; ++index)
                {
                    MyCubeBlockDefinition.MountPoint mountPoint = mountPoints[index];
                    Vector3 vector3_1 = mountPoint.Start - (Vector3) center;
                    Vector3 vector3_2 = mountPoint.End - (Vector3) center;
                    if (MyFakes.ENABLE_TEST_BLOCK_CONNECTIVITY_CHECK)
                    {
                        Vector3 vector3_3 = Vector3.Min(mountPoint.Start, mountPoint.End);
                        Vector3 vector3_4 = Vector3.Max(mountPoint.Start, mountPoint.End);
                        Vector3I vector3I1 = Vector3I.One - Vector3I.Abs(mountPoint.Normal);
                        Vector3I vector3I2 = Vector3I.One - vector3I1;
                        Vector3 vector3_5 = vector3I2 * vector3_3 + Vector3.Clamp(vector3_3, Vector3.Zero, (Vector3) size) * vector3I1 + 1f / 1000f * vector3I1;
                        Vector3 vector3_6 = vector3I2 * vector3_4 + Vector3.Clamp(vector3_4, Vector3.Zero, (Vector3) size) * vector3I1 - 1f / 1000f * vector3I1;
                        vector3_1 = vector3_5 - (Vector3) center;
                        Vector3 vector3_7 = (Vector3) center;
                        vector3_2 = vector3_6 - vector3_7;
                    }
                    Vector3I vector3I3 = Vector3I.Floor(vector3_1);
                    Vector3I vector3I4 = Vector3I.Floor(vector3_2);
                    Vector3 result1;
                    Vector3.Transform(ref vector3_1, ref rotation, out result1);
                    Vector3 result2;
                    Vector3.Transform(ref vector3_2, ref rotation, out result2);
                    Vector3I result3;
                    Vector3I.Transform(ref vector3I3, ref rotation, out result3);
                    Vector3I result4;
                    Vector3I.Transform(ref vector3I4, ref rotation, out result4);
                    Vector3I vector3I5 = Vector3I.Floor(result1);
                    Vector3I vector3I6 = Vector3I.Floor(result2);
                    Vector3I vector3I7 = result3 - vector3I5;
                    Vector3I vector3I8 = result4 - vector3I6;
                    result1 += (Vector3) vector3I7;
                    result2 += (Vector3) vector3I8;
                    Vector3 vector3_8 = (Vector3) position + result1;
                    Vector3 vector3_9 = (Vector3) position + result2;
                    m_cacheNeighborBlocks.Clear();
                    Vector3 currentMin = Vector3.Min(vector3_8, vector3_9);
                    Vector3 currentMax = Vector3.Max(vector3_8, vector3_9);
                    Vector3I minI = Vector3I.Floor(currentMin);
                    Vector3I maxI = Vector3I.Floor(currentMax);

                    // grid.GetConnectedBlocks(minI, maxI, m_cacheNeighborBlocks);
                    GetConnectedBlocks(grid, minI, maxI, m_cacheNeighborBlocks);

                    if (m_cacheNeighborBlocks.Count != 0)
                    {
                        Vector3I result5;
                        Vector3I.Transform(ref mountPoint.Normal, ref rotation, out result5);
                        Vector3I otherBlockMinPos = minI - result5;
                        Vector3I otherBlockMaxPos = maxI - result5;
                        Vector3I faceNormal = -result5;
                        foreach (ConnectivityResult connectivityResult in m_cacheNeighborBlocks.Values)
                        {
                            if (connectivityResult.Position == position)
                            {
                                if (MyFakes.ENABLE_COMPOUND_BLOCKS && (connectivityResult.FatBlock == null || !connectivityResult.FatBlock.CheckConnectionAllowed || connectivityResult.FatBlock.ConnectionAllowed(ref otherBlockMinPos, ref otherBlockMaxPos, ref faceNormal, def)) && connectivityResult.FatBlock is MyCompoundCubeBlock)
                                {
                                    foreach (MySlimBlock block in (connectivityResult.FatBlock as MyCompoundCubeBlock).GetBlocks())
                                    {
                                        MyCubeBlockDefinition.MountPoint[] modelMountPoints = block.BlockDefinition.GetBuildProgressModelMountPoints(block.BuildLevelRatio);
                                        if (MyCubeGrid.CheckNeighborMountPointsForCompound(currentMin, currentMax, mountPoint, ref result5, def, connectivityResult.Position, block.BlockDefinition, modelMountPoints, block.Orientation, m_cacheMountPointsA))
                                        {
                                            __result = true;
                                            return false;
                                        }                                    }
                                }
                            }
                            else if (connectivityResult.FatBlock == null || !connectivityResult.FatBlock.CheckConnectionAllowed || connectivityResult.FatBlock.ConnectionAllowed(ref otherBlockMinPos, ref otherBlockMaxPos, ref faceNormal, def))
                            {
                                if (connectivityResult.FatBlock is MyCompoundCubeBlock)
                                {
                                    foreach (MySlimBlock block in (connectivityResult.FatBlock as MyCompoundCubeBlock).GetBlocks())
                                    {
                                        MyCubeBlockDefinition.MountPoint[] modelMountPoints = block.BlockDefinition.GetBuildProgressModelMountPoints(block.BuildLevelRatio);
                                        if (MyCubeGrid.CheckNeighborMountPoints(currentMin, currentMax, mountPoint, ref result5, def, connectivityResult.Position, block.BlockDefinition, modelMountPoints, block.Orientation, m_cacheMountPointsA))
                                        {
                                            __result = true;
                                            return false;
                                        }
                                    }
                                }
                                else
                                {
                                    float currentIntegrityRatio = 1f;
                                    if (connectivityResult.FatBlock != null && connectivityResult.FatBlock.SlimBlock != null)
                                        currentIntegrityRatio = connectivityResult.FatBlock.SlimBlock.BuildLevelRatio;
                                    MyCubeBlockDefinition.MountPoint[] modelMountPoints = connectivityResult.Definition.GetBuildProgressModelMountPoints(currentIntegrityRatio);
                                    if (MyCubeGrid.CheckNeighborMountPoints(currentMin, currentMax, mountPoint, ref result5, def, connectivityResult.Position, connectivityResult.Definition, modelMountPoints, connectivityResult.Orientation, m_cacheMountPointsA))
                                    {
                                        __result = true;
                                        return false;
                                    }
                                }
                            }
                        }
                    }
                }
                __result = false;
                return false;
            }
            finally
            {
                m_cacheNeighborBlocks.Clear();
            }
        }

        private static void GetConnectedBlocks(IMyGridConnectivityTest grid, Vector3I minI, Vector3I maxI, Dictionary<Vector3I, ConnectivityResult> outOverlappedCubeBlocks)
        {
            Vector3I pos = new Vector3I();
            for (pos.Z = minI.Z; pos.Z <= maxI.Z; ++pos.Z)
            {
                for (pos.Y = minI.Y; pos.Y <= maxI.Y; ++pos.Y)
                {
                    for (pos.X = minI.X; pos.X <= maxI.X; ++pos.X)
                    {
                        MySlimBlock cubeBlock = GetCubeBlock((MyCubeGrid)grid, pos);
                        if (cubeBlock != null)
                            outOverlappedCubeBlocks[cubeBlock.Position] = new ConnectivityResult()
                            {
                                Definition = cubeBlock.BlockDefinition,
                                FatBlock = cubeBlock.FatBlock,
                                Orientation = cubeBlock.Orientation,
                                Position = cubeBlock.Position
                            };
                    }
                }
            }
        }

        public static MySlimBlock GetCubeBlock(MyCubeGrid grid, Vector3I pos)
        {
            return grid.TryGetCube(pos, out var myCube) ? myCube.CubeBlock : null;
        }
    }
}