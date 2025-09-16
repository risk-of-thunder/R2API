using Mono.Cecil;
using System.Collections.Generic;

namespace R2API;

internal static class TeamsPatcher
{
    public static IEnumerable<string> TargetDLLs { get; } = ["RoR2.dll"];

    public static void Patch(AssemblyDefinition assembly)
    {
        TypeDefinition procChainMask = assembly.MainModule.GetType("RoR2", "TeamMask");
        procChainMask?.Fields.Add(new FieldDefinition("r2api_moddedMask", FieldAttributes.Public, assembly.MainModule.ImportReference(typeof(byte[]))));
    }
}
