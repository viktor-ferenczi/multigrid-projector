using System;
using Sandbox.ModAPI;


// ReSharper disable once CheckNamespace
namespace MultigridProjector.Extra
{
    public class Comms : IDisposable
    {
        public static Role Role;
        private const ushort Channel = 0x351a;

        public delegate void OnPacketReceived(ushort handlerId, byte[] data, ulong fromSteamId, bool fromServer);

        public static event OnPacketReceived PacketReceived;

        public Comms()
        {
            Role = DetectRole();

            if (Role != Role.SinglePlayer)
                MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(Channel, packetReceived);
        }

        public void Dispose()
        {
            if (Role != Role.SinglePlayer)
                MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(Channel, packetReceived);
        }

        private static Role DetectRole()
        {
            if (MyAPIGateway.Utilities.IsDedicated)
                return Role.DedicatedServer;

            if (MyAPIGateway.Multiplayer.MultiplayerActive)
                return MyAPIGateway.Multiplayer.IsServer ? Role.MultiplayerServer : Role.MultiplayerClient;

            return Role.SinglePlayer;
        }

        private static void packetReceived(ushort handlerId, byte[] data, ulong fromSteamId, bool fromServer)
        {
            PacketReceived?.Invoke(handlerId, data, fromSteamId, fromServer);
        }

        public static void SendToServer<T>(ushort handlerId, T packet, bool reliable = true)
        {
            var data = MyAPIGateway.Utilities.SerializeToBinary(packet);
            MyAPIGateway.Multiplayer.SendMessageToServer(Channel, data, reliable);
        }

        public static void SendToClient<T>(ushort handlerId, T packet, ulong steamId, bool reliable = true)
        {
            var data = MyAPIGateway.Utilities.SerializeToBinary(packet);
            MyAPIGateway.Multiplayer.SendMessageTo(Channel, data, steamId, reliable);
        }
    }
}