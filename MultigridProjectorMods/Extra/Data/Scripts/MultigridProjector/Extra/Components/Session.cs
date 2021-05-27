using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;

// ReSharper disable once CheckNamespace
namespace MultigridProjector.Extra
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class Session : MySessionComponentBase
    {
        public static bool IsServer { get; private set; }
        public static bool IsDedicated { get; private set; }
        public static bool IsMultiplayer { get; private set; }

        private static Session instance;

        private Aligner aligner;

        public override void UpdateBeforeSimulation()
        {
            if (instance == null)
                Initialize();
        }

        private void Initialize()
        {
            instance = this;

            IsServer = MyAPIGateway.Multiplayer.IsServer || MyAPIGateway.Session.OnlineMode == MyOnlineModeEnum.OFFLINE;
            IsDedicated = IsServer && MyAPIGateway.Utilities.IsDedicated;
            IsMultiplayer = MyAPIGateway.Multiplayer.MultiplayerActive;

            aligner = new Aligner();
        }


        public override void HandleInput()
        {
            instance?.aligner?.HandleInput();
        }
    }
}