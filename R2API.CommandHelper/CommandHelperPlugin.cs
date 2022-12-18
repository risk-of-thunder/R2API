using BepInEx;
using BepInEx.Logging;

namespace R2API.Utils;

[BepInPlugin(CommandHelper.PluginGUID, CommandHelper.PluginName, CommandHelper.PluginVersion)]
public sealed class CommandHelperPlugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger { get; set; }

    private void Awake()
    {
        Logger = base.Logger;
    }

    private void OnDestroy()
    {
        CommandHelper.UnsetHooks();
    }
}
