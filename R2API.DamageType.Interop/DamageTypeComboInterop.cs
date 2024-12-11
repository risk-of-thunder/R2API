using RoR2;
using System.Runtime.CompilerServices;

[assembly:InternalsVisibleTo("R2API.DamageType")]

namespace R2API;

internal static class DamageTypeComboInterop
{
    public static byte[] GetModdedDamageTypes(DamageTypeCombo damageType) => damageType.r2api_moddedDamageTypes;

    public static void SetModdedDamageTypes(ref DamageTypeCombo damageType, byte[] value) => damageType.r2api_moddedDamageTypes = value;
}
