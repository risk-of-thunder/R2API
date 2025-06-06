using RoR2;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("R2API.CharacterBody")]

namespace R2API;

internal static class CharacterBodyInterop
{
    public static byte[] GetModdedBodyFlags(CharacterBody characterBody) => characterBody.r2api_moddedBodyFlags;
    public static void SetModdedBodyFlags(CharacterBody characterBody, byte[] value) => characterBody.r2api_moddedBodyFlags = value;
}
