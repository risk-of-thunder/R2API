using RoR2;
using System.Runtime.CompilerServices;

[assembly:InternalsVisibleTo("R2API.ProcType")]

namespace R2API;

internal static class ProcTypeInterop
{
    public static byte[] GetModdedMask(ProcChainMask procChainMask) => procChainMask.r2api_moddedMask;

    public static void SetModdedMask(ref ProcChainMask procChainMask, byte[] value) => procChainMask.r2api_moddedMask = value;
}
