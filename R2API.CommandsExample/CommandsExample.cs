using BepInEx;

namespace R2API.CommandsExample {
    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin("r2api.commands.example", "Commands Example", "1.0")]
    public class CommandsExample : BaseUnityPlugin
    {
        public CommandsExample()
        {
            // Register your commands, either in the constructor of your plugin or in awake
            R2API.CommandAPI.Register(new Commands());
        }
    }
}
