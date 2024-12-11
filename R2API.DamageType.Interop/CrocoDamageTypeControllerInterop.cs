using System;
using System.Collections.Generic;
using System.Text;
using RoR2;

namespace R2API;
internal class CrocoDamageTypeControllerInterop
{
    public static byte[] GetModdedDamageTypes(CrocoDamageTypeController damageTypeController) => damageTypeController.r2api_moddedDamageTypes;

    public static void SetModdedDamageTypes(CrocoDamageTypeController damageTypeController, byte[] value) => damageTypeController.r2api_moddedDamageTypes = value;
}
