﻿using System;
using System.Collections.Generic;
using R2API.Networking.Interfaces;
using R2API.Networking.Messages;
using R2API.Utils;
using RoR2.Networking;
using UnityEngine.Networking;

namespace R2API.Networking {
    // ReSharper disable once InconsistentNaming
    [R2APISubmodule]
    public static class NetworkingAPI {
        /// <summary>
        /// Return true if the submodule is loaded.
        /// </summary>
        public static bool Loaded {
            get => _loaded;
            set => _loaded = value;
        }

        private static bool _loaded;

        internal static short MessageIndex => 2048;
        internal static short CommandIndex => 4096;
        internal static short RequestIndex => 6144;
        internal static short ReplyIndex => 8192;

        private static readonly Dictionary<int, INetMessage> NetMessages = new Dictionary<int, INetMessage>();
        private static readonly Dictionary<int, INetCommand> NetCommands = new Dictionary<int, INetCommand>();
        private static readonly Dictionary<int, RequestPerformerBase> NetRequests = new Dictionary<int, RequestPerformerBase>();

        public static bool RegisterMessageType<TMessage>() where TMessage : INetMessage, new() {
            var inst = new TMessage();
            var type = inst.GetType();

            int hash = GetNetworkHash(type);
            if (NetMessages.ContainsKey(hash)) {
                R2API.Logger.LogError("Tried to register a message type with a duplicate hash");
                return false;
            }
            else {
                NetMessages[hash] = inst;
                return true;
            }
        }

        public static bool RegisterCommandType<TCommand>() where TCommand : INetCommand, new() {
            var inst = new TCommand();
            var type = inst.GetType();
            int hash = GetNetworkHash(type);

            if (NetCommands.ContainsKey(hash)) {
                R2API.Logger.LogError("Tried to register a command type with a duplicate hash");
                return false;
            }
            else {
                NetCommands[hash] = inst;
                return true;
            }
        }

        public static bool RegisterRequestTypes<TRequest, TReply>()
            where TRequest : INetRequest<TRequest, TReply>, new()
            where TReply : INetRequestReply<TRequest, TReply>, new() {
            var request = new TRequest();
            var reply = new TReply();

            var requestType = request.GetType();
            int requestHash = GetNetworkHash(requestType);

            if (NetRequests.ContainsKey(requestHash)) {
                R2API.Logger.LogError("Tried to register a request type with a duplicate hash");
                return false;
            }
            NetRequests[requestHash] = new RequestPerformer<TRequest, TReply>(request, reply);
            return true;
        }

        [R2APISubmoduleInit(Stage = InitStage.SetHooks)]
        internal static void SetHooks() {
            RegisterMessageType<DamageMessage>();
            RegisterMessageType<BuffMessage>();
            RegisterMessageType<DotMessage>();

            RegisterMessageType<ExampleMessage>();
            RegisterRequestTypes<ExamplePing, ExamplePingReply>();

            GameNetworkManager.onStartServerGlobal += RegisterServerHandlers;
            GameNetworkManager.onStartClientGlobal += RegisterClientHandlers;

            GameNetworkManager.onStopServerGlobal -= UnRegisterServerHandlers;
            GameNetworkManager.onStopClientGlobal -= UnRegisterClientHandlers;
        }

        [R2APISubmoduleInit(Stage = InitStage.UnsetHooks)]
        internal static void UnsetHooks() {
            GameNetworkManager.onStartServerGlobal -= RegisterServerHandlers;
            GameNetworkManager.onStartClientGlobal -= RegisterClientHandlers;

            GameNetworkManager.onStopServerGlobal += UnRegisterServerHandlers;
            GameNetworkManager.onStopClientGlobal += UnRegisterClientHandlers;
        }

        private static void RegisterServerHandlers() {
            R2API.Logger.LogInfo("Server Handlers registered");
            NetworkServer.RegisterHandler(MessageIndex, HandleMessageServer);
            NetworkServer.RegisterHandler(CommandIndex, HandleCommandServer);
            NetworkServer.RegisterHandler(RequestIndex, HandleRequestServer);
            NetworkServer.RegisterHandler(ReplyIndex, HandleReplyServer);
        }

        private static void RegisterClientHandlers(NetworkClient client) {
            R2API.Logger.LogInfo("Client Handlers registered");
            client.RegisterHandler(MessageIndex, HandleMessageClient);
            client.RegisterHandler(CommandIndex, HandleCommandClient);
            client.RegisterHandler(RequestIndex, HandleRequestClient);
            client.RegisterHandler(ReplyIndex, HandleReplyClient);
        }

        private static void UnRegisterServerHandlers() {
            R2API.Logger.LogInfo("Server Handlers unregistered");
            NetworkServer.UnregisterHandler(MessageIndex);
            NetworkServer.UnregisterHandler(CommandIndex);
            NetworkServer.UnregisterHandler(RequestIndex);
            NetworkServer.UnregisterHandler(ReplyIndex);
        }

        private static void UnRegisterClientHandlers() {
            R2API.Logger.LogInfo("Client Handlers unregistered");

            foreach (var client in NetworkClient.allClients) {
                client.UnregisterHandler(MessageIndex);
                client.UnregisterHandler(CommandIndex);
                client.UnregisterHandler(RequestIndex);
                client.UnregisterHandler(ReplyIndex);
            }
        }

        /// <summary>
        /// <para>Used for generating and retrieving hash when registering messages.</para>
        /// <para>Also used when looking up the TypeCode when sending / retrieving the Header</para>
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal static int GetNetworkHash(Type type) => $"{type.Assembly.FullName}{type.FullName}".GetHashCode();

        private static NetworkWriter UniversalWriter {
            get;
        } = new NetworkWriter();

        internal static Writer GetWriter(short messageIndex, NetworkConnection target, QosType qos) => new Writer(UniversalWriter, messageIndex, target, qos);

        private static void HandleCommandServer(NetworkMessage mag) {
            NetworkReader reader = mag.reader;
            var header = reader.Read<Header>();

            if (header.Destination.ShouldRun()) {
                header.RemoveDestination(NetworkDestination.Server);

                if (NetCommands.TryGetValue(header.TypeCode, out INetCommand command)) {
                    command.OnReceived();
                }
                else {
                    R2API.Logger.LogError("Unhandled command received, you may be missing mods");
                }
            }

            if (header.Destination.ShouldSend()) {
                int receivedFromId = mag.conn.connectionId;
                for (int i = 0; i < NetworkServer.connections.Count; ++i) {
                    if (i == receivedFromId) {
                        continue;
                    }

                    NetworkConnection targetConn = NetworkServer.connections[i];
                    if (targetConn == null) {
                        continue;
                    }

                    using (Writer netWriter = GetWriter(CommandIndex, targetConn, QosType.Reliable)) {
                        NetworkWriter writer = netWriter;
                        writer.Write(header);
                    }
                }

            }
        }

        private static void HandleMessageServer(NetworkMessage msg) {
            NetworkReader reader = msg.reader;
            var header = reader.Read<Header>();

            if (header.Destination.ShouldRun()) {
                header.RemoveDestination(NetworkDestination.Server);

                if (NetMessages.TryGetValue(header.TypeCode, out INetMessage message)) {
                    message.Deserialize(reader);
                    message.OnReceived();
                }
                else {
                    R2API.Logger.LogError("Unhandled message received, you may be missing mods");
                }
            }

            if (header.Destination.ShouldSend()) {
                int receivedFrom = msg.conn.connectionId;
                byte[] bytes = reader.ReadBytes((int)(reader.Length - reader.Position));
                for (int i = 0; i < NetworkServer.connections.Count; ++i) {
                    if (i == receivedFrom) {
                        continue;
                    }

                    NetworkConnection conn = NetworkServer.connections[i];
                    if (conn == null) {
                        continue;
                    }

                    using (Writer netWriter = GetWriter(MessageIndex, conn, QosType.Reliable)) {
                        NetworkWriter writer = netWriter;
                        writer.Write(header);
                        writer.WriteBytesFull(bytes);
                    }
                }
            }
        }

        private static void HandleRequestServer(NetworkMessage msg) {
            NetworkReader reader = msg.reader;
            var header = reader.Read<Header>();

            if (header.Destination.ShouldRun()) {
                header.RemoveDestination(NetworkDestination.Server);

                if (NetRequests.TryGetValue(header.TypeCode, out RequestPerformerBase requestPerformer)) {
                    var reply = requestPerformer.PerformRequest(reader);
                    var replyHeader = new Header(header.TypeCode, NetworkDestination.Clients);

                    using (Writer netWriter = GetWriter(ReplyIndex, msg.conn, QosType.Reliable)) {
                        NetworkWriter writer = netWriter;
                        writer.Write(replyHeader);
                        writer.Write(reply);
                    }
                }
                else {
                    R2API.Logger.LogError("Unhandled request message received, you may be missing mods");
                }
            }

            if (header.Destination.ShouldSend()) {
                int receivedFrom = msg.conn.connectionId;
                var bytes = reader.ReadBytes((int)(reader.Length - reader.Position));
                for (var i = 0; i < NetworkServer.connections.Count; ++i) {
                    if (i == receivedFrom) {
                        continue;
                    }

                    NetworkConnection conn = NetworkServer.connections[i];
                    if (conn == null) {
                        continue;
                    }

                    using (Writer netWriter = GetWriter(RequestIndex, conn, QosType.Reliable)) {
                        NetworkWriter writer = netWriter;
                        writer.Write(header);
                        writer.WriteBytesFull(bytes);
                    }
                }
            }
        }

        private static void HandleReplyServer(NetworkMessage msg) {
            NetworkReader reader = msg.reader;
            var header = reader.Read<Header>();

            if (header.Destination.ShouldRun()) {
                header.RemoveDestination(NetworkDestination.Server);

                if (NetRequests.TryGetValue(header.TypeCode, out RequestPerformerBase requestPerformer)) {
                    requestPerformer.PerformReply(reader);
                }
                else {
                    R2API.Logger.LogError("Unhandled reply received, you may be missing mods");
                }
            }

            if (header.Destination.ShouldSend()) {
                int receivedFrom = msg.conn.connectionId;
                for (var i = 0; i < NetworkServer.connections.Count; ++i) {
                    if (i == receivedFrom) {
                        continue;
                    }

                    NetworkConnection conn = NetworkServer.connections[i];
                    if (conn == null) {
                        continue;
                    }

                    using (Writer netWriter = GetWriter(RequestIndex, conn, QosType.Reliable)) {
                        NetworkWriter writer = netWriter;
                        writer.Write(header);
                    }
                }
            }
        }

        private static void HandleCommandClient(NetworkMessage msg) {
            NetworkReader reader = msg.reader;
            var header = reader.Read<Header>();

            if (header.Destination.ShouldRun()) {
                header.RemoveDestination(NetworkDestination.Clients);

                if (NetCommands.TryGetValue(header.TypeCode, out INetCommand command)) {
                    command.OnReceived();
                }
                else {
                    R2API.Logger.LogError("Unhandled command received, you may be missing mods");
                }
            }
        }

        private static void HandleMessageClient(NetworkMessage msg) {
            NetworkReader reader = msg.reader;
            var header = reader.Read<Header>();

            if (header.Destination.ShouldRun()) {
                header.RemoveDestination(NetworkDestination.Clients);

                if (NetMessages.TryGetValue(header.TypeCode, out INetMessage message)) {
                    message.Deserialize(reader);
                    message.OnReceived();
                }
                else {
                    R2API.Logger.LogError("Unhandled message received, you may be missing mods");
                }
            }
        }

        private static void HandleRequestClient(NetworkMessage msg) {
            NetworkReader reader = msg.reader;
            var header = reader.Read<Header>();

            if (header.Destination.ShouldRun()) {
                header.RemoveDestination(NetworkDestination.Clients);

                if (NetRequests.TryGetValue(header.TypeCode, out RequestPerformerBase requestPerformer)) {
                    var reply = requestPerformer.PerformRequest(reader);
                    var replyHeader = new Header(header.TypeCode, NetworkDestination.Clients);

                    using (Writer netWriter = GetWriter(ReplyIndex, msg.conn, QosType.Reliable)) {
                        NetworkWriter writer = netWriter;
                        writer.Write(replyHeader);
                        writer.Write(reply);
                    }
                }
                else {
                    R2API.Logger.LogError("Unhandled request message received, you may be missing mods");
                }
            }
        }

        private static void HandleReplyClient(NetworkMessage msg) {
            NetworkReader reader = msg.reader;
            var header = reader.Read<Header>();

            if (header.Destination.ShouldRun()) {
                header.RemoveDestination(NetworkDestination.Clients);

                if (NetRequests.TryGetValue(header.TypeCode, out RequestPerformerBase requestPerformer)) {
                    requestPerformer.PerformReply(reader);
                }
                else {
                    R2API.Logger.LogError("Unhandled reply received, you may be missing mods");
                }
            }
        }
    }
}
