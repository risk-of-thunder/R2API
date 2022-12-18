using BepInEx;
using BepInEx.Logging;
using R2API.Utils;

namespace R2API.Networking;

[BepInPlugin(NetworkingAPI.PluginGUID, NetworkingAPI.PluginName, NetworkingAPI.PluginVersion)]
public sealed class NetworkingPlugin : BaseUnityPlugin
{
    private NetworkCompatibilityHandler _networkCompatibilityHandler;

    internal static new ManualLogSource Logger { get; set; }

    private void Awake()
    {
        Logger = base.Logger;

        _networkCompatibilityHandler = new NetworkCompatibilityHandler();
        _networkCompatibilityHandler.BuildModList();
    }

    private void OnDestroy()
    {
        _networkCompatibilityHandler.CleanupModList();

        NetworkingAPI.UnsetHooks();
    }
}
