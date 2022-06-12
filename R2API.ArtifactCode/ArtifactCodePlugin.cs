using BepInEx;

namespace R2API;

[BepInPlugin(ArtifactCodeAPI.PluginGUID, ArtifactCodeAPI.PluginName, ArtifactCodeAPI.PluginVersion)]
public sealed class ArtifactCodePlugin : BaseUnityPlugin
{
    private void OnEnable()
    {
        ArtifactCodeAPI.SetHooks();
    }

    private void OnDisable()
    {
        ArtifactCodeAPI.UnsetHooks();
    }
}
