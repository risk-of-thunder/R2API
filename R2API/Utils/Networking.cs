using RoR2;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace R2API.Utils {

    /// <summary>
    /// Provides utility functions for networking.
    /// </summary>
    public static class Networking {
        /// <summary>
        /// This function is not performance friendly. It is recommended you cache the result.
        /// </summary>
        /// <param name="user">The networkuser to search for.</param>
        /// <returns></returns>
        public static NetworkConnection GetNetworkConnectionFromNetworkUser(NetworkUser user) {
            foreach(NetworkConnection conn in NetworkServer.connections) {
                List<PlayerController> playerControllers = conn.playerControllers;
                for (int index = 0; index < playerControllers.Count; ++index) {
                    if (playerControllers[index].gameObject.GetComponent<NetworkUser>() == user) {
                        return conn;
                    }
                }
            }
            return null;
        }
    }
}
