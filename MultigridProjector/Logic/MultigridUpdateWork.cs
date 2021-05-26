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
        private MultigridProjection projection;
        private MyProjectorBase Projector => projection.Projector;
        private IEnumerable<Subgrid> SupportedSubgrids => projection.SupportedSubgrids;

        // Task control
        private Task task;
        private volatile bool stop;
        private bool allGridsProcessed;
        private bool ShouldStop => stop || !projection.Initialized || Projector.Closed;

        public bool IsComplete => task.IsComplete;

        // Subgrid scan statistics for performance logging only (no functionality affected)
        public int SubgridsScanned;
        public int BlocksScanned;

        // ReSharper disable once PossiblyImpureMethodCallOnReadonlyVariable
        public WorkOptions Options => Parallel.DefaultOptions.WithDebugInfo(MyProfiler.TaskType.Block, "MultigridUpdateWork");

        public MultigridUpdateWork(MultigridProjection projection)
        {
            this.projection = projection;
        }

        public void Dispose()
        {
            Cancel();

            if (!task.IsComplete)
            {
                task.Wait(true);
            }

            projection = null;
        }

        public void Start()
        {
            if (!IsComplete)
                return;

            stop = false;
            allGridsProcessed = false;
            task = Parallel.Start(this, OnComplete);
        }

        private void Cancel()
        {
            if (IsComplete)
                return;

            stop = true;
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

            allGridsProcessed = !ShouldStop;
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
            if (!allGridsProcessed)
                return;

            OnUpdateWorkCompleted?.Invoke();
        }
    }
}