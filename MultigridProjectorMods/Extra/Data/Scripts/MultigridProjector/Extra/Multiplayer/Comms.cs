using System;
using Sandbox.ModAPI;


// ReSharper disable once CheckNamespace
namespace MultigridProjector.Extra
{
    public class Comms : IDisposable
    {
        public static Role Role;
        public static bool HasLocalPlayer => Role != Role.DedicatedServer;
        public static bool IsServer => Role != Role.MultiplayerClient;
        public static ulong SteamId;

        public delegate void OnPacketReceived(ushort handlerId, Packet packet, ulong fromSteamId, bool fromServer);

        public static event OnPacketReceived PacketReceived;

        private const ushort Channel = 29719;

        public Comms()
        {
            Role = DetectRole();
            SteamId = MyAPIGateway.Multiplayer.MyId;
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(Channel, receive);
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
            MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(Channel, receive);
        }

        private static void receive(ushort handlerId, byte[] data, ulong fromSteamId, bool fromServer)
        {
            var packet = MyAPIGateway.Utilities.SerializeFromBinary<Packet>(data);
            PacketReceived?.Invoke(handlerId, packet, fromSteamId, fromServer);
        }

        private static void SendToServer(ushort handlerId, Packet packet, bool reliable = true)
        {
            switch (Role)
            {
                case Role.SinglePlayer:
                case Role.MultiplayerServer:
                case Role.DedicatedServer:
                    PacketReceived?.Invoke(handlerId, packet, SteamId, IsServer);
                    break;

                case Role.MultiplayerClient:
                    var data = MyAPIGateway.Utilities.SerializeToBinary(packet);
                    MyAPIGateway.Multiplayer.SendMessageToServer(Channel, data, reliable);
                    break;
            }
        }

        private static void SendToClient(ushort handlerId, Packet packet, ulong steamId, bool reliable = true)
        {
            if (Role == Role.SinglePlayer || Role != Role.DedicatedServer && steamId == SteamId)
            {
                PacketReceived?.Invoke(handlerId, packet, SteamId, IsServer);
                return;
            }

            var data = MyAPIGateway.Utilities.SerializeToBinary(packet);
            MyAPIGateway.Multiplayer.SendMessageTo(Channel, data, steamId, reliable);
        }
    }
}