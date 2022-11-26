using BepInEx;

namespace R2API.Utils;

[BepInPlugin(CommandHelper.PluginGUID, CommandHelper.PluginName, CommandHelper.PluginVersion)]
public sealed class CommandHelperPlugin : BaseUnityPlugin
{
    private void OnDestroy()
    {
        CommandHelper.UnsetHooks();
    }
}
