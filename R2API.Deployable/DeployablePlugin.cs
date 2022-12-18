using BepInEx;
using BepInEx.Logging;

namespace R2API;

[BepInPlugin(DeployableAPI.PluginGUID, DeployableAPI.PluginName, DeployableAPI.PluginVersion)]
public sealed class DeployablePlugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger { get; set; }

    private void Awake()
    {
        Logger = base.Logger;
    }

    private void OnDestroy()
    {
        DeployableAPI.UnsetHooks();
    }
}
