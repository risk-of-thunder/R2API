using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

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
        PatchSprintIcon(assembly);
    }
    private static void PatchSprintIcon(AssemblyDefinition assembly)
    {
        var sprintIcon = assembly.MainModule.GetType("RoR2.UI.SprintIcon");
        if (sprintIcon != null)
        {
            sprintIcon.Fields.Add(new FieldDefinition("r2api_customIconObject", FieldAttributes.Public, assembly.MainModule.ImportReference(typeof(GameObject))));
            sprintIcon.Fields.Add(new FieldDefinition("r2api_currentCustomSprintIcon", FieldAttributes.Public, assembly.MainModule.ImportReference(typeof(Sprite))));
        }
    }
    private static void PatchCharacterBody(AssemblyDefinition assembly)
    {
        var characterBody = assembly.MainModule.GetType("RoR2", "CharacterBody");
        if (characterBody != null)
        {
            characterBody.Fields.Add(new FieldDefinition("r2api_moddedBodyFlags", FieldAttributes.Public, assembly.MainModule.ImportReference(typeof(byte[]))));
            characterBody.Fields.Add(new FieldDefinition("r2api_customSprintIcon", FieldAttributes.Public, assembly.MainModule.ImportReference(typeof(Sprite))));
        }
    }
}
