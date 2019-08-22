using R2API.Commands;
using R2API.Utils;
using RoR2;

namespace R2API.CommandsExample
{
    // Here you can set the default permission/s for your commands. If the permission is used in another plugin then it does not matter that you put it here too, it'll only add it once.
    [DefaultPermission("r2api.default")]
    public class Commands
    {
        // The command attribute is to declare a method as a command, and it's parameters as the arguments for said command. 
        // !!! NOTE: Every command should ALWAYS have it's first parameter as NetworkUser, otherwise you will not know who used the command. !!!
        // Name must be the command itself excluding the forward slash, case does not matter.
        // Syntax is what is sent to the player in the event that they give the wrong arguments.
        // Permission is the permission for the command, if it is empty, by default everyone can use it.
        // Alias is the other command names that will execute this method
        // Help Text is the text displayed by the default help command
        // The method name does not need to mean anything, though it is useful for keeping neat code.
        [Command("example", "/example string int dagger", Alias = new[] {"r2"}, HelpText = "This is an example command")]
        public void ExampleCommand(NetworkUser user, string string1, int int1, [Rest] ItemDef item = null)
        {
            GameUtils.SendChat($"Example Command: string1 = {string1}");
            GameUtils.SendChat($"Example Command: int1 = {int1}");
            if (item != null)
            {
                // Send a message to one player via this NetworkUser Extension method
                user.SendDirectMessage($"Example Command: item = {item.nameToken.FromLang()}");
            }
        }
    }
}
