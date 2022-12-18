using BepInEx;
using BepInEx.Logging;

namespace R2API;

[BepInPlugin(ArtifactCodeAPI.PluginGUID, ArtifactCodeAPI.PluginName, ArtifactCodeAPI.PluginVersion)]
public sealed class ArtifactCodePlugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger { get; set; }

    private void Awake()
    {
        Logger = base.Logger;
    }

    private void OnDestroy()
    {
        ArtifactCodeAPI.UnsetHooks();
    }
}
