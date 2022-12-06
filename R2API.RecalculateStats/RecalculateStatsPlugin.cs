using BepInEx;

namespace R2API;

[BepInPlugin(RecalculateStatsAPI.PluginGUID, RecalculateStatsAPI.PluginName, RecalculateStatsAPI.PluginVersion)]
public sealed class RecalculateStatsPlugin : BaseUnityPlugin
{
    private void OnDestroy()
    {
        RecalculateStatsAPI.UnsetHooks();
    }
}
