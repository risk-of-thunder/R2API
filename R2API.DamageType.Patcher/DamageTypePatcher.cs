using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace R2API;

internal static class DamageTypePatcher
{
    public static IEnumerable<string> TargetDLLs
    {
        get
        {
            yield return "RoR2.dll";
        }
    }

    public static void Patch(AssemblyDefinition assembly)
    {
        PatchDamageTypeCombo(assembly);
        PatchCrocoDamageTypeController(assembly);
    }

    private static void PatchCrocoDamageTypeController(AssemblyDefinition assembly)
    {
        var crocoDamageTypeController = assembly.MainModule.GetType("RoR2", "CrocoDamageTypeController");
        var newField = new FieldDefinition("r2api_moddedDamageTypes", FieldAttributes.Public, assembly.MainModule.ImportReference(typeof(byte[])));
        crocoDamageTypeController?.Fields.Add(newField);
    }

    private static void PatchDamageTypeCombo(AssemblyDefinition assembly)
    {
        var damageTypeCombo = assembly.MainModule.GetType("RoR2", "DamageTypeCombo");
        var newField = new FieldDefinition("r2api_moddedDamageTypes", FieldAttributes.Public, assembly.MainModule.ImportReference(typeof(byte[])));
        newField.Offset = 16;
        damageTypeCombo?.Fields.Add(newField);
    }
}
