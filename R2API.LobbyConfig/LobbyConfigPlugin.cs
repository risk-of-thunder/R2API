using BepInEx;

namespace R2API;

[BepInPlugin(LobbyConfigAPI.PluginGUID, LobbyConfigAPI.PluginName, LobbyConfigAPI.PluginVersion)]
public sealed class DepplpoyablePlugin : BaseUnityPlugin
{
    private void OnEnable()
    {
        LobbyConfigAPI.SetHooks();
    }

    private void OnDisable()
    {
        LobbyConfigAPI.UnsetHooks();
    }
}
