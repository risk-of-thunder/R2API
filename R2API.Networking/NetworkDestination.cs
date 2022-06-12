using System;
using UnityEngine.Networking;

namespace R2API.Networking {

    [Flags]
    public enum NetworkDestination : byte {
        Clients = 1 << 0,
        Server = 1 << 1,
    }

    internal static class NetworkDestinationExtensions {

        internal static bool ShouldSend(this NetworkDestination dest) {
            var isServer = NetworkServer.active;

            return !(isServer && dest == NetworkDestination.Server);
        }

        internal static bool ShouldRun(this NetworkDestination dest) {
            var isServer = NetworkServer.active;
            var isClient = NetworkClient.active;

            return isServer && (dest & NetworkDestination.Server) != 0 ||
                   isClient && (dest & NetworkDestination.Clients) != 0;
        }

        internal static Header GetHeader(this NetworkDestination dest, int typeCode) => new Header(typeCode, dest);
    }
}
