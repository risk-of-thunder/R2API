using System;

namespace R2API.Commands
{
    public class DefaultPermissionAttribute : Attribute
    {
        /// <summary>
        /// Set any default permissions for your commands class, otherwise commands with permissions will always return unauthorized
        /// </summary>
        /// <param name="permissions">Permissions to add</param>
        public DefaultPermissionAttribute(params string[] permissions)
        {
            Permissions = permissions;
        }

        public string[] Permissions { get; }
    }
}
