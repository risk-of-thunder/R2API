using BepInEx;

namespace R2API.Utils;

[BepInPlugin(CommandHelper.PluginGUID, CommandHelper.PluginName, CommandHelper.PluginVersion)]
public sealed class CommandHelperPlugin : BaseUnityPlugin {
    private void OnEnable() {
        CommandHelper.SetHooks();
    }

    private void OnDisable() {
        CommandHelper.UnsetHooks();
    }
}
