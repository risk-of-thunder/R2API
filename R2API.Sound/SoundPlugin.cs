using BepInEx;

namespace R2API;

[BepInPlugin(SoundAPI.PluginGUID, SoundAPI.PluginName, SoundAPI.PluginVersion)]
public sealed class SoundPlugin : BaseUnityPlugin
{
    private void OnEnable()
    {
        SoundAPI.SetHooks();
    }

    private void OnDisable()
    {
        SoundAPI.UnsetHooks();
    }
}
