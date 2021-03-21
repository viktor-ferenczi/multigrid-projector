using VRage.Game.Components;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRage.ObjectBuilders;
using VRage.ModAPI;
using MultigridProjector.Api;
using VRage.Utils;

// ReSharper disable once CheckNamespace
namespace MultigridProjectorMod
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Projector), true)]
    public class MultigridProjectorMod : MyGameLogicComponent
    {
        IMyProjector Projector;
        private MultigridProjectorModAgent _mgp;
        private bool _noted;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            if (Projector.Closed || !Projector.InScene || Projector.OwnerId == 0)
                return;

            Projector = Entity as IMyProjector;

            Entity.NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;

            MyLog.Default.WriteLine($"MultigridProjectorMod: Projector {Projector.DisplayName} [{Projector.EntityId}] registered");

            _mgp = new MultigridProjectorModAgent();
        }

        public override void UpdateBeforeSimulation100()
        {
            if (_mgp.Available && !_noted)
            {
                MyLog.Default.WriteLine($"MultigridProjectorMod: Projector {Projector.DisplayName} [{Projector.EntityId}] MGP API connection is available");
                MyAPIGateway.Utilities.ShowMessage("MGP", $"Client plugin version {_mgp.Version}");
                MyAPIGateway.Utilities.ShowMessage("MGP", $"Projector {Projector.DisplayName} [{Projector.EntityId}] is usable with multigrid blueprints!");
                _noted = true;
            }
        }

        public override void Close()
        {
            if(Projector == null)
                return;

            MyLog.Default.WriteLine($"MultigridProjectorMod: Projector {Projector.DisplayName} [{Projector.EntityId}] closed");

            _mgp = null;

            Projector = null;

            base.Close();
        }
    }
}