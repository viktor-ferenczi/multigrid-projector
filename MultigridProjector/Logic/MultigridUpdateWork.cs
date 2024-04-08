using System;
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

        // Background worker task
        private Task task;

        // Set to true to explicitly signal the background worker to stop
        private volatile bool stop;

        // Set to true when the background worker successfully finishes grid scanning
        private volatile bool gridScanSucceeded;

        // True value causes the running background worker to stop prematurely,
        // this should be triggered if the result of the grid scanning is not needed anymore
        private bool ShouldStop => stop || !projection.Initialized || Projector.Closed;

        // Explicitly set to true only while the worker is running
        private volatile bool isRunning;

        // True value indicates that the background worker has finished executing (regardless of success/failure)
        public bool IsComplete => !isRunning && task.IsComplete;

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
            stop = true;

            if (isRunning)
                task.Wait(true);

            projection = null;
        }

        public void Start()
        {
            if (!IsComplete)
                return;

            stop = false;
            gridScanSucceeded = false;
            task = Parallel.Start(this, OnComplete);
        }

        public void DoWork(WorkData workData = null)
        {
            if (projection == null)
                return;
            
            isRunning = true;
            try
            {
                var supportedSubgrids = projection.GetSupportedSubgrids();
                UpdateBlockStatesAndCollectStatistics(supportedSubgrids, projection.CheckHavokIntersections);
                FindBuiltMechanicalConnections(supportedSubgrids);
                gridScanSucceeded = !ShouldStop;
            }
            catch (Exception e)
            {
                PluginLog.Error(e, "Update work failed");
            }
            finally
            {
                isRunning = false;
            }
        }

        private void UpdateBlockStatesAndCollectStatistics(Subgrid[] supportedSubgrids, bool checkHavokIntersections)
        {
            SubgridsScanned = 0;
            BlocksScanned = 0;

            foreach (var subgrid in supportedSubgrids)
            {
                if (ShouldStop) break;

                var blockCount = subgrid.UpdateBlockStatesBackgroundWork(Projector, checkHavokIntersections);

                BlocksScanned += blockCount;
                if (blockCount > 0)
                    SubgridsScanned++;
            }
        }

        private void FindBuiltMechanicalConnections(Subgrid[] supportedSubgrids)
        {
            foreach (var subgrid in supportedSubgrids)
            {
                if (ShouldStop) break;
                subgrid.FindBuiltBaseConnectionsBackgroundWork();

                if (ShouldStop) break;
                subgrid.FindBuiltTopConnectionsBackgroundWork();
            }
        }

        private void OnComplete()
        {
            if (gridScanSucceeded)
                OnUpdateWorkCompleted?.Invoke();
        }
    }
}