using System;
using System.Collections.Generic;
using Mono.Cecil;

namespace R2API;

internal static class SkinsPatcher
{
    public static IEnumerable<string> TargetDLLs
    {
        get
        {
            yield return "RoR2.dll";
        }
    }
    public const string skillVariants = "r2api_skillVariants";
    public const string skinSkillVariantsDef = "r2api_skinSkillVariantsDef";
    public static void Patch(AssemblyDefinition assembly)
    {
        TypeDefinition rendererInfo = assembly.MainModule.GetType("RoR2.CharacterModel/RendererInfo");
        rendererInfo?.Fields.Add(new FieldDefinition(skillVariants, FieldAttributes.Public, assembly.MainModule.ImportReference(typeof(object[]))));
        TypeDefinition meshReplacement = assembly.MainModule.GetType("RoR2.SkinDefParams/MeshReplacement");
        meshReplacement?.Fields.Add(new FieldDefinition(skillVariants, FieldAttributes.Public, assembly.MainModule.ImportReference(typeof(object[]))));
        TypeDefinition lightInfo = assembly.MainModule.GetType("RoR2.CharacterModel/LightInfo");
        lightInfo?.Fields.Add(new FieldDefinition(skillVariants, FieldAttributes.Public, assembly.MainModule.ImportReference(typeof(object[]))));
        TypeDefinition projectileGhostReplacement = assembly.MainModule.GetType("RoR2.SkinDefParams/ProjectileGhostReplacement");
        projectileGhostReplacement?.Fields.Add(new FieldDefinition(skillVariants, FieldAttributes.Public, assembly.MainModule.ImportReference(typeof(object[]))));
        TypeDefinition minionSkinReplacement = assembly.MainModule.GetType("RoR2.SkinDefParams/MinionSkinReplacement");
        minionSkinReplacement?.Fields.Add(new FieldDefinition(skillVariants, FieldAttributes.Public, assembly.MainModule.ImportReference(typeof(object[]))));
        TypeDefinition meshReplacementTemplate = assembly.MainModule.GetType("RoR2.SkinDef/MeshReplacementTemplate");
        meshReplacementTemplate?.Fields.Add(new FieldDefinition(skillVariants, FieldAttributes.Public, assembly.MainModule.ImportReference(typeof(object[]))));
        TypeDefinition lightReplacementTemplate = assembly.MainModule.GetType("RoR2.SkinDef/LightReplacementTemplate");
        lightReplacementTemplate?.Fields.Add(new FieldDefinition(skillVariants, FieldAttributes.Public, assembly.MainModule.ImportReference(typeof(object[]))));
        TypeDefinition ghostReplacementTemplate = assembly.MainModule.GetType("RoR2.SkinDef/GhostReplacementTemplate");
        ghostReplacementTemplate?.Fields.Add(new FieldDefinition(skillVariants, FieldAttributes.Public, assembly.MainModule.ImportReference(typeof(object[]))));
        TypeDefinition minionSkinTemplate = assembly.MainModule.GetType("RoR2.SkinDef/MinionSkinTemplate");
        minionSkinTemplate?.Fields.Add(new FieldDefinition(skillVariants, FieldAttributes.Public, assembly.MainModule.ImportReference(typeof(object[]))));
        TypeDefinition skinDef = assembly.MainModule.GetType("RoR2.SkinDef");
        if (skinDef != null)
        {
            skinDef.Fields.Add(new FieldDefinition("r2api_onSkinApplied", FieldAttributes.Public, assembly.MainModule.ImportReference(typeof(Delegate))));
            TypeDefinition runtimeSkin = assembly.MainModule.GetType("RoR2.SkinDef/RuntimeSkin");
            runtimeSkin?.Fields.Add(new FieldDefinition("r2api_skinDef", FieldAttributes.Public, assembly.MainModule.ImportReference(skinDef)));
        }
        TypeDefinition skinDefParams = assembly.MainModule.GetType("RoR2.SkinDefParams");
        skinDefParams?.Fields.Add(new FieldDefinition(skinSkillVariantsDef, FieldAttributes.Public, assembly.MainModule.ImportReference(typeof(object))));
        TypeDefinition modelSkinController = assembly.MainModule.GetType("RoR2.ModelSkinController");
        modelSkinController?.Fields.Add(new FieldDefinition(skinSkillVariantsDef, FieldAttributes.Public, assembly.MainModule.ImportReference(typeof(object))));
    }
}
