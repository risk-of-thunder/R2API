using System;
using System.Collections.Generic;
using Mono.Cecil;

namespace R2API;

internal static class SkillsPatcher
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
        TypeDefinition genericSkill = assembly.MainModule.GetType("RoR2", "GenericSkill");
        if (genericSkill != null)
        {
            genericSkill.Fields.Add(new FieldDefinition("r2api_hideInLoadout", FieldAttributes.Public, assembly.MainModule.ImportReference(typeof(bool))));
            genericSkill.Fields.Add(new FieldDefinition("r2api_hideInCharacterSelectIfFirstSkillSelected", FieldAttributes.Public, assembly.MainModule.ImportReference(typeof(bool))));
            genericSkill.Fields.Add(new FieldDefinition("r2api_orderPriority", FieldAttributes.Public, assembly.MainModule.ImportReference(typeof(int))));
            genericSkill.Fields.Add(new FieldDefinition("r2api_loadoutTitleTokenOverride", FieldAttributes.Public, assembly.MainModule.ImportReference(typeof(string))));
        }
        TypeDefinition skillDef = assembly.MainModule.GetType("RoR2.Skills", "SkillDef");
        if (skillDef != null)
        {
            skillDef.Fields.Add(new FieldDefinition("r2api_bonusStockMultiplier", FieldAttributes.Public, assembly.MainModule.ImportReference(typeof(int))));
        }
    }
}
