using BepInEx;
using BepInEx.Logging;

namespace R2API;

[BepInPlugin(RuleCatalogExtras.PluginGUID, RuleCatalogExtras.PluginName, RuleCatalogExtras.PluginVersion)]
public sealed class RulesPlugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger { get; set; }

    private void Awake()
    {
        Logger = base.Logger;
    }

    private void OnEnable()
    {
        RuleCatalogExtras.SetHooks();
    }

    private void OnDisable()
    {
        RuleCatalogExtras.UnsetHooks();
    }
}
