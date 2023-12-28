using RoR2;

namespace R2API;

public static class ProcTypeInterop
{
    public static bool[] GetModdedMask(ProcChainMask procChainMask) => procChainMask.r2api_moddedMask;

    public static void SetModdedMask(ref ProcChainMask procChainMask, bool[] value) => procChainMask.r2api_moddedMask = value;
}
