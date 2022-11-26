using BepInEx;

namespace R2API;

[BepInPlugin(DeployableAPI.PluginGUID, DeployableAPI.PluginName, DeployableAPI.PluginVersion)]
public sealed class DeployablePlugin : BaseUnityPlugin
{
    private void OnDestroy()
    {
        DeployableAPI.UnsetHooks();
    }
}
