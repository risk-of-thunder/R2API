using System;
using UnityEngine.Networking;

namespace R2API.Networking.Interfaces;

/// <summary>
/// <inheritdoc cref="INetCommand"/>
/// <inheritdoc cref="ISerializableObject"/>
/// </summary>
public interface INetMessage : ISerializableObject
{
    /// <inheritdoc cref="INetCommand.OnReceived"/>
    void OnReceived();
}

public static class NetMessageExtensions
{

    private static void SendMessage(INetMessage? message, Header header, NetworkConnection conn)
    {
        using (Writer netWriter = NetworkingAPI.GetWriter(NetworkingAPI.MessageIndex, conn, QosType.Reliable))
        {
            NetworkWriter writer = netWriter;
            writer.Write(header);
            writer.Write(message);
        }
    }

    /// <summary>
    /// Send the passed message over the network
    /// </summary>
    /// <param name="message">Registered message</param>
    /// <param name="destination">Destination of the message</param>
    public static void Send(this INetMessage? message, NetworkDestination destination)
    {
        if (destination.ShouldRun())
        {
            message.OnReceived();
        }

        if (destination.ShouldSend())
        {
            var header = destination.GetHeader(NetworkingAPI.GetNetworkHash(message.GetType()));

            if (NetworkServer.active)
            {
                for (int i = 0; i < NetworkServer.connections.Count; ++i)
                {
                    NetworkConnection conn = NetworkServer.connections[i];
                    if (conn == null)
                    {
                        continue;
                    }

                    if (NetworkServer.localClientActive && NetworkServer.localConnections.Contains(conn))
                    {
                        continue;
                    }

                    SendMessage(message, header, conn);
                }
            }
            else if (NetworkClient.active)
            {
                SendMessage(message, header, ClientScene.readyConnection);
            }
        }
    }

    /// <summary>
    /// <inheritdoc cref="Send(INetMessage?, NetworkDestination)"/>
    /// to a specific NetworkConnection, only callable from server.
    /// You can retrieve a <see cref="NetworkConnection"/> from <see cref="NetworkServer.connections"/> or
    /// from a <see cref="NetworkBehaviour.connectionToClient"/> field.
    /// </summary>
    /// <param name="message">Registered message</param>
    /// <param name="target">NetworkConnection the message will be sent to.</param>
    /// <exception cref="ArgumentNullException">Thrown when target is null</exception>
    /// <exception cref="InvalidOperationException">Thrown if not called from server</exception>
    public static void Send(this INetMessage? message, NetworkConnection target)
    {
        if (target == null)
        {
            throw new ArgumentNullException(nameof(target));
        }

        if (!NetworkServer.active)
        {
            throw new InvalidOperationException("NetworkServer is not active.");
        }

        if (NetworkClient.active)
        {
            foreach (var networkClient in NetworkClient.allClients)
            {
                if (networkClient.connection != null && networkClient.connection.connectionId == target.connectionId)
                {
                    message.OnReceived();
                    return;
                }
            }
        }

        var header = NetworkDestination.Clients.GetHeader(NetworkingAPI.GetNetworkHash(message.GetType()));

        SendMessage(message, header, target);
    }
}
