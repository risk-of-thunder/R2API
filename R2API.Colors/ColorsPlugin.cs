using BepInEx;
using BepInEx.Logging;

namespace R2API;

[BepInPlugin(ColorsAPI.PluginGUID, ColorsAPI.PluginName, ColorsAPI.PluginVersion)]
public sealed class ColorsPlugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger { get; set; }

    private void Awake()
    {
        Logger = base.Logger;
    }

    private void OnDestroy()
    {
        ColorsAPI.UnsetHooks();
    }
}
