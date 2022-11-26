using BepInEx;
using R2API.Utils;

namespace R2API.Networking;

[BepInPlugin(NetworkingAPI.PluginGUID, NetworkingAPI.PluginName, NetworkingAPI.PluginVersion)]
public sealed class NetworkingPlugin : BaseUnityPlugin
{
    private NetworkCompatibilityHandler _networkCompatibilityHandler;

    private void Awake()
    {
        _networkCompatibilityHandler = new NetworkCompatibilityHandler();
        _networkCompatibilityHandler.BuildModList();
    }

    private void OnDestroy()
    {
        _networkCompatibilityHandler.CleanupModList();

        NetworkingAPI.UnsetHooks();
    }
}
