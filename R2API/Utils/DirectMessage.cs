using RoR2;
using System;
using UnityEngine.Networking;

namespace R2API.Utils {
    static class DirectMessage {

        [Obsolete("Not really obsolete, but try caching your network connection instead!")]
        public static void SendDirectMessage(string message, NetworkUser networkUser) {
            SendDirectMessage(message, Networking.GetNetworkConnectionFromNetworkUser(networkUser));
        }

        [Obsolete("Not really obsolete, but try caching your network connection instead!")]
        public static void SendDirectMessage(Chat.ChatMessageBase message, NetworkUser networkUser) {
            SendDirectMessage(message, Networking.GetNetworkConnectionFromNetworkUser(networkUser));
        }

        public static void SendDirectMessage(string message, NetworkConnection connection) {
            SendDirectMessage(new Chat.SimpleChatMessage() { baseToken = "{0}", paramTokens = new[] { message } }, connection);
        }

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
