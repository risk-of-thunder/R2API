using BepInEx;
using BepInEx.Logging;

namespace R2API;

[BepInPlugin(StagesAPI.PluginGUID, StagesAPI.PluginName, StagesAPI.PluginVersion)]
public sealed class StagesPlugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger { get; set; }

    private void Awake()
    {
        Logger = base.Logger;

        StageRegistration.SetHooks();
    }

    private void OnDestroy()
    {
        StageRegistration.UnsetHooks();
    }
}
