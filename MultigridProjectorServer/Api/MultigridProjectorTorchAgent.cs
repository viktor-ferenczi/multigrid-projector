using System;
using System.Collections.Generic;
using System.Reflection;
using Torch.API.Managers;
using Torch.API.Plugins;
using Torch.API.Session;
using Torch.Managers;
using VRage.Game;
using VRage.Game.ModAPI;
using VRageMath;

namespace MultigridProjector.Api
{
    public class MultigridProjectorTorchAgent : IMultigridProjectorApi
    {
        public static readonly Guid PluginId = new Guid("d9359ba0-9a69-41c3-971d-eb5170adb97e");
        public static readonly string CompatibleMajorVersion = "0.";
        public readonly ITorchPlugin Plugin;
        public readonly object Api;

        public bool Available => Plugin != null;
        public string Version { get; }

        private readonly MethodInfo _miGetSubgridCount;
        private readonly MethodInfo _miGetOriginalGridBuilders;
        private readonly MethodInfo _miGetPreviewGrid;
        private readonly MethodInfo _miGetBuiltGrid;
        private readonly MethodInfo _miGetBlockState;
        private readonly MethodInfo _miGetBlockStates;
        private readonly MethodInfo _miGetBaseConnections;
        private readonly MethodInfo _miGetTopConnections;

        public MultigridProjectorTorchAgent(ITorchSession torchSession)
        {
            var pluginManager = torchSession.Managers.GetManager<PluginManager>();
            if (!pluginManager.Plugins.TryGetValue(PluginId, out var plugin))
                return;

            Api = plugin.GetType().GetProperty("Api")?.GetValue(plugin);
            if (Api == null)
                return;

            var apiType = Api.GetType();
            Version = (string) apiType.GetProperty("Version")?.GetValue(Api);
            if (Version == null || !Version.StartsWith(CompatibleMajorVersion))
                return;

            _miGetSubgridCount = apiType.GetMethod("GetSubgridCount", BindingFlags.Instance | BindingFlags.Public);
            _miGetOriginalGridBuilders = apiType.GetMethod("GetOriginalGridBuilders", BindingFlags.Instance | BindingFlags.Public);
            _miGetPreviewGrid = apiType.GetMethod("GetPreviewGrid", BindingFlags.Instance | BindingFlags.Public);
            _miGetBuiltGrid = apiType.GetMethod("GetBuiltGrid", BindingFlags.Instance | BindingFlags.Public);
            _miGetBlockState = apiType.GetMethod("GetBlockState", BindingFlags.Instance | BindingFlags.Public);
            _miGetBlockStates = apiType.GetMethod("GetBlockStates", BindingFlags.Instance | BindingFlags.Public);
            _miGetBaseConnections = apiType.GetMethod("GetBaseConnections", BindingFlags.Instance | BindingFlags.Public);
            _miGetTopConnections = apiType.GetMethod("GetTopConnections", BindingFlags.Instance | BindingFlags.Public);

            Plugin = plugin;
        }

        public int GetSubgridCount(long projectorId)
        {
            return (int) (_miGetSubgridCount?.Invoke(Api, new object[] {projectorId}) ?? 0);
        }

        public List<MyObjectBuilder_CubeGrid> GetOriginalGridBuilders(long projectorId)
        {
            return (List<MyObjectBuilder_CubeGrid>) _miGetOriginalGridBuilders?.Invoke(Api, new object[] {projectorId});
        }

        public IMyCubeGrid GetPreviewGrid(long projectorId, int subgridIndex)
        {
            return (IMyCubeGrid) _miGetPreviewGrid?.Invoke(Api, new object[] {projectorId, subgridIndex});
        }

        public IMyCubeGrid GetBuiltGrid(long projectorId, int subgridIndex)
        {
            return (IMyCubeGrid) _miGetBuiltGrid?.Invoke(Api, new object[] {projectorId, subgridIndex});
        }

        public BlockState GetBlockState(long projectorId, int subgridIndex, Vector3I position)
        {
            return (BlockState) (_miGetBlockState?.Invoke(Api, new object[] {projectorId, subgridIndex, position}) ?? BlockState.Unknown);
        }

        public bool GetBlockStates(Dictionary<Vector3I, BlockState> blockStates, long projectorId, int subgridIndex, BoundingBoxI box, int mask)
        {
            return (bool) (_miGetBlockStates?.Invoke(Api, new object[] {blockStates, projectorId, subgridIndex, box, mask}) ?? false);
        }

        public Dictionary<Vector3I, BlockLocation> GetBaseConnections(long projectorId, int subgridIndex)
        {
            return (Dictionary<Vector3I, BlockLocation>) _miGetBaseConnections?.Invoke(Api, new object[] {projectorId, subgridIndex});
        }

        public Dictionary<Vector3I, BlockLocation> GetTopConnections(long projectorId, int subgridIndex)
        {
            return (Dictionary<Vector3I, BlockLocation>) _miGetTopConnections?.Invoke(Api, new object[] {projectorId, subgridIndex});
        }
    }
}