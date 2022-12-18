using BepInEx;
using BepInEx.Logging;

namespace R2API;

[BepInPlugin(LobbyConfigAPI.PluginGUID, LobbyConfigAPI.PluginName, LobbyConfigAPI.PluginVersion)]
public sealed class LobbyConfigPlugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger { get; set; }

    private void Awake()
    {
        Logger = base.Logger;
    }

    private void OnDestroy()
    {
        LobbyConfigAPI.UnsetHooks();
    }
}
