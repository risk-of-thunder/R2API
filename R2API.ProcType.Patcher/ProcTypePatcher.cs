using BepInEx;
using BepInEx.Logging;
using Mono.Cecil;
using System.Collections.Generic;

namespace R2API;

public static class ProcTypePatcher
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
        TypeDefinition procChainMask = assembly.MainModule.GetType("RoR2", "ProcChainMask");
        procChainMask?.Fields.Add(new FieldDefinition("r2api_moddedMask", FieldAttributes.Public, assembly.MainModule.ImportReference(typeof(byte[]))));
    }
}
