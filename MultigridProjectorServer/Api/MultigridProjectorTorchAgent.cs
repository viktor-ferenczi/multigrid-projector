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

// ReSharper disable once CheckNamespace
namespace MultigridProjector.Api
{
    // ReSharper disable once UnusedType.Global
    public class MultigridProjectorTorchAgent : IMultigridProjectorApi
    {
        public static readonly Guid PluginId = new Guid("d9359ba0-9a69-41c3-971d-eb5170adb97e");
        public static readonly string CompatibleMajorVersion = "0.";
        public readonly ITorchPlugin Plugin;
        public readonly object Api;

        // ReSharper disable once UnusedMember.Global
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
        private readonly MethodInfo _miGetScanNumber;
        private readonly MethodInfo _miGetYaml;
        private readonly MethodInfo _miGetStateHash;
        private readonly MethodInfo _miIsSubgridComplete;
        private readonly MethodInfo _miGetStats;
        private readonly MethodInfo _miGetSubgridStats;
        private readonly MethodInfo _miEnablePreview;
        private readonly MethodInfo _miEnableSubgridPreview;
        private readonly MethodInfo _miEnableBlockPreview;
        private readonly MethodInfo _miIsPreviewEnabled;
        private readonly MethodInfo _miEnableWelding;
        private readonly MethodInfo _miEnableSubgridWelding;
        private readonly MethodInfo _miEnableBlockWelding;
        private readonly MethodInfo _miIsWeldingEnabled;

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

            _miGetSubgridCount = apiType.GetMethod(nameof(GetSubgridCount), BindingFlags.Instance | BindingFlags.Public);
            _miGetOriginalGridBuilders = apiType.GetMethod(nameof(GetOriginalGridBuilders), BindingFlags.Instance | BindingFlags.Public);
            _miGetPreviewGrid = apiType.GetMethod(nameof(GetPreviewGrid), BindingFlags.Instance | BindingFlags.Public);
            _miGetBuiltGrid = apiType.GetMethod(nameof(GetBuiltGrid), BindingFlags.Instance | BindingFlags.Public);
            _miGetBlockState = apiType.GetMethod(nameof(GetBlockState), BindingFlags.Instance | BindingFlags.Public);
            _miGetBlockStates = apiType.GetMethod(nameof(GetBlockStates), BindingFlags.Instance | BindingFlags.Public);
            _miGetBaseConnections = apiType.GetMethod(nameof(GetBaseConnections), BindingFlags.Instance | BindingFlags.Public);
            _miGetTopConnections = apiType.GetMethod(nameof(GetTopConnections), BindingFlags.Instance | BindingFlags.Public);
            _miGetScanNumber = apiType.GetMethod(nameof(GetScanNumber), BindingFlags.Instance | BindingFlags.Public);
            _miGetYaml = apiType.GetMethod(nameof(GetYaml), BindingFlags.Instance | BindingFlags.Public);
            _miGetStateHash = apiType.GetMethod(nameof(GetStateHash), BindingFlags.Instance | BindingFlags.Public);
            _miIsSubgridComplete = apiType.GetMethod(nameof(IsSubgridComplete), BindingFlags.Instance | BindingFlags.Public);
            _miGetStats = apiType.GetMethod(nameof(GetStats), BindingFlags.Instance | BindingFlags.Public);
            _miGetSubgridStats = apiType.GetMethod(nameof(GetSubgridStats), BindingFlags.Instance | BindingFlags.Public);
            _miEnablePreview = apiType.GetMethod(nameof(EnablePreview), BindingFlags.Instance | BindingFlags.Public);
            _miEnableSubgridPreview = apiType.GetMethod(nameof(EnableSubgridPreview), BindingFlags.Instance | BindingFlags.Public);
            _miEnableBlockPreview = apiType.GetMethod(nameof(EnableBlockPreview), BindingFlags.Instance | BindingFlags.Public);
            _miIsPreviewEnabled = apiType.GetMethod(nameof(IsPreviewEnabled), BindingFlags.Instance | BindingFlags.Public);
            _miEnableWelding = apiType.GetMethod(nameof(EnableWelding), BindingFlags.Instance | BindingFlags.Public);
            _miEnableSubgridWelding = apiType.GetMethod(nameof(EnableSubgridWelding), BindingFlags.Instance | BindingFlags.Public);
            _miEnableBlockWelding = apiType.GetMethod(nameof(EnableBlockWelding), BindingFlags.Instance | BindingFlags.Public);
            _miIsWeldingEnabled = apiType.GetMethod(nameof(IsWeldingEnabled), BindingFlags.Instance | BindingFlags.Public);

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

        public long GetScanNumber(long projectorId)
        {
            return (long) (_miGetScanNumber?.Invoke(Api, new object[] {projectorId}) ?? 0);
        }

        public string GetYaml(long projectorId)
        {
            return (string) (_miGetYaml?.Invoke(Api, new object[] {projectorId}) ?? "");
        }

        public ulong GetStateHash(long projectorId, int subgridIndex)
        {
            return (ulong) (_miGetStateHash?.Invoke(Api, new object[] {projectorId, subgridIndex}) ?? 0);
        }

        public bool IsSubgridComplete(long projectorId, int subgridIndex)
        {
            return (bool) (_miIsSubgridComplete?.Invoke(Api, new object[] {projectorId, subgridIndex}) ?? false);
        }

        public void GetStats(long projectorId, ProjectionStats stats)
        {
            _miGetStats?.Invoke(Api, new object[] {projectorId, stats});
        }

        public void GetSubgridStats(long projectorId, int subgridIndex, ProjectionStats stats)
        {
            _miGetSubgridStats?.Invoke(Api, new object[] {projectorId, subgridIndex, stats});
        }

        public void EnablePreview(long projectorId, bool enable)
        {
            _miEnablePreview?.Invoke(Api, new object[] {projectorId, enable});
        }

        public void EnableSubgridPreview(long projectorId, int subgridIndex, bool enable)
        {
            _miEnableSubgridPreview?.Invoke(Api, new object[] {projectorId, subgridIndex, enable});
        }

        public void EnableBlockPreview(long projectorId, int subgridIndex, Vector3I position, bool enable)
        {
            _miEnableBlockPreview?.Invoke(Api, new object[] {projectorId, subgridIndex, position, enable});
        }

        public bool IsPreviewEnabled(long projectorId, int subgridIndex, Vector3I position)
        {
            return (bool) (_miIsPreviewEnabled?.Invoke(Api, new object[] {projectorId, subgridIndex, position}) ?? false);
        }

        public void EnableWelding(long projectorId, bool enable)
        {
            _miEnableWelding?.Invoke(Api, new object[] {projectorId, enable});
        }

        public void EnableSubgridWelding(long projectorId, int subgridIndex, bool enable)
        {
            _miEnableSubgridWelding?.Invoke(Api, new object[] {projectorId, subgridIndex, enable});
        }

        public void EnableBlockWelding(long projectorId, int subgridIndex, Vector3I position, bool enable)
        {
            _miEnableBlockWelding?.Invoke(Api, new object[] {projectorId, subgridIndex, position, enable});
        }

        public bool IsWeldingEnabled(long projectorId, int subgridIndex, Vector3I position)
        {
            return (bool) (_miIsWeldingEnabled?.Invoke(Api, new object[] {projectorId, subgridIndex, position}) ?? false);
        }
    }
}