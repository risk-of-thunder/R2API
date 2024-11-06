using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;

namespace R2API;

[BepInPlugin(AnimationsAPI.PluginGUID, AnimationsAPI.PluginName, AnimationsAPI.PluginVersion)]
public sealed class AnimationsPlugin : BaseUnityPlugin
{
    internal static AnimationsPlugin Instance { get; private set; }
    internal static new ManualLogSource Logger { get; private set; }
    internal static ConfigEntry<bool> IgnoreCache { get; private set; }

    private void Awake()
    {
        Instance = this;
        Logger = base.Logger;
        IgnoreCache = Config.Bind("Dev", nameof(IgnoreCache), false, "Always generate new bundles with modifications");
    }

    private void OnDestroy()
    {
        AnimationsAPI.UnsetHooks();
    }
}
