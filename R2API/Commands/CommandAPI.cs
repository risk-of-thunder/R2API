using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using R2API.Utils;
using RoR2;

namespace R2API.Commands
{
    /// <summary>
    ///     Manages commands.
    /// </summary>
    public sealed class CommandAPI
    {
        private static readonly Regex CamelCaseRegex = new Regex("(?<=[a-z])([A-Z])");

        private readonly List<Command> _commands = new List<Command>();
        private readonly Dictionary<Type, Parser> _parsers = new Dictionary<Type, Parser>();

        /// <summary>
        /// List of command objects for the manager to register, this is for plugins that load before this library
        /// </summary>
        public static List<object> CommandObjects = new List<object>();

        public static bool Initialised;
        public CommandAPI()
        {
            if (Initialised)
            {
                return;
            }

            Initialised = true;

            if (CommandObjects.Any())
            {
                CommandObjects.ForEach(Register);
            }

            AddParser(typeof(int), s => int.TryParse(s, out var result) ? (object)result : null);
            AddParser(typeof(uint), s => uint.TryParse(s, out var result) ? (object)result : null);
            AddParser(typeof(byte), s => byte.TryParse(s, out var result) ? (object)result : null);
            AddParser(typeof(bool), s => bool.TryParse(s, out var result) ? (object)result : null);
            AddParser(typeof(float), s => float.TryParse(s, out var result) ? (object)result : null);
            AddParser(typeof(string), s => s);
            AddParser(typeof(PlayerCharacterMasterController), s => GameUtils.GetPlayerWithName(s, false));
            AddParser(typeof(NetworkUser), s => GameUtils.GetNetworkUser(s, false));
            AddParser(typeof(BuffIndex), s => Enum.GetNames(typeof(BuffIndex)).Any(x => x.ContainsIgnoreCase(s)) ? Enum.GetNames(typeof(BuffIndex)).First(x => x.ContainsIgnoreCase(s)).ToEnum<BuffIndex>() : BuffIndex.None);
            AddParser(typeof(TeamIndex), s => Enum.GetNames(typeof(TeamIndex)).Any(x => x.ContainsIgnoreCase(s)) ? Enum.GetNames(typeof(TeamIndex)).First(x => x.ContainsIgnoreCase(s)).ToEnum<TeamIndex>() : TeamIndex.None);
            AddParser(typeof(ArtifactIndex), s => Enum.GetNames(typeof(ArtifactIndex)).Any(x => x.ContainsIgnoreCase(s)) ? Enum.GetNames(typeof(ArtifactIndex)).First(x => x.ContainsIgnoreCase(s)).ToEnum<ArtifactIndex>() : ArtifactIndex.None);
            AddParser(typeof(SurvivorIndex), s => Enum.GetNames(typeof(SurvivorIndex)).Any(x => x.ContainsIgnoreCase(s)) ? Enum.GetNames(typeof(SurvivorIndex)).First(x => x.ContainsIgnoreCase(s)).ToEnum<SurvivorIndex>() : SurvivorIndex.None);
            AddParser(typeof(EquipmentDef), s => {
                try {
                    var equipments = typeof(EquipmentCatalog).GetFieldValue<EquipmentDef[]>("equipmentDefs");
                    return Enum.TryParse(s, true, out EquipmentIndex i) ? equipments.First(x => x.equipmentIndex == i) : equipments.First(x => x.nameToken.FromLang().ContainsIgnoreCase(s));
                }
                catch {
                    return null;
                }
            });
            AddParser(typeof(SurvivorDef), s => {
                try
                {
                    return Enum.TryParse(s, true, out SurvivorIndex i) ? SurvivorCatalog.GetSurvivorDef(i) : SurvivorCatalog.allSurvivorDefs.First(x => x.displayNameToken.FromLang().ContainsIgnoreCase(s));
                }
                catch {
                    return null;
                }
            });
            AddParser(typeof(ItemDef), s => {
                try
                {
                    return Enum.TryParse(s, true, out ItemIndex i) ? ItemCatalog.GetItemDef(i) : ItemCatalog.GetItemDef(ItemCatalog.allItems.First(x => ItemCatalog.GetItemDef(x).nameToken.FromLang().ContainsIgnoreCase(s)));
                }
                catch {
                    return null;
                }
            });

            On.RoR2.Chat.CCSay += ChatOnCcSay;
            Register(new DefaultCommands());
        }

        /// <summary>
        /// Manages whether or not a player's message is a command or not, and if it is a command to prevent it from being sent to other players as a message
        /// </summary>
        private void ChatOnCcSay(On.RoR2.Chat.orig_CCSay orig, ConCommandArgs args) {
            // Check if the message starts with a / - This includes all players if the host has this mod - Kyle
            if (args[0].StartsWith("/")) {
                R2API.PermissionsAPI.AddDefault(args.sender);
                try {
                    Run(args[0].Substring(1), args.sender);
                }
                catch (Exception e) {
                    args.sender.SendDirectMessage(e.Message);
                }
            }
            else {
                args.CheckArgumentCount(1);
                if (args.sender) {
                    Chat.SendBroadcastChat(new Chat.UserChatMessage {
                        sender = args.sender.gameObject,
                        text = args[0]
                    });
                }
            }
        }

        /// <summary>
        ///     Gets a read-only view of the commands.
        /// </summary>
        public IEnumerable<Command> Commands => _commands.AsReadOnly();

        private static string GetNextArgument(string s, out int nextIndex, bool getRest = false)
        {
            var inQuotes = s[0] == '"';
            var i = inQuotes ? 1 : 0;
            if (getRest)
            {
                inQuotes = true;
                i = 0;
            }
            var result = new StringBuilder();
            for (; i < s.Length; ++i)
            {
                var c = s[i];
                if (c == '\\' && ++i < s.Length)
                {
                    var nextC = s[i];
                    if (nextC != '"' && nextC != ' ' && nextC != '\\')
                    {
                        result.Append(c);
                    }
                    result.Append(nextC);
                }
                else if (c == '"' && inQuotes || char.IsWhiteSpace(c) && !inQuotes)
                {
                    ++i;
                    break;
                }
                else
                {
                    result.Append(c);
                }
            }

            nextIndex = i;
            return result.ToString();
        }

        private static string PrettifyCamelCase(string camelCase) =>
            CamelCaseRegex.Replace(camelCase, " $1").ToLower().Trim();

        /// <summary>
        ///     Adds the specified parser.
        /// </summary>
        /// <param name="type">The type, which must not be <c>null</c>.</param>
        /// <param name="parser">The parser, which must not be <c>null</c>.</param>
        /// <exception cref="ArgumentNullException">
        ///     Either <paramref name="type" /> or <paramref name="parser" /> is <c>null</c>.
        /// </exception>
        public void AddParser(Type type, Parser parser)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            _parsers[type] = parser ?? throw new ArgumentNullException(nameof(parser));
        }

        /// <summary>
        ///     Unregisters the commands marked in the specified object.
        /// </summary>
        /// <param name="obj">The object, which must not be <c>null</c>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="obj" /> is <c>null</c>.</exception>
        public void Unregister(object obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            var commandAttributes = from m in obj.GetType().GetMethods()
                                    let a = m.GetCustomAttribute<CommandAttribute>()
                                    where a != null
                                    select a;
            foreach (var commandAttribute in commandAttributes)
            {
                _commands.RemoveAll(c => c.Name == commandAttribute.Name);
            }
        }

        /// <summary>
        ///     Registers the commands marked in the specified object.
        /// </summary>
        /// <param name="obj">The object, which must not be <c>null</c>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="obj" /> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">A command contains a type which cannot be parsed.</exception>
        public void Register(object obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            var type = obj.GetType();

            var defaultPerms = type.GetCustomAttributes();
            if (defaultPerms.Any(x => x.GetType() == typeof(DefaultPermissionAttribute)))
            {
                var def = (DefaultPermissionAttribute) defaultPerms.FirstOrDefault(x => x.GetType() == typeof(DefaultPermissionAttribute));
                foreach (var s in def.Permissions)
                {
                    R2API.PermissionsAPI.AddDefaultPermission(s);
                }
            }

            foreach (var method in type.GetMethods())
            {
                var commandAttribute = method.GetCustomAttribute<CommandAttribute>();
                if (commandAttribute == null)
                {
                    continue;
                }

                var syntax = commandAttribute.Syntax;
                var reducers = new List<CommandReducer>();
                foreach (var parameter in method.GetParameters())
                {
                    var parameterType = parameter.ParameterType;
                    if (parameterType.IsGenericType && parameterType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        parameterType = parameterType.GenericTypeArguments[0];
                    }
                    if (!_parsers.TryGetValue(parameterType, out var parser))
                    {
                        throw new InvalidOperationException($"Type '{parameterType.Name}' cannot be parsed on method {method.Name}.");
                    }

                    reducers.Add((s, p) =>
                    {
                        s = s.Trim();
                        if (string.IsNullOrWhiteSpace(s))
                        {
                            if (parameter.HasDefaultValue)
                            {
                                p.Add(parameter.DefaultValue);
                                return s;
                            }
                            throw new FormatException($"Syntax: {syntax}");
                        }

                        var argument = GetNextArgument(s, out var nextIndex, parameter.CustomAttributes.Any(x => x.AttributeType == typeof(RestAttribute)));
                        var result = parser(argument);
                        if (result == null)
                        {
                            var parameterName = PrettifyCamelCase(parameter.Name);
                            throw new FormatException($"Invalid {parameterName} '{argument}'.");
                        }

                        p.Add(result);

                        return s.Substring(nextIndex);
                    });
                }

                _commands.Add(new Command(commandAttribute, (s, u) =>
                {
                    if (!u.isServer && commandAttribute.Local)
                    {
                        throw new UnauthorizedAccessException("Unauthorized: Host only command");
                    }

                    var parameters = new List<object>();
                    // First param is always the player
                    s = $"\"{u.userName}\"{s}";
                    s = reducers.Aggregate(s, (s2, reducer) => reducer(s2, parameters));
                    // Ensure that all arguments were consumed.
                    if (!string.IsNullOrWhiteSpace(s))
                    {
                        throw new FormatException($"Syntax: {syntax}");
                    }

                    if (!string.IsNullOrWhiteSpace(commandAttribute.Permission))
                    {
                        if (!u.HasPermission(commandAttribute.Permission))
                        {
                            throw new UnauthorizedAccessException("Unauthorized: You do not have permission to use this command");
                        }
                    }

                    method.Invoke(obj, parameters.ToArray());
                }));
            }
        }

        /// <summary>
        ///     Runs the specified string as a command.
        /// </summary>
        /// <param name="s">The string, which must not be <c>null</c>.</param>
        /// <param name="user">The user who executed the command, which much not be <c>null</c>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="s" /> is <c>null</c>.</exception>
        /// <exception cref="FormatException"><paramref name="s" /> failed to be parsed properly.</exception>
        public void Run(string s, NetworkUser user)
        {
            if (s == null)
            {
                throw new ArgumentNullException(nameof(s));
            }

            var commandName = s.Split(' ')[0];
            var command = _commands.FirstOrDefault(
                c => c.Name.Same(commandName) || (c.Alias?.Any(x => x.Same(commandName)) ?? false));
            if (command == null)
            {
                throw new FormatException($"Invalid command, '{s}'");
            }

            command.Invoke(s.Substring(commandName.Length), user);
        }

        private delegate string CommandReducer(string s, List<object> parameters);
    }
}
