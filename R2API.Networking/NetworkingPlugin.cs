using BepInEx;
using BepInEx.Logging;
using R2API.Utils;
using TF = System.Runtime.CompilerServices.TypeForwardedToAttribute;

[assembly: TF(typeof(CompatibilityLevel))]
[assembly: TF(typeof(VersionStrictness))]
[assembly: TF(typeof(NetworkCompatibility))]

namespace R2API.Networking;

[BepInPlugin(NetworkingAPI.PluginGUID, NetworkingAPI.PluginName, NetworkingAPI.PluginVersion)]
public sealed class NetworkingPlugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger { get; set; }

    private void Awake()
    {
        Logger = base.Logger;
        NetworkingAPI.SetHooks();
    }

    private void OnDestroy()
    {
        NetworkingAPI.UnsetHooks();
    }
}
