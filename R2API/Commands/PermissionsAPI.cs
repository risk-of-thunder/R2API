using System.Collections.Generic;
using System.Linq;
using R2API.Utils;
using RoR2;

namespace R2API.Commands
{
    /// <summary>
    /// Really basic Permissions Manager
    /// </summary>
    public class PermissionsAPI
    {
        public static Dictionary<NetworkUser, List<string>> Permissions { get; private set; }
        private static List<string> DefaultPermissions { get; set; }
        public PermissionsAPI()
        {
            Permissions = new Dictionary<NetworkUser, List<string>>();
            DefaultPermissions = new List<string>();
        }

        public void AddDefault(NetworkUser user)
        {
            if(!Permissions.ContainsKey(user))
            {
                Permissions.Add(user, DefaultPermissions);
            }
        }

        public void AddDefaultPermission(string permission)
        {
            if (!DefaultPermissions.Contains(permission))
            {
                DefaultPermissions.Add(permission);
            }
        }
    }

    public static class PermissionsManagerExtensions
    {
        /// <summary>
        /// Check if a player has a permission
        /// </summary>
        /// <param name="user">User to check</param>
        /// <param name="permission">Permission to check</param>
        /// <returns></returns>
        public static bool HasPermission(this NetworkUser user, string permission)
        {
            if (string.IsNullOrWhiteSpace(permission))
                return true;
            return PermissionsAPI.Permissions.ContainsKey(user) && PermissionsAPI.Permissions[user].Any(x => x.Same(permission));
        }

        /// <summary>
        /// Get all a users permissions
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public static List<string> GetPermissions(this NetworkUser user)
        {
            return PermissionsAPI.Permissions.ContainsKey(user) ? PermissionsAPI.Permissions[user] : null;
        }

        /// <summary>
        /// Add permissions to a user
        /// </summary>
        /// <param name="user">User to add to</param>
        /// <param name="permission">Permission to add</param>
        public static void AddPermission(this NetworkUser user, params string[] permission)
        {
            foreach (var s in permission)
            {
                if (PermissionsAPI.Permissions.ContainsKey(user))
                {
                    if (PermissionsAPI.Permissions[user].All(x => !x.Same(s)))
                    {
                        PermissionsAPI.Permissions[user].Add(s);
                    }
                }
            }
        }

        /// <summary>
        /// Remove a permission from a user
        /// </summary>
        /// <param name="user">User to remove from</param>
        /// <param name="permission">Permission to remove</param>
        public static void RemovePermission(this NetworkUser user, string permission)
        {
            if (PermissionsAPI.Permissions.ContainsKey(user))
            {
                if (PermissionsAPI.Permissions[user].Any(x => x.Same(permission)))
                {
                    PermissionsAPI.Permissions[user].Remove(permission);
                }
            }
        }
    }
}
