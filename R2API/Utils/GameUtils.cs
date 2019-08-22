using System;
using System.Collections.Generic;
using System.Text;
using RoR2;
using RoR2.Networking;

namespace R2API.Utils {
    public class GameUtils {

        /// <summary>
        /// Sends a message to all connected players in the chat box.
        /// </summary>
        /// <param name="message">message to send</param>
        public static void SendChat(string message) {
            Chat.SendBroadcastChat(new Chat.PlayerChatMessage() {
                networkPlayerName = new NetworkPlayerName() {
                    steamId = new CSteamID(1234),
                    nameOverride = "user"
                },
                baseToken = message
            }, QosChannelIndex.chat.intVal);
        }

        /// <summary>
        /// Finds the player master controller with the given display name.
        /// </summary>
        /// <param name="playerName">Display name to find</param>
        /// <param name="caseSensitive">Whether or not to ignore case sensitivity or not, this makes the check look via contains rather than equals</param>
        /// <returns></returns>
        public static PlayerCharacterMasterController GetPlayerWithName(string playerName, bool caseSensitive = true) {
            foreach (var player in GetAllPlayers()) {
                if (player != null && !string.IsNullOrWhiteSpace(player.GetDisplayName())) {
                    var name = player.networkUser.userName;
                    if (caseSensitive) {
                        if (name == playerName) {
                            return player;
                        }
                    }
                    else {
                        if (name.ContainsIgnoreCase(playerName)) {
                            return player;
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Finds the network user with the given display name.
        /// </summary>
        /// <param name="playerName">Display name to find</param>
        /// <param name="caseSensitive">Whether or not to ignore case sensitivity or not, this makes the check look via contains rather than equals</param>
        /// <returns></returns>
        public static NetworkUser GetNetworkUser(string playerName, bool caseSensitive = true) {
            foreach (var player in GetAllNetworkUsers()) {
                if (player != null && !string.IsNullOrWhiteSpace(player.userName)) {
                    var name = player.userName;
                    if (caseSensitive) {
                        if (name == playerName) {
                            return player;
                        }
                    }
                    else {
                        if (name.ContainsIgnoreCase(playerName)) {
                            return player;
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Get all the players
        /// </summary>
        /// <returns>List of PlayerCharacterMasterControllers</returns>
        public static IEnumerable<PlayerCharacterMasterController> GetAllPlayers() {
            return PlayerCharacterMasterController.instances;
        }

        /// <summary>
        /// Get all the network users
        /// </summary>
        /// <returns>List of NetworkUsers</returns>
        public static IEnumerable<NetworkUser> GetAllNetworkUsers() {

            return RoR2.NetworkUser.readOnlyInstancesList;
        }
    }
}
