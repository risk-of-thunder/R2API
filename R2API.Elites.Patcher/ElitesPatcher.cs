using Mono.Cecil;
using System.Collections.Generic;
using System.Linq;

namespace R2API;

internal static class ElitesPatcher
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
        var combatDirector = assembly.MainModule.GetType("RoR2", "CombatDirector");
        var eliteTierDef = combatDirector?.NestedTypes.FirstOrDefault(t => t.Name is "EliteTierDef");
        eliteTierDef?.Fields.Add(new FieldDefinition("r2api_name", FieldAttributes.Public, assembly.MainModule.ImportReference(typeof(string))));
    }
}
