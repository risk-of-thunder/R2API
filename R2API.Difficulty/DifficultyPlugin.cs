using BepInEx;
using BepInEx.Logging;

namespace R2API;

[BepInPlugin(DifficultyAPI.PluginGUID, DifficultyAPI.PluginName, DifficultyAPI.PluginVersion)]
public sealed class DifficultyPlugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger { get; set; }

    private void Awake()
    {
        Logger = base.Logger;
    }

    private void OnEnable()
    {
        DifficultyAPI.SetHooks();
    }

    private void OnDisable()
    {
        DifficultyAPI.UnsetHooks();
    }
}
