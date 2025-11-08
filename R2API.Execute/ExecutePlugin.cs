using BepInEx;
using BepInEx.Logging;

namespace R2API;

[BepInPlugin(ExecuteAPI.PluginGUID, ExecuteAPI.PluginName, ExecuteAPI.PluginVersion)]
public sealed class ExecutePlugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger { get; set; }

    private void Awake()
    {
        Logger = base.Logger;
        ExecuteAPI.SetHooks();
    }

    private void OnDestroy()
    {
        ExecuteAPI.UnsetHooks();
    }
}
