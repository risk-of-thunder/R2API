using UnityEngine.Networking;

namespace R2API.Networking.Interfaces {
    public interface INetMessage : ISerializableObject {
        void OnReceived();
    }

    public static class NetMessageExtensions {
        public static void Send(this INetMessage message, NetworkDestination destination) {
            if (destination.ShouldRun()) {
                message.OnReceived();
            }

            if (destination.ShouldSend()) {
                var header = destination.GetHeader(NetworkingAPI.GetNetworkHash(message.GetType()));

                if (NetworkServer.active) {
                    for (int i = 0; i < NetworkServer.connections.Count; ++i) {
                        NetworkConnection conn = NetworkServer.connections[i];
                        if (conn == null) {
                            continue;
                        }

                        using (Writer netWriter = NetworkingAPI.GetWriter(NetworkingAPI.MessageIndex, conn, QosType.Reliable)) {
                            NetworkWriter writer = netWriter;
                            writer.Write(header);
                            writer.Write(message);
                        }
                    }
                }

                if (NetworkClient.active) {
                    using (Writer netWriter = NetworkingAPI.GetWriter(NetworkingAPI.MessageIndex, ClientScene.readyConnection, QosType.Reliable)) {
                        NetworkWriter writer = netWriter;
                        writer.Write(header);
                        writer.Write(message);
                    }
                }
            }
        }
    }
}
