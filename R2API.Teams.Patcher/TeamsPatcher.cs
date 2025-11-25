using Mono.Cecil;
using System.Collections.Generic;

namespace R2API;

internal static class TeamsPatcher
{
    public static IEnumerable<string> TargetDLLs { get; } = ["RoR2.dll"];

    public static void Patch(AssemblyDefinition assembly)
    {
        TypeDefinition teamMask = assembly.MainModule.GetType("RoR2", "TeamMask");
        teamMask?.Fields.Add(new FieldDefinition("r2api_moddedMask", FieldAttributes.Public, assembly.MainModule.ImportReference(typeof(byte[]))));
    }
}
