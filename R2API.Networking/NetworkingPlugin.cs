using BepInEx;
using R2API.Utils;

namespace R2API.Networking;

[BepInPlugin(NetworkingAPI.PluginGUID, NetworkingAPI.PluginName, NetworkingAPI.PluginVersion)]
public sealed class NetworkingPlugin : BaseUnityPlugin
{
    private void Awake()
    {
        var networkCompatibilityHandler = new NetworkCompatibilityHandler();
        networkCompatibilityHandler.BuildModList();
    }

    private void OnEnable()
    {
        NetworkingAPI.SetHooks();
    }

    private void OnDisable()
    {
        NetworkingAPI.UnsetHooks();
    }
}
