using BepInEx;

namespace R2API;

[BepInPlugin(ArtifactCodeAPI.PluginGUID, ArtifactCodeAPI.PluginName, ArtifactCodeAPI.PluginVersion)]
public sealed class ArtifactCodePlugin : BaseUnityPlugin
{
    private void OnDestroy()
    {
        ArtifactCodeAPI.UnsetHooks();
    }
}
