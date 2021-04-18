using System;
using System.Collections.Generic;
using System.Threading;
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
        private IEnumerable<Subgrid> SupportedSubgrids => _projection.SupportedSubgrids;

        // Task control
        private Task _task;
        private volatile bool _stop;
        private bool _allGridsProcessed;
        private bool ShouldStop => _stop || !_projection.Initialized || Projector.Closed;

        public bool IsComplete => _task.IsComplete;

        // Subgrid scan statistics for performance logging only (no functionality affected)
        public int SubgridsScanned;
        public int BlocksScanned;

        // ReSharper disable once PossiblyImpureMethodCallOnReadonlyVariable
        public WorkOptions Options => Parallel.DefaultOptions.WithDebugInfo(MyProfiler.TaskType.Block, "MultigridUpdateWork");

        public MultigridUpdateWork(MultigridProjection projection)
        {
            _projection = projection;
        }

        public void Dispose()
        {
            Cancel();

            if (!_task.IsComplete)
            {
                _task.Wait(true);
            }

            _projection = null;
        }

        public void Start()
        {
            if (!IsComplete)
                return;

            _stop = false;
            _allGridsProcessed = false;
            _task = Parallel.Start(this, OnComplete);
        }

        private void Cancel()
        {
            if (IsComplete)
                return;

            _stop = true;
        }

        public void DoWork(WorkData workData = null)
        {
            if (ShouldStop) return;

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

            _allGridsProcessed = !ShouldStop;
        }

        private void UpdateBlockStatesAndCollectStatistics(WorkData workData = null)
        {
            SubgridsScanned = 0;
            BlocksScanned = 0;

            foreach (var subgrid in SupportedSubgrids)
            {
                if (ShouldStop) break;

                var blockCount = subgrid.UpdateBlockStatesBackgroundWork(Projector);

                BlocksScanned += blockCount;
                if(blockCount > 0)
                    SubgridsScanned++;
            }
        }

        private void FindBuiltMechanicalConnections()
        {
            foreach (var subgrid in SupportedSubgrids)
            {
                if (ShouldStop) break;
                subgrid.FindBuiltBaseConnectionsBackgroundWork();

                if (ShouldStop) break;
                subgrid.FindBuiltTopConnectionsBackgroundWork();
            }
        }

        private void OnComplete()
        {
            if (!_allGridsProcessed)
                return;

            OnUpdateWorkCompleted?.Invoke();
        }
    }
}