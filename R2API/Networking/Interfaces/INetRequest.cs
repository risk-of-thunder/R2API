using System;
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

        private static void SendRequest<TRequest, TReply>(TRequest request, Header header, NetworkConnection conn)
            where TRequest : INetRequest<TRequest, TReply>
            where TReply : INetRequestReply<TRequest, TReply> {
            using (Writer netWriter = NetworkingAPI.GetWriter(NetworkingAPI.RequestIndex, conn, QosType.Reliable)) {
                NetworkWriter writer = netWriter;
                writer.Write(header);
                writer.Write(request);
            }
        }

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

                        SendRequest<TRequest, TReply>(request, header, conn);
                    }
                }
                else if (NetworkClient.active) {
                    SendRequest<TRequest, TReply>(request, header, ClientScene.readyConnection);
                }
            }
        }

        public static void Send<TRequest, TReply>(this TRequest request, NetworkConnection target)
            where TRequest : INetRequest<TRequest, TReply>
            where TReply : INetRequestReply<TRequest, TReply> {
            if (target == null) {
                throw new ArgumentNullException(nameof(target));
            }

            if (!NetworkServer.active) {
                throw new InvalidOperationException("NetworkServer is not active.");
            }

            if (NetworkClient.active) {
                foreach (var networkClient in NetworkClient.allClients) {
                    if (networkClient.connection != null && networkClient.connection.connectionId == target.connectionId) {
                        request.OnRequestReceived().OnReplyReceived();
                        return;
                    }
                }
            }

            var header = NetworkDestination.Clients.GetHeader(NetworkingAPI.GetNetworkHash(request.GetType()));

            SendRequest<TRequest, TReply>(request, header, target);
        }
    }
}
