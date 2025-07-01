using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection;

namespace R2API;

internal static class CharacterBodyPatcher
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
        PatchCharacterBody(assembly);
    }
    private static void PatchCharacterBody(AssemblyDefinition assembly)
    {
        var characterBody = assembly.MainModule.GetType("RoR2", "CharacterBody");
        var moddedBodyFlags = new FieldDefinition("r2api_moddedBodyFlags", FieldAttributes.Public, assembly.MainModule.ImportReference(typeof(byte[])));
        var damageSourceDamageMultiplier = new FieldDefinition("r2api_damageSourceDamageMultiplier", FieldAttributes.Public, assembly.MainModule.ImportReference(typeof(Dictionary<string, float>)));
        var damageSourceDamageAddition = new FieldDefinition("r2api_damageSourceDamageAddition", FieldAttributes.Public, assembly.MainModule.ImportReference(typeof(Dictionary<string, float>)));
        var damageSourceVulnerabilityMultiplier = new FieldDefinition("r2api_damageSourceVulnerabilityMultiplier", FieldAttributes.Public, assembly.MainModule.ImportReference(typeof(Dictionary<string, float>)));
        var damageSourceVulnerabilityAddition = new FieldDefinition("r2api_damageSourceVulnerabilityAddition", FieldAttributes.Public, assembly.MainModule.ImportReference(typeof(Dictionary<string, float>)));
        if (characterBody != null)
        {
            characterBody.Fields.Add(moddedBodyFlags);
            characterBody.Fields.Add(damageSourceDamageMultiplier);
            characterBody.Fields.Add(damageSourceDamageAddition);
            characterBody.Fields.Add(damageSourceVulnerabilityMultiplier);
            characterBody.Fields.Add(damageSourceVulnerabilityAddition);
        }
    }
}
