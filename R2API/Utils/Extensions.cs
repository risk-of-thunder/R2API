using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RoR2;
using RoR2.Networking;
using UnityEngine;
using UnityEngine.Networking;

namespace R2API.Utils {
    public static class Extensions {
        /// <summary>
        /// Check if a string contains another string without worrying about case sensitivity
        /// </summary>
        /// <param name="s">String to check</param>
        /// <param name="compare">String to compare with</param>
        /// <returns></returns>
        public static bool ContainsIgnoreCase(this string s, string compare) {
            return s.IndexOf(compare, StringComparison.CurrentCultureIgnoreCase) >= 0;
        }

        /// <summary>
        /// Short-hand method for using equals ignoring case
        /// </summary>
        /// <param name="s">String to check</param>
        /// <param name="compare">String to compare with</param>
        /// <returns></returns>
        public static bool Same(this string s, string compare) {
            return s.Equals(compare, StringComparison.CurrentCultureIgnoreCase);
        }

        /// <summary>
        /// Short-hand method for checking an array of strings against one string, returns true if ANY of them are the same
        /// </summary>
        /// <param name="s">Array of strings</param>
        /// <param name="compare">String to compare with</param>
        /// <returns></returns>
        public static bool SameAny(this string s, params string[] compare) {
            return compare.ToList().Any(x => x.Same(s));
        }

        /// <summary>
        /// Send a direct message to a specific player
        /// </summary>
        /// <param name="nu">Player's NetworkUser</param>
        /// <param name="message">Message to send</param>
        public static void SendDirectMessage(this NetworkUser nu, string message) {
            var msg = new Chat.SimpleChatMessage();
            message += "<color=#ffffff></color>";
            msg.baseToken = message;
            var networkWriter = new NetworkWriter();
            networkWriter.StartMessage(59);
            networkWriter.Write(msg.GetTypeIndex());
            networkWriter.Write(msg);
            networkWriter.FinishMessage();

            nu.connectionToClient?.SendWriter(networkWriter, QosChannelIndex.chat.intVal);
        }

        /// <summary>
        /// Get's the string from the game's language set from the string
        /// </summary>
        /// <param name="token">String Token to fetch</param>
        /// <returns></returns>
        public static string FromLang(this string token) {
            return Language.GetString(token);
        }

        /// <summary>
        /// Adjust a Vector3 values and return the adjusted one
        /// </summary>
        /// <param name="v">Vector3 to Adjust</param>
        /// <param name="x">Amount to adjust X by</param>
        /// <param name="y">Amount to adjust Y by</param>
        /// <param name="z">Amount to adjust Z by</param>
        /// <returns></returns>
        public static Vector3 Offset(this Vector3 v, float x = 0f, float y = 0f, float z = 0f) {
            v.x += x;
            v.y += y;
            v.z += z;
            return v;
        }

        public static string EnumName<T>(this object n) {
            return Enum.GetName(typeof(T), n);
        }

        public static T ToEnum<T>(this string n) {
            return (T)Enum.Parse(typeof(T), n, true);
        }
    }
}
