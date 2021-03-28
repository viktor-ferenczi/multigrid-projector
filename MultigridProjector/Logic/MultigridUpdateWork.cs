using System;
using System.Collections.Generic;
using System.Linq;
using MultigridProjector.Api;
using MultigridProjector.Extensions;
using MultigridProjector.Utilities;
using ParallelTasks;
using Sandbox.Game.Entities.Blocks;
using VRage.Profiler;

namespace MultigridProjector.Logic
{
    public class MultigridUpdateWork : IWork, IDisposable
    {
        public event Action OnUpdateWorkCompleted;

        // Access to the projection
        // Concurrency: The update work must write only into the dedicated members of subgrids
        private MultigridProjection _projection;
        private MyProjectorBase Projector => _projection.Projector;
        private List<Subgrid> Subgrids => _projection.Subgrids;

        // Task control
        private Task _task;
        private volatile bool _stop;
        private bool _allGridsProcessed;

        public bool IsComplete => _task.IsComplete;

        // ReSharper disable once PossiblyImpureMethodCallOnReadonlyVariable
        public WorkOptions Options => Parallel.DefaultOptions.WithDebugInfo(MyProfiler.TaskType.Block, "MultigridUpdateWork");

        public MultigridUpdateWork(MultigridProjection projection)
        {
            _projection = projection;
        }

        public void Dispose()
        {
            _stop = true;
            if (!_task.IsComplete)
            {
                _task.Wait(true);
            }

            _projection = null;
        }

        public void Start()
        {
            if (!IsComplete) return;

            _allGridsProcessed = false;
            _task = Parallel.Start(this, OnComplete);
        }

        public void DoWork(WorkData workData = null)
        {
            if (_stop || Projector.Closed) return;

            try
            {
                UpdateBlockStatesAndCollectStatistics();
                FindBuiltMechanicalConnections();
            }
            catch (Exception e)
            {
                PluginLog.Error(e, "Update work failed");
                return;
            }

            _allGridsProcessed = true;
        }

        public void UpdateBlockStatesAndCollectStatistics(WorkData workData = null)
        {
            foreach (var subgrid in Subgrids)
            {
                if(_stop) break;

                if(!subgrid.UpdateRequested) continue;
                subgrid.UpdateRequested = false;

                var previewGrid = subgrid.PreviewGrid;

                var stats = subgrid.Stats;
                stats.Clear();
                stats.TotalBlocks += previewGrid.CubeBlocks.Count;

                var blockStates = subgrid.BlockStates;

                // Optimization: Shortcut the case when there are no blocks yet
                if (!subgrid.HasBuilt)
                {
                    foreach (var previewBlock in previewGrid.CubeBlocks)
                    {
                        blockStates[previewBlock.Position] = BlockState.NotBuildable;
                        stats.RegisterRemainingBlock(previewBlock);
                    }

                    continue;
                }

                foreach (var previewBlock in previewGrid.CubeBlocks)
                {
                    if(subgrid.TryGetBuiltBlockByPreview(previewBlock, out var builtSlimBlock))
                    {
                        // Already built
                        blockStates[previewBlock.Position] = builtSlimBlock.Integrity >= previewBlock.Integrity ? BlockState.FullyBuilt : BlockState.BeingBuilt;  
                        continue;
                    }

                    // This block hasn't been built yet
                    stats.RegisterRemainingBlock(previewBlock);

                    if (builtSlimBlock != null)
                    {
                        // A different block was built there
                        blockStates[previewBlock.Position] = BlockState.Mismatch;
                        continue;
                    }

                    if (Projector.CanBuild(previewBlock))
                    {
                        // Block is buildable
                        blockStates[previewBlock.Position] = BlockState.Buildable;
                        stats.BuildableBlocks++;
                        continue;
                    }

                    blockStates[previewBlock.Position] = BlockState.NotBuildable;
                }
            }
        }

        private void FindBuiltMechanicalConnections()
        {
            foreach (var subgrid in Subgrids)
            {
                if(_stop) break;

                var blockStates = subgrid.BlockStates;

                foreach (var (position, baseConnection) in subgrid.BaseConnections)
                {
                    switch (blockStates[position])
                    {
                        case BlockState.BeingBuilt:
                        case BlockState.FullyBuilt:
                            if (baseConnection.Found != null) break;
                            if (subgrid.TryGetBuiltBlockByPreview(baseConnection.Preview.SlimBlock, out var builtBlock))
                                baseConnection.Found = (MyMechanicalConnectionBlockBase) builtBlock.FatBlock;
                            else
                                baseConnection.Found = null;
                            break;
                        default:
                            baseConnection.Found = null;
                            break;
                    }
                }

                foreach (var (position, topConnection) in subgrid.TopConnections)
                {
                    switch (blockStates[position])
                    {
                        case BlockState.BeingBuilt:
                        case BlockState.FullyBuilt:
                            if (topConnection.Found != null) break;
                            if (subgrid.TryGetBuiltBlockByPreview(topConnection.Preview.SlimBlock, out var builtBlock))
                                topConnection.Found = (MyAttachableTopBlockBase)builtBlock.FatBlock;
                            break;
                        default:
                            topConnection.Found = null;
                            break;
                    }
                }
            }
        }

        private void OnComplete()
        {
            if (_allGridsProcessed)
                OnUpdateWorkCompleted?.Invoke();
        }
    }
}