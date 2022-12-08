using BepInEx;
using BepInEx.Logging;

namespace R2API;

[BepInPlugin(DamageAPI.PluginGUID, DamageAPI.PluginName, DamageAPI.PluginVersion)]
public sealed class DamageTypePlugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger { get; set; }

    private void Awake()
    {
        Logger = base.Logger;
    }

    private void OnDestroy()
    {
        DamageAPI.UnsetHooks();
    }
}
