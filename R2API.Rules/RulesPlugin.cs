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

        // TODO: Disabling this module currently doesnt do much, as we don't rely on hooks for the custom categories and rules to be active.
        // Ideally, we would have code that remove all custom rules and categories when it get disabled
        // Note: This is very low priority.
    }
}
