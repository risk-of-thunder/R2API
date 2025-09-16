using RoR2;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("R2API.Teams")]

namespace R2API;

internal static class TeamsInterop
{
    public static byte[] GetModdedMask(in TeamMask teamMask) => teamMask.r2api_moddedMask;

    public static ref byte[] GetModdedMaskRef(ref TeamMask teamMask) => ref teamMask.r2api_moddedMask;

    public static void SetModdedMask(ref TeamMask teamMask, byte[] value) => teamMask.r2api_moddedMask = value;
}
