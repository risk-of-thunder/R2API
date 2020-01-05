using RoR2;
using System;
using UnityEngine.Networking;

namespace R2API.Utils {
    /// <summary>
    /// Class for sending messages directly to singular clients. Mostly useful for Dedicated servers.
    /// </summary>
    public static class DirectMessage {

        /// <summary>
        /// Sends a string directly to a connection. Useful for when you don't want to take advantage of any preformatted string found in RoR2.Chat.
        /// </summary>
        /// <param name="message">The message to send</param>
        /// <param name="connection">The network connection to send to. If you can figure out how to convert a networkuser into a NetworkConnection, include it in r2api pls.</param>
        public static void SendDirectMessage(string message, NetworkConnection connection) {
            SendDirectMessage(new Chat.SimpleChatMessage() { baseToken = "{0}", paramTokens = new[] { message } }, connection);
        }

        /// <summary>
        /// Sends a ChatMessage directly to a connection. Checkout RoR2.Chat for possible chatmessage types.
        /// </summary>
        /// <param name="message">The message to send</param>
        /// <param name="connection">The network connection to send to. If you can figure out how to convert a networkuser into a NetworkConnection, include it in r2api pls.</param>
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
