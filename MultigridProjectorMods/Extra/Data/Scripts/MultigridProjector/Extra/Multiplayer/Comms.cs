using System;
using Sandbox.ModAPI;


// ReSharper disable once CheckNamespace
namespace MultigridProjector.Extra
{
    public class Comms : IDisposable
    {
        public static Role Role;
        public static bool HasLocalPlayer => Role != Role.DedicatedServer;

        public static event Action<Packet> PacketReceived;

        private const ushort Channel = 29719;

        public Comms()
        {
            Role = DetectRole();

            MyAPIGateway.Multiplayer.RegisterMessageHandler(Channel, receive);
        }

        private static Role DetectRole()
        {
            if (MyAPIGateway.Utilities.IsDedicated)
                return Role.DedicatedServer;

            if (MyAPIGateway.Multiplayer.MultiplayerActive)
                return MyAPIGateway.Multiplayer.IsServer ? Role.MultiplayerServer : Role.MultiplayerClient;

            return Role.SinglePlayer;
        }

        public void Dispose()
        {
            MyAPIGateway.Multiplayer.UnregisterMessageHandler(Channel, receive);
        }

        private static void receive(byte[] data)
        {
            var packet = MyAPIGateway.Utilities.SerializeFromBinary<Packet>(data);
            PacketReceived?.Invoke(packet);
        }

        private static void SendToServer(Packet packet, bool reliable=true)
        {
            switch (Role)
            {
                case Role.SinglePlayer:
                case Role.MultiplayerServer:
                case Role.DedicatedServer:
                    PacketReceived?.Invoke(packet);
                    break;

                case Role.MultiplayerClient:
                    var data = MyAPIGateway.Utilities.SerializeToBinary(packet);
                    MyAPIGateway.Multiplayer.SendMessageToServer(Channel, data, reliable);
                    break;
            }
        }

        private static void SendToClient(Packet packet, ulong steamId, bool reliable = true)
        {
            if (Role == Role.SinglePlayer || Role != Role.DedicatedServer && steamId == MyAPIGateway.Multiplayer.MyId)
            {
                PacketReceived?.Invoke(packet);
                return;
            }

            var data = MyAPIGateway.Utilities.SerializeToBinary(packet);
            MyAPIGateway.Multiplayer.SendMessageTo(Channel, data, steamId, reliable);
        }
    }
}