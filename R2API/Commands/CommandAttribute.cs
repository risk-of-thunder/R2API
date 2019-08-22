using System;

namespace R2API.Commands
{
    /// <summary>
    ///     Specifies that a method is a command.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class CommandAttribute : Attribute
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="CommandAttribute" /> class with the specified name and syntax.
        /// </summary>
        /// <param name="name">The name, which must not be null.</param>
        /// <param name="syntax">The synatx, which must not be null.</param>
        /// <param name="permission">The permission, can be null</param>
        /// <exception cref="ArgumentNullException">
        ///     Either <paramref name="name" /> or <paramref name="syntax" />is null.
        /// </exception>
        public CommandAttribute(string name, string syntax, string permission = "")
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Syntax = syntax ?? throw new ArgumentNullException(nameof(syntax));
            Permission = permission;
        }

        /// <summary>
        ///     Gets or sets the alias.
        /// </summary>
        public string[] Alias { get; set; }

        /// <summary>
        ///     Gets or sets the help text.
        /// </summary>
        public string HelpText { get; set; }

        /// <summary>
        ///     Gets or sets whether the command is local or not
        /// </summary>
        public bool Local { get; set; } = false;

        /// <summary>
        ///     Gets the name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     Gets the syntax.
        /// </summary>
        public string Syntax { get; }

        /// <summary>
        ///     Gets the permission.
        /// </summary>
        public string Permission { get; }
    }
}
