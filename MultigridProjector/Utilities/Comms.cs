using Sandbox.Game;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using System;
using System.Linq;

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
        public static bool ServerHasPlugin;
        private const ushort HandlerId = 0x7b94;
        private static readonly byte[] Signature = { 2, 3, 5, 7, 11, 13, 17, 19 };

        public delegate void OnPacketReceived(byte[] data, ulong fromSteamId, bool fromServer);
        public static event OnPacketReceived PacketReceived;

        public Comms()
        {
            Role = DetectRole();

            // If we're in single player we already know we have MGP
            if (Role == Role.SinglePlayer)
            {
                ServerHasPlugin = true;
                return;
            }

            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(HandlerId, DoPacketReceived);

            // Wait until the message handler is registered
            Events.InvokeOnGameThread(() =>
            {
                // If connecting to a server listen if it has MGP
                if (Role == Role.MultiplayerClient)
                {
                    ServerHasPlugin = false;

                    PacketReceived += OnServerPluginMessage;
                }

                // If hosting a game let players know we have MGP
                else if (Role == Role.MultiplayerServer || Role == Role.DedicatedServer)
                {
                    ServerHasPlugin = true;

                    // This will not fire as we connect to our own game due to the delay with InvokeOnGameThread
                    MyVisualScriptLogicProvider.PlayerConnected += SendServerHasPlugin;
                }
            });
        }

        private static void SendServerHasPlugin(long playerId)
        {
            ulong steamId = MySession.Static.Players.TryGetSteamId(playerId);
            SendToClient(HandlerId, Signature, steamId);
            PluginLog.Debug($"Sent Message to {steamId} (check if this is garage collected)");
        }

        private static void OnServerPluginMessage(byte[] data, ulong fromSteamId, bool fromServer)
        {
            if (!fromServer || data.SequenceEqual(Signature))
                return;

            ServerHasPlugin = true;
            PacketReceived -= OnServerPluginMessage;

            PluginLog.Debug("Server plugin is present");
        }

        public void Dispose()
        {
            if (Role == Role.SinglePlayer)
                return;

            MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(HandlerId, DoPacketReceived);

            if (Role == Role.MultiplayerServer)
                MyVisualScriptLogicProvider.PlayerConnected -= SendServerHasPlugin;

            if (Role == Role.MultiplayerClient)
                PacketReceived -= OnServerPluginMessage;
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
            PacketReceived?.Invoke(data, fromSteamId, fromServer);
        }

        public static void SendToServer<T>(ushort handlerId, T packet, bool reliable = true)
        {
            byte[] data = MyAPIGateway.Utilities.SerializeToBinary(packet);
            MyAPIGateway.Multiplayer.SendMessageToServer(HandlerId, data, reliable);
        }

        public static void SendToClient<T>(ushort handlerId, T packet, ulong steamId, bool reliable = true)
        {
            byte[] data = MyAPIGateway.Utilities.SerializeToBinary(packet);
            MyAPIGateway.Multiplayer.SendMessageTo(HandlerId, data, steamId, reliable);
        }
    }
}