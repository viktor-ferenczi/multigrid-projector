using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;

// ReSharper disable once CheckNamespace
namespace MultigridProjector.Extra
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class Session : MySessionComponentBase
    {
        public static bool IsServer;
        public static bool IsDedicated;

        public static Session Instance;
        private bool initialized;

        public AlignerClient AlignerClient { get; private set; }

        public override void UpdateBeforeSimulation()
        {
            if (!initialized)
                Initialize();
        }

        private void Initialize()
        {
            Instance = this;

            IsServer = MyAPIGateway.Multiplayer.IsServer || MyAPIGateway.Session.OnlineMode == MyOnlineModeEnum.OFFLINE;
            IsDedicated = IsServer && MyAPIGateway.Utilities.IsDedicated;

            // TODO: Consider client/server/dedicated
            AlignerClient = new AlignerClient();

            initialized = true;
        }

        public override void HandleInput()
        {
            if (!initialized)
                return;

            Instance?.AlignerClient?.HandleInput();
        }
    }
}