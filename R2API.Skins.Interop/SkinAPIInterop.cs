using System;
using System.Runtime.CompilerServices;
using RoR2;

[assembly: InternalsVisibleTo("R2API.Skins")]
namespace R2API.SkinsAPI.Interop;

public static class SkinAPIInterop
{
    public static ref object[] GetSkillVariants(ref CharacterModel.RendererInfo rendererInfo) => ref rendererInfo.r2api_skillVariants;
    public static void SetSkillVariants(ref CharacterModel.RendererInfo rendererInfo, object[] value) => rendererInfo.r2api_skillVariants = value;
    public static ref object[] GetSkillVariants(ref SkinDefParams.MeshReplacement meshReplacement) => ref meshReplacement.r2api_skillVariants;
    public static void SetSkillVariants(ref SkinDefParams.MeshReplacement meshReplacement, object[] value) => meshReplacement.r2api_skillVariants = value;
    public static ref object[] GetSkillVariants(ref CharacterModel.LightInfo lightInfo) => ref lightInfo.r2api_skillVariants;
    public static void SetSkillVariants(ref CharacterModel.LightInfo lightInfo, object[] value) => lightInfo.r2api_skillVariants = value;
    public static ref object[] GetSkillVariants(ref SkinDefParams.ProjectileGhostReplacement projectileGhostReplacement) => ref projectileGhostReplacement.r2api_skillVariants;
    public static void SetSkillVariants(ref SkinDefParams.ProjectileGhostReplacement projectileGhostReplacement, object[] value) => projectileGhostReplacement.r2api_skillVariants = value;
    public static ref object[] GetSkillVariants(ref SkinDefParams.MinionSkinReplacement minionSkinReplacement) => ref minionSkinReplacement.r2api_skillVariants;
    public static void SetSkillVariants(ref SkinDefParams.MinionSkinReplacement minionSkinReplacement, object[] value) => minionSkinReplacement.r2api_skillVariants = value;
    public static ref object[] GetSkillVariants(ref SkinDef.MeshReplacementTemplate meshReplacementTemplate) => ref meshReplacementTemplate.r2api_skillVariants;
    public static void SetSkillVariants(ref SkinDef.MeshReplacementTemplate meshReplacement, object[] value) => meshReplacement.r2api_skillVariants = value;
    public static ref object[] GetSkillVariants(ref SkinDef.LightReplacementTemplate lightReplacementTemplate) => ref lightReplacementTemplate.r2api_skillVariants;
    public static void SetSkillVariants(ref SkinDef.LightReplacementTemplate lightReplacementTemplate, object[] value) => lightReplacementTemplate.r2api_skillVariants = value;
    public static ref object[] GetSkillVariants(ref SkinDef.GhostReplacementTemplate ghostReplacementTemplate) => ref ghostReplacementTemplate.r2api_skillVariants;
    public static void SetSkillVariants(ref SkinDef.GhostReplacementTemplate ghostReplacementTemplate, object[] value) => ghostReplacementTemplate.r2api_skillVariants = value;
    public static ref object[] GetSkillVariants(ref SkinDef.MinionSkinTemplate minionSkinTemplate) => ref minionSkinTemplate.r2api_skillVariants;
    public static void SetSkillVariants(ref SkinDef.MinionSkinTemplate minionSkinTemplate, object[] value) => minionSkinTemplate.r2api_skillVariants = value;
    public static SkinDef GetSkinDef(SkinDef.RuntimeSkin runtimeSkin) => runtimeSkin.r2api_skinDef;
    public static void SetSkinDef(SkinDef.RuntimeSkin runtimeSkin, SkinDef value) => runtimeSkin.r2api_skinDef = value;
    public static Delegate GetOnSkinApplied(SkinDef skinDef) => skinDef.r2api_onSkinApplied;
    public static void SetOnSkinApplied(SkinDef skinDef, Delegate value) => skinDef.r2api_onSkinApplied = value;
    public static object GetSkinSkillVariantsDef(SkinDefParams skinDefParams) => skinDefParams.r2api_skinSkillVariantsDef;
    public static void SetSkinSkillVariantsDef(SkinDefParams skinDefParams, object value) => skinDefParams.r2api_skinSkillVariantsDef = value;
    public static object GetSkinSkillVariantsDef(ModelSkinController modelSkinController) => modelSkinController.r2api_skinSkillVariantsDef;
    public static void SetSkinSkillVariantsDef(ModelSkinController modelSkinController, object value) => modelSkinController.r2api_skinSkillVariantsDef = value;
}
