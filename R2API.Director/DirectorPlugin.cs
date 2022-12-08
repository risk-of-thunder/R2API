using BepInEx;
using BepInEx.Logging;

namespace R2API;

[BepInPlugin(DirectorAPI.PluginGUID, DirectorAPI.PluginName, DirectorAPI.PluginVersion)]
public sealed class DirectorPlugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger { get; set; }

    private void Awake()
    {
        Logger = base.Logger;

        DirectorAPI.SetHooks();
    }

    private void OnEnable()
    {
        DirectorAPI.SetHooks();
    }

    private void OnDisable()
    {
        DirectorAPI.UnsetHooks();
    }
}
