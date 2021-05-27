using VRage.Game.Components;

// ReSharper disable once CheckNamespace
namespace MultigridProjector.Extra
{
    // ReSharper disable once UnusedType.Global
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class Session : MySessionComponentBase
    {
        private static Session instance;

        private Comms comms;
        private Aligner aligner;

        public override void UpdateBeforeSimulation()
        {
            if (instance == null)
                Initialize();
        }

        private void Initialize()
        {
            instance = this;
            comms = new Comms();
            aligner = new Aligner();
        }

        protected override void UnloadData()
        {
            aligner?.Dispose();
            aligner = null;

            comms?.Dispose();
            comms = null;

            instance = null;

            base.UnloadData();
        }

        public override void HandleInput()
        {
            instance?.aligner?.HandleInput();
        }
    }
}