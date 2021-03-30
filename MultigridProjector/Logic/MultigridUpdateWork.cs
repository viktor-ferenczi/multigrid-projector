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

        private void UpdateBlockStatesAndCollectStatistics(WorkData workData = null)
        {
            foreach (var subgrid in Subgrids)
            {
                if(_stop) break;
                subgrid.UpdateBlockStatesBackgroundWork(Projector);
            }
        }

        private void FindBuiltMechanicalConnections()
        {
            foreach (var subgrid in Subgrids)
            {
                if(_stop) break;
                subgrid.FindBuiltBaseConnectionsBackgroundWork();
                subgrid.FindBuiltTopConnectionsBackgroundWork();
            }
        }

        private void OnComplete()
        {
            if (_allGridsProcessed)
                OnUpdateWorkCompleted?.Invoke();
        }
    }
}