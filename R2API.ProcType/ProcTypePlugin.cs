using BepInEx;
using BepInEx.Logging;

[assembly: HG.Reflection.SearchableAttribute.OptIn]

namespace R2API;

[BepInPlugin(ProcTypeAPI.PluginGUID, ProcTypeAPI.PluginName, ProcTypeAPI.PluginVersion)]
public sealed class ProcTypePlugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger { get; set; }

    private void Awake()
    {
        Logger = base.Logger;
        ModdedProcType a = ProcTypeAPI.ReserveProcType();
        ModdedProcType b = ProcTypeAPI.ReserveProcType();
        ModdedProcType c = ProcTypeAPI.ReserveProcType();
        ProcTypeAPI.ReserveProcType();
        ProcTypeAPI.ReserveProcType();
        ProcTypeAPI.ReserveProcType();
        RoR2.ProcChainMask procChainMask = default;
        Logger.LogInfo(procChainMask);
        procChainMask.AddProc(RoR2.ProcType.HealNova);
        procChainMask.AddProc(RoR2.ProcType.Backstab);
        Logger.LogInfo(procChainMask);
        procChainMask.RemoveModdedProc(a);
        procChainMask.AddModdedProc(a);
        Logger.LogInfo(procChainMask);
        procChainMask.AddModdedProc(c);
        Logger.LogInfo(procChainMask);
        procChainMask.AddModdedProc(b);
        Logger.LogInfo(procChainMask);
        procChainMask.RemoveModdedProc(b);
        Logger.LogInfo(procChainMask);
        Logger.LogInfo(procChainMask.HasModdedProc(a));
        Logger.LogInfo(procChainMask.HasModdedProc(b));
        Logger.LogInfo(procChainMask.HasModdedProc(c));
        System.Collections.BitArray moddedMask = ProcTypeAPI.GetModdedMask(procChainMask);
        Logger.LogInfo(moddedMask.Length);
        moddedMask[5] = true;
        ProcTypeAPI.SetModdedMask(ref procChainMask, moddedMask);
        Logger.LogInfo(procChainMask);
    }

    private void OnDestroy()
    {
        ProcTypeAPI.UnsetHooks();
    }
}
