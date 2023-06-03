using BepInEx;
using BepInEx.Logging;
using R2API.ContentManagement;

namespace R2API;

[BepInDependency(R2APIContentManager.PluginGUID)]
[BepInPlugin(SoundAPI.PluginGUID, SoundAPI.PluginName, SoundAPI.PluginVersion)]
public sealed class SoundPlugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger { get; set; }

    private void Awake()
    {
        Logger = base.Logger;
    }

    private void OnEnable()
    {
        SoundAPI.SetHooks();
    }

    private void OnDisable()
    {
        SoundAPI.UnsetHooks();
    }
}
