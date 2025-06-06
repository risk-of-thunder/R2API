using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Diagnostics;

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
        characterBody?.Fields.Add(moddedBodyFlags);
    }
}
