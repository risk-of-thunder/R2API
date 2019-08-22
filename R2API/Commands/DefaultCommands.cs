using System.Linq;
using R2API.Utils;
using RoR2;

namespace R2API.Commands
{
    [DefaultPermission("r2.api.default")]
    public class DefaultCommands
    {
        [Command("help", "/help [command]", Alias = new[] {"h"}, HelpText = "List all commands or show the help text for the specified command")]
        public void HelpCommand(NetworkUser user, string commandName = null)
        {
            var commands = R2API.CommandAPI.Commands;
            if (commandName == null)
            {
                user.SendDirectMessage("Available commands:");
                user.SendDirectMessage(string.Join(", ", commands.Where(x => user.HasPermission(x.Permission)).Select(c => c.Name)));
                return;
            }

            var command = commands.FirstOrDefault(
                c => c.Name.Same(commandName) ||
                     (c.Alias?.Any(x => x.Same(commandName)) ?? false));
            if (command == null)
            {
                user.SendDirectMessage($"Invalid command '{commandName}'.");
                return;
            }

            user.SendDirectMessage($"{command.Name} help:");
            if (command.Alias != null)
            {
                user.SendDirectMessage($"Alias: {command.Alias}");
            }
            user.SendDirectMessage($"Syntax: {command.Syntax}");
            user.SendDirectMessage(command.HelpText ?? "No help text available");
        }
    }
}
