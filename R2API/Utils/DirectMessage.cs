using RoR2;
using System;
using UnityEngine.Networking;

namespace R2API.Utils {
    /// <summary>
    /// Class for sending messages directly to singular clients. Mostly useful for Dedicated servers.
    /// </summary>
    public static class DirectMessage {
        /// <summary>
		/// Converts NetworkUser to NetworkConnection
		/// </summary>
		/// <param name="user"></param>
		/// <returns>NetworkUser's NetworkConnection</returns>
		private static UnityEngine.Networking.NetworkConnection ResolveUserToConnection(NetworkUser user) {
            RoR2.Networking.GameNetworkManager[] networkManagers = (RoR2.Networking.GameNetworkManager[])UnityEngine.Object.FindObjectsOfType(typeof(RoR2.Networking.GameNetworkManager));

            foreach (var networkManager in networkManagers) {
                UnityEngine.Networking.NetworkConnection networkConnection = networkManager.GetClient(user.GetNetworkPlayerName().steamId);

                if (networkConnection != null) {
                    return networkConnection;
                }
            }

            return null;
        }

        /// <summary>
        /// Sends a string directly to a connection. Useful for when you don't want to take advantage of any preformatted string found in RoR2.Chat.
        /// </summary>
        /// <param name="message">The message to send</param>
        /// <param name="connection">The network connection to send to.</param>
        public static void SendDirectMessage(string message, NetworkConnection connection) {
            SendDirectMessage(new Chat.SimpleChatMessage() { baseToken = "{0}", paramTokens = new[] { message } }, connection);
        }

        /// <summary>
        /// Sends a string directly to a user. Useful for when you don't want to take advantage of any preformatted string found in RoR2.Chat.
        /// </summary>
        /// <param name="message">The message to send</param>
        /// <param name="user">The network user to send to.</param>
        public static void SendDirectMessage(string message, RoR2.NetworkUser user) {
            SendDirectMessage(message, ResolveUserToConnection(user));
        }

        /// <summary>
        /// Sends a ChatMessage directly to a connection. Checkout RoR2.Chat for possible chatmessage types.
        /// </summary>
        /// <param name="message">The message to send</param>
        /// <param name="connection">The network connection to send to.</param>
        public static void SendDirectMessage(Chat.ChatMessageBase message, NetworkConnection connection) {
            NetworkWriter writer = new NetworkWriter();
            writer.StartMessage((short)59);
            writer.Write(message.GetTypeIndex());
            writer.Write((MessageBase)message);
            writer.FinishMessage();
            connection.SendWriter(writer, RoR2.Networking.QosChannelIndex.chat.intVal);
        }
    }

}
