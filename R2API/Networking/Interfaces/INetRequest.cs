using UnityEngine.Networking;

namespace R2API.Networking.Interfaces {
    public interface INetRequest<TRequest, TReply> : ISerializableObject
        where TRequest : INetRequest<TRequest, TReply>
        where TReply : INetRequestReply<TRequest, TReply> {
        TReply OnRequestReceived();
    }

    public interface INetRequestReply<TRequest, TReply> : ISerializableObject
        where TRequest : INetRequest<TRequest, TReply>
        where TReply : INetRequestReply<TRequest, TReply> {
        void OnReplyReceived();
    }

    public static class NetRequestExtensions {
        public static void Send<TRequest, TReply>(this TRequest request, NetworkDestination destination)
            where TRequest : INetRequest<TRequest, TReply>
            where TReply : INetRequestReply<TRequest, TReply> {
            if (destination.ShouldRun()) {
                request.OnRequestReceived().OnReplyReceived();
            }

            if (destination.ShouldSend()) {
                var header = destination.GetHeader(NetworkingAPI.GetNetworkHash(request.GetType()));

                if (NetworkServer.active) {
                    for (var i = 0; i < NetworkServer.connections.Count; ++i) {
                        NetworkConnection conn = NetworkServer.connections[i];
                        if (conn == null) {
                            continue;
                        }

                        if (NetworkServer.localClientActive && NetworkServer.localConnections.Contains(conn)) {
                            continue;
                        }

                        using (Writer netWriter = NetworkingAPI.GetWriter(NetworkingAPI.RequestIndex, conn, QosType.Reliable)) {
                            NetworkWriter writer = netWriter;
                            writer.Write(header);
                            writer.Write(request);
                        }
                    }
                } else if (NetworkClient.active) {
                    using (Writer netWriter = NetworkingAPI.GetWriter(NetworkingAPI.RequestIndex, ClientScene.readyConnection, QosType.Reliable)) {
                        NetworkWriter writer = netWriter;
                        writer.Write(header);
                        writer.Write(request);
                    }
                }
            }
        }
    }
}
