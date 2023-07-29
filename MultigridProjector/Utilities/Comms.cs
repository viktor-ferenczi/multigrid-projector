using Sandbox.Game;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using System;

// ReSharper disable once CheckNamespace
namespace MultigridProjector.Utilities
{
    public enum Role
    {
        SinglePlayer,
        MultiplayerServer,
        MultiplayerClient,
        DedicatedServer,
    }

    public class Comms : IDisposable
    {
        public static Role Role;
        public static bool ServerPlugin;
        private const ushort Channel = 0x7b94;

        public delegate void OnPacketReceived(ushort handlerId, byte[] data, ulong fromSteamId, bool fromServer);
        public static event OnPacketReceived PacketReceived;

        public Comms()
        {
            Role = DetectRole();

            // If we're in singleplayer we already know we have MGP
            if (Role == Role.SinglePlayer)
            {
                ServerPlugin = true;
                return;
            }

            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(Channel, DoPacketReceived);

            // Wait until the message handler is registered
            Events.InvokeOnGameThread(() =>
            {
                // If connnecting to a server listen if it has MGP
                if (Role == Role.MultiplayerClient)
                {
                    ServerPlugin = false;

                    PacketReceived += ListenServerHasPlugin;
                }

                // If hosting a game let players know we have MGP
                else if (Role == Role.MultiplayerServer || Role == Role.DedicatedServer)
                {
                    ServerPlugin = true;

                    // This will not fire as we connect to our own game due to the delay with InvokeOnGameThread
                    MyVisualScriptLogicProvider.PlayerConnected += SendServerHasPlugin;
                }
            });
        }

        private static void SendServerHasPlugin(long playerId)
        {
            ulong steamId = MySession.Static.Players.TryGetSteamId(playerId);
            SendToClient(0x01, true, steamId);
            PluginLog.Debug($"Sent Message to {steamId} (check if this is garage collected)");
        }

        private static void ListenServerHasPlugin(ushort handlerId, byte[] data, ulong fromSteamId, bool fromServer)
        {
            if (!fromServer || handlerId != 0x01)
                return;

            ServerPlugin = true;
            PacketReceived -= ListenServerHasPlugin;

            PluginLog.Debug("Server has MGP");
        }

        public void Dispose()
        {
            if (Role == Role.SinglePlayer)
                return;

            MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(Channel, DoPacketReceived);

            if (Role == Role.MultiplayerServer)
                MyVisualScriptLogicProvider.PlayerConnected -= SendServerHasPlugin;

            if (Role == Role.MultiplayerClient)
                PacketReceived -= ListenServerHasPlugin;
        }

        private static Role DetectRole()
        {
            if (MyAPIGateway.Utilities.IsDedicated)
                return Role.DedicatedServer;
            
            if (MyAPIGateway.Multiplayer.MultiplayerActive)
                return MyAPIGateway.Multiplayer.IsServer ? Role.MultiplayerServer : Role.MultiplayerClient;

            return Role.SinglePlayer;
        }

        private static void DoPacketReceived(ushort handlerId, byte[] data, ulong fromSteamId, bool fromServer)
        {
            PacketReceived?.Invoke(handlerId, data, fromSteamId, fromServer);
        }

        public static void SendToServer<T>(ushort handlerId, T packet, bool reliable = true)
        {
            byte[] data = MyAPIGateway.Utilities.SerializeToBinary(packet);
            MyAPIGateway.Multiplayer.SendMessageToServer(Channel, data, reliable);
        }

        public static void SendToClient<T>(ushort handlerId, T packet, ulong steamId, bool reliable = true)
        {
            byte[] data = MyAPIGateway.Utilities.SerializeToBinary(packet);
            MyAPIGateway.Multiplayer.SendMessageTo(Channel, data, steamId, reliable);
        }
    }
}