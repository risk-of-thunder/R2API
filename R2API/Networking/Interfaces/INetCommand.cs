using System;
using UnityEngine.Networking;

namespace R2API.Networking.Interfaces {

    public interface INetCommand {

        void OnReceived();
    }

    public static class NetCommandExtensions {

        private static void SendCommand(Header header, NetworkConnection conn) {
            using (Writer netWriter = NetworkingAPI.GetWriter(NetworkingAPI.CommandIndex, conn, QosType.Reliable)) {
                NetworkWriter writer = netWriter;
                writer.Write(header);
            }
        }

        public static void Send(this INetCommand? command, NetworkDestination destination) {
            if (destination.ShouldRun()) {
                command.OnReceived();
            }

            if (destination.ShouldSend()) {
                var header = destination.GetHeader(NetworkingAPI.GetNetworkHash(command.GetType()));

                if (NetworkServer.active) {
                    for (int i = 0; i < NetworkServer.connections.Count; ++i) {
                        NetworkConnection conn = NetworkServer.connections[i];
                        if (conn == null) {
                            continue;
                        }

                        if (NetworkServer.localClientActive && NetworkServer.localConnections.Contains(conn)) {
                            continue;
                        }

                        SendCommand(header, conn);
                    }
                }
                else if (NetworkClient.active) {
                    SendCommand(header, ClientScene.readyConnection);
                }
            }
        }

        public static void Send(this INetCommand? command, NetworkConnection target) {
            if (target == null) {
                throw new ArgumentNullException(nameof(target));
            }

            if (!NetworkServer.active) {
                throw new InvalidOperationException("NetworkServer is not active.");
            }

            if (NetworkClient.active) {
                foreach (var networkClient in NetworkClient.allClients) {
                    if (networkClient.connection != null && networkClient.connection.connectionId == target.connectionId) {
                        command.OnReceived();
                        return;
                    }
                }
            }

            var header = NetworkDestination.Clients.GetHeader(NetworkingAPI.GetNetworkHash(command.GetType()));

            SendCommand(header, target);
        }
    }
}
