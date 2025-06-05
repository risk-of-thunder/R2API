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
        moddedBodyFlags.Offset = 16;
        characterBody?.Fields.Add(moddedBodyFlags);
        // TODO: Implement this later
        /*
        var primarySkillDamageAddition = AddFloatField("primarySkillDamageAddition");
        var primarySkillDamageMultiplier = AddFloatField("primarySkillDamageMultiplier");
        var secondarySkillDamageAddition = AddFloatField("secondarySkillDamageAddition");
        var secondarySkillDamageMultiplier = AddFloatField("secondarySkillDamageMultiplier");
        var utilitySkillDamageAddition = AddFloatField("utilitySkillDamageAddition");
        var utilitySkillDamageMultiplier = AddFloatField("utilitySkillDamageMultiplier");
        var specialSkillDamageAddition = AddFloatField("specialSkillDamageAddition");
        var specialSkillDamageMultiplier = AddFloatField("specialSkillDamageMultiplier");
        var equipmentDamageAddition = AddFloatField("equipmentDamageAddition");
        var equipmentDamageMultiplier = AddFloatField("equipmentDamageMultiplier");
        var dotDamageAddition = AddFloatField("dotDamageAddition");
        var dotDamageMultiplier = AddFloatField("dotDamageMultiplier");
        var hazardDamageAddition = AddFloatField("hazardDamageAddition");
        var hazardDamageMultiplier = AddFloatField("hazardDamageMultiplier");
        var primarySkillVulnerabilityAddition = AddFloatField("primarySkillVulnerabilityAddition");
        var primarySkillVulnerabilityMultiplier = AddFloatField("primarySkillVulnerabilityMultiplier");
        var secondarySkillVulnerabilityAddition = AddFloatField("secondarySkillVulnerabilityAddition");
        var secondarySkillVulnerabilityMultiplier = AddFloatField("secondarySkillVulnerabilityMultiplier");
        var utilitySkillVulnerabilityAddition = AddFloatField("utilitySkillVulnerabilityAddition");
        var utilitySkillVulnerabilityMultiplier = AddFloatField("utilitySkillVulnerabilityMultiplier");
        var specialSkillVulnerabilityAddition = AddFloatField("specialSkillVulnerabilityAddition");
        var specialSkillVulnerabilityMultiplier = AddFloatField("specialSkillVulnerabilityMultiplier");
        var equipmentVulnerabilityAddition = AddFloatField("equipmentVulnerabilityAddition");
        var equipmentVulnerabilityMultiplier = AddFloatField("equipmentVulnerabilityMultiplier");
        var dotVulnerabilityAddition = AddFloatField("dotVulnerabilityAddition");
        var dotVulnerabilityMultiplier = AddFloatField("dotVulnerabilityMultiplier");
        var hazardVulnerabilityAddition = AddFloatField("hazardVulnerabilityAddition");
        var hazardVulnerabilityMultiplier = AddFloatField("hazardVulnerabilityMultiplier");
        FieldDefinition AddFloatField(string name)
        {
            FieldDefinition fieldDefinition = new FieldDefinition("r2api_" + name, FieldAttributes.Public, assembly.MainModule.ImportReference(typeof(float)));
            characterBody?.Fields.Add(fieldDefinition);
            return fieldDefinition;
        }*/
    }
}
