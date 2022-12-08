using BepInEx;
using BepInEx.Logging;

namespace R2API;

[BepInPlugin(DotAPI.PluginGUID, DotAPI.PluginName, DotAPI.PluginVersion)]
public sealed class DotPlugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger { get; set; }

    private void Awake()
    {
        Logger = base.Logger;
    }

    private void OnDestroy()
    {
        DotAPI.UnsetHooks();
    }
}
