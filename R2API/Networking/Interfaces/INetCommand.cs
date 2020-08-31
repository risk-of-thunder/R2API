using UnityEngine.Networking;

namespace R2API.Networking.Interfaces {
    public interface INetCommand {
        void OnReceived();
    }

    public static class NetCommandExtensions {
        public static void Send(this INetCommand command, NetworkDestination destination) {
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

                        using (Writer netWriter = NetworkingAPI.GetWriter(NetworkingAPI.CommandIndex, conn, QosType.Reliable)) {
                            NetworkWriter writer = netWriter;
                            writer.Write(header);
                        }
                    }
                } else if (NetworkClient.active) {
                    using (var netWriter = NetworkingAPI.GetWriter(NetworkingAPI.CommandIndex, ClientScene.readyConnection, QosType.Reliable)) {
                        NetworkWriter writer = netWriter;
                        writer.Write(header);
                    }
                }
            }
        }
    }
}
