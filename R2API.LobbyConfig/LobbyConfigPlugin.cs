using BepInEx;

namespace R2API;

[BepInPlugin(LobbyConfigAPI.PluginGUID, LobbyConfigAPI.PluginName, LobbyConfigAPI.PluginVersion)]
public sealed class LobbyConfigPlugin : BaseUnityPlugin
{
    private void OnDestroy()
    {
        LobbyConfigAPI.UnsetHooks();
    }
}
