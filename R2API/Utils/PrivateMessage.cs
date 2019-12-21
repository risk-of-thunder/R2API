using RoR2;
using System;
using UnityEngine.Networking;

namespace R2API.Utils {
    static class PrivateMessage {

        [Obsolete("Not really obsolete, but try caching your network connection instead!")]
        public static void SendPrivateMessage(string message, NetworkUser networkUser) {
            SendPrivateMessage(message, Networking.GetNetworkConnectionFromNetworkUser(networkUser));
        }

        public static void SendPrivateMessage(string message, NetworkConnection connection) {
            SendPrivateMessage(new Chat.SimpleChatMessage() { baseToken = "{0}", paramTokens = new[] { message } }, connection);
        }

        public static void SendPrivateMessage(Chat.ChatMessageBase message, NetworkConnection connection) {
            NetworkWriter writer = new NetworkWriter();
            writer.StartMessage((short)59);
            writer.Write(message.GetTypeIndex());
            writer.Write((MessageBase)message);
            writer.FinishMessage();
            connection.SendWriter(writer, RoR2.Networking.QosChannelIndex.chat.intVal);
        }
    }

}
