using BepInEx;

namespace R2API;

[BepInPlugin(TeamsAPI.PluginGUID, TeamsAPI.PluginName, TeamsAPI.PluginVersion)]
public sealed class TeamsPlugin : BaseUnityPlugin
{
    void Awake()
    {
        Log.Init(Logger);

        TeamsAPI.Init();

#if DEBUG
        // Enable hooks immediately for debugging
        TeamsAPI.SetHooks();
#endif
    }

    void OnDestroy()
    {
        TeamsAPI.UnsetHooks();
    }
}
