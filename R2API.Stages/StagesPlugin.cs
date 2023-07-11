using BepInEx;
using BepInEx.Logging;

namespace R2API;

[BepInPlugin(StageRegistration.PluginGUID, StageRegistration.PluginName, StageRegistration.PluginVersion)]
public sealed class StagesPlugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger { get; set; }

    private void Awake()
    {
        Logger = base.Logger;
        new Log(Logger);

        StageRegistration.SetHooks();
    }

    private void OnDestroy()
    {
        StageRegistration.UnsetHooks();
    }
}
