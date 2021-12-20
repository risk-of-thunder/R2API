using System;
using UnityEngine.Networking;

namespace R2API.Networking.Interfaces {

    /// <summary>
    /// Interface for network requests which will execute <see cref="OnRequestReceived"/> when received by the targeted machine(s).
    /// Must be used in conjunction with <see cref="INetRequestReply{TRequest, TReply}"/>"/>.
    /// Check <seealso cref="Messages.ExamplePing"/> for an example implementation.
    /// <inheritdoc cref="ISerializableObject"/>
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    /// <typeparam name="TReply"></typeparam>
    public interface INetRequest<TRequest, TReply> : ISerializableObject
        where TRequest : INetRequest<TRequest, TReply>
        where TReply : INetRequestReply<TRequest, TReply> {

        /// <summary>
        /// Executed when received by the targeted machine(s).
        /// </summary>
        /// <returns></returns>
        TReply OnRequestReceived();
    }

    /// <summary>
    /// Interface for network replies which will execute <see cref="OnReplyReceived"/>
    /// after the original target received and executed <see cref="INetRequest{TRequest, TReply}.OnRequestReceived"/>.
    /// Must be used in conjunction with <see cref="INetRequest{TRequest, TReply}"/>"/>.
    /// Check <seealso cref="Messages.ExamplePingReply"/> for an example implementation.
    /// <inheritdoc cref="ISerializableObject"/>
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    /// <typeparam name="TReply"></typeparam>
    public interface INetRequestReply<TRequest, TReply> : ISerializableObject
        where TRequest : INetRequest<TRequest, TReply>
        where TReply : INetRequestReply<TRequest, TReply> {

        /// <summary>
        /// Executed by the original sender of the <see cref="INetRequest{TRequest, TReply}"/>.
        /// </summary>
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

        /// <summary>
        /// Send the passed request over the network
        /// </summary>
        /// <param name="request">Registered request</param>
        /// <param name="destination">Destination of the request</param>
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

        /// <summary>
        /// <inheritdoc cref="Send{TRequest, TReply}(TRequest, NetworkDestination)"/>
        /// to a specific NetworkConnection, only callable from server.
        /// You can retrieve a <see cref="NetworkConnection"/> from <see cref="NetworkServer.connections"/> or
        /// from a <see cref="NetworkBehaviour.connectionToClient"/> field.
        /// </summary>
        /// <param name="request">Registered request</param>
        /// <param name="target">NetworkConnection the request will be sent to.</param>
        /// <exception cref="ArgumentNullException">Thrown when target is null</exception>
        /// <exception cref="InvalidOperationException">Thrown if not called from server</exception>
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
