using HarmonyLib;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API.SkinsAPI.Interop;
using R2API.Utils;
using RoR2;
using RoR2.Projectile;
using RoR2.Skills;
using RoR2.SurvivorMannequins;
using RoR2BepInExPack.GameAssetPaths;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using static R2API.SkinSkillVariants;
using static UnityEngine.GridBrushBase;

namespace R2API;
public static partial class SkinSkillVariants
{
    public static Dictionary<SkinDef, BodyIndex> skinToBody = new Dictionary<SkinDef, BodyIndex>();
    #region Hooks
    private static bool _hooksSet;
    private static bool _isLoaded;
    internal static void SetHooks()
    {
        if (_hooksSet)
            return;

        _hooksSet = true;
        
        SkinsPlugin.harmonyPatcher.CreateClassProcessor(typeof(Patches)).Patch();
        On.RoR2.SkinDef.MeshReplacementTemplate.ctor += MeshReplacementTemplate_ctor;
        On.RoR2.SkinDef.GhostReplacementTemplate.ctor += GhostReplacementTemplate_ctor;
        On.RoR2.SkinDef.LightReplacementTemplate.ctor += LightReplacementTemplate_ctor;
        On.RoR2.SkinDef.MinionSkinTemplate.ctor += MinionSkinTemplate_ctor;
        IL.RoR2.ProjectileGhostReplacementManager.FindProjectileGhostPrefab += ProjectileGhostReplacementManager_FindProjectileGhostPrefab;
        IL.RoR2.MasterSummon.Perform += MasterSummon_Perform;
    }
    internal static void UnsetHooks()
    {
        _hooksSet = false;

        On.RoR2.SkinDef.MeshReplacementTemplate.ctor -= MeshReplacementTemplate_ctor;
        On.RoR2.SkinDef.GhostReplacementTemplate.ctor -= GhostReplacementTemplate_ctor;
        On.RoR2.SkinDef.LightReplacementTemplate.ctor -= LightReplacementTemplate_ctor;
        On.RoR2.SkinDef.MinionSkinTemplate.ctor -= MinionSkinTemplate_ctor;
        IL.RoR2.ProjectileGhostReplacementManager.FindProjectileGhostPrefab -= ProjectileGhostReplacementManager_FindProjectileGhostPrefab;
        IL.RoR2.MasterSummon.Perform -= MasterSummon_Perform;
    }
    private static void MinionSkinTemplate_ctor(On.RoR2.SkinDef.MinionSkinTemplate.orig_ctor orig, ref SkinDef.MinionSkinTemplate self, SkinDefParams.MinionSkinReplacement minionSkinReplacement)
    {
        self.SetSkillVariants(minionSkinReplacement.GetSkillVariants());
        orig(ref self, minionSkinReplacement);
    }
    private static void MasterSummon_Perform(ILContext il)
    {
        ILCursor c = new ILCursor(il);
        int newLocal = il.Body.Variables.Count;
        il.Body.Variables.Add(new(il.Import(typeof(List<SkillDef>))));
        il.Body.Variables.Add(new(il.Import(typeof(SkinDef))));
        if (
            c.TryGotoNext(MoveType.After,
                x => x.MatchLdloc(1),
                x => x.MatchCallvirt(typeof(CharacterBody).GetPropertyGetter(nameof(CharacterBody.master))),
                x => x.MatchStloc(2)
            ))
        {
            c.Emit(OpCodes.Ldloc_1);
            c.EmitDelegate(CollectSkills);
            c.Emit(OpCodes.Stloc, newLocal);
            il.Body.Variables.Add(new(il.Import(typeof(List<SkillDef>))));
            if (
                c.TryGotoNext(MoveType.After,
                    x => x.MatchLdfld<SkinDef.MinionSkinTemplate>(nameof(SkinDef.MinionSkinTemplate.minionSkin))
                ))
            {
                Instruction instruction = c.Next;
                Instruction instruction2 = c.Prev;
                c.Index--;
                c.Emit(OpCodes.Dup);
                c.Emit(OpCodes.Ldloc, newLocal);
                c.EmitDelegate(ApplyMinionSkinVariants);
                c.Emit(OpCodes.Dup);
                c.Emit(OpCodes.Stloc, newLocal + 1);
                c.Emit(OpCodes.Call, AccessTools.Method(typeof(UnityEngine.Object), "op_Implicit"));
                c.Emit(OpCodes.Brfalse_S, instruction2);
                c.Emit(OpCodes.Pop);
                c.Emit(OpCodes.Ldloc, newLocal + 1);
                c.Emit(OpCodes.Br, instruction);
            }
            else
            {
                Debug.LogError(il.Method.Name + " IL Hook 2 failed!");
            }
        }
        else
        {
            Debug.LogError(il.Method.Name + " IL Hook 1 failed!");
        }
    }
    private static void LightReplacementTemplate_ctor(On.RoR2.SkinDef.LightReplacementTemplate.orig_ctor orig, ref SkinDef.LightReplacementTemplate self, CharacterModel.LightInfo source, GameObject rootObject)
    {
        self.SetSkillVariants(source.GetSkillVariants());
        orig(ref self, source, rootObject);
    }
    private static void GhostReplacementTemplate_ctor(On.RoR2.SkinDef.GhostReplacementTemplate.orig_ctor orig, ref SkinDef.GhostReplacementTemplate self, SkinDefParams.ProjectileGhostReplacement projectileGhostReplacement)
    {
        self.SetSkillVariants(projectileGhostReplacement.GetSkillVariants());
        orig(ref self, projectileGhostReplacement);
    }
    private static void ProjectileGhostReplacementManager_FindProjectileGhostPrefab(ILContext il)
    {
        ILCursor c = new ILCursor(il);
        ILLabel iLLabel = null;
        Instruction lastInstruction = il.Instrs[il.Instrs.Count - 1];
        if (
            c.TryGotoNext(MoveType.After,
                x => x.MatchLdloc(5),
                x => x.MatchLdfld<SkinDef.GhostReplacementTemplate>(nameof(SkinDef.GhostReplacementTemplate.projectileIndex)),
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<ProjectileController>(nameof(ProjectileController.catalogIndex)),
                x => x.MatchBneUn(out iLLabel)
            ))
        {
            c.Emit(OpCodes.Ldloc, 5);
            c.Emit(OpCodes.Ldloc, 1);
            c.EmitDelegate(ReplaceProjectileGhost);
            c.Emit(OpCodes.Dup);
            c.Emit(OpCodes.Call, AccessTools.Method(typeof(UnityEngine.Object), "op_Implicit"));
            c.Emit(OpCodes.Brtrue_S, lastInstruction);
            c.Emit(OpCodes.Pop);
        }
        else
        {
            Debug.LogError(il.Method.Name + " IL Hook failed!");
        }
    }
    private static void MeshReplacementTemplate_ctor(On.RoR2.SkinDef.MeshReplacementTemplate.orig_ctor orig, ref SkinDef.MeshReplacementTemplate self, SkinDefParams.MeshReplacement source, GameObject rootObject)
    {
        self.SetSkillVariants(source.GetSkillVariants());
        orig(ref self, source, rootObject);
    }
    #endregion

    #region Harmony
    [HarmonyPatch]
    class Patches
    {
        [HarmonyPatch(typeof(SkinDef.RuntimeSkin), nameof(SkinDef.RuntimeSkin.ApplyAsync), MethodType.Enumerator)]
        [HarmonyILManipulator]
        private static void SkinDef_RuntimeSkin_ApplyAsync(MonoMod.Cil.ILContext il)
        {
            int newLocal = il.Body.Variables.Count;
            il.Body.Variables.Add(new(il.Import(typeof(List<SkillDef>))));
            il.Body.Variables.Add(new(il.Import(typeof(Mesh))));
            FieldReference fieldReference = null;
            Instruction lastInstruction = il.Instrs[il.Instrs.Count - 1];
            int i = 0;
            ILCursor c = new ILCursor(il);
            if (
                c.TryGotoNext(MoveType.After,
                    x => x.MatchLdarg(0),
                    x => x.MatchLdarg(0),
                    x => x.MatchLdfld(out fieldReference),
                    x => x.MatchCallvirt(typeof(GameObject).GetPropertyGetter(nameof(GameObject.transform))),
                    x => x.MatchStfld(out _)
                ))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldfld, fieldReference);
                c.Emit(OpCodes.Ldloc, 1);
                c.EmitDelegate(CacheSkillDefs);
                c.Emit(OpCodes.Stloc, newLocal);
                if (
                c.TryGotoNext(MoveType.After,
                    x => x.MatchLdloc(out _),
                    x => x.MatchLdflda<SkinDef.RuntimeSkin>(nameof(SkinDef.RuntimeSkin.rendererInfoTemplates)),
                    x => x.MatchLdloc(out _),
                    x => x.MatchCall(out _),
                    //x => x.MatchCall<HG.ReadOnlyArray<SkinDef.RendererInfoTemplate>>("get_Item"),
                    x => x.MatchCall(typeof(SkinDef.RendererInfoTemplate).GetPropertyGetter(nameof(SkinDef.RendererInfoTemplate.rendererInfoData))),
                    x => x.MatchStloc(out i)
                ))
                {
                    c.Emit(OpCodes.Ldloca, i);
                    c.Emit(OpCodes.Ldloc, newLocal);
                    c.EmitDelegate(ApplyRendererInfoSkillVariants);
                }
                else
                {
                    Debug.LogError(il.Method.Name + " IL Hook 2 failed!");
                }
                if (
                c.TryGotoNext(MoveType.After,
                    x => x.MatchStloc(8)
                ))
                {
                    Instruction instruction = c.Prev;
                    c.Index -= 2;
                    Instruction instruction2 = c.Next;
                    c.Emit(OpCodes.Dup);
                    c.Emit(OpCodes.Ldloc, newLocal);
                    c.EmitDelegate(CheckForLightInfoSkillVariants);
                    c.Emit(OpCodes.Brfalse_S, instruction2);
                    c.Emit(OpCodes.Ldloc, newLocal);
                    c.EmitDelegate(ApplyLightInfoSkillVariants);
                    c.Emit(OpCodes.Br, instruction);
                }
                else
                {
                    Debug.LogError(il.Method.Name + " IL Hook 3 failed!");
                }
                if (
                c.TryGotoNext(MoveType.After,
                    //x => x.MatchLdfld<SkinDef.MeshReplacementTemplate>(nameof(SkinDef.MeshReplacementTemplate.meshReference)),
                    //x => x.MatchCallvirt(out _),
                    x => x.MatchStloc(18)
                ))
                {
                    Instruction instruction = c.Prev;
                    c.Index -= 3;
                    Instruction instruction2 = c.Next;
                    c.Emit(OpCodes.Dup);
                    c.Emit(OpCodes.Ldloc, newLocal);
                    c.EmitDelegate(ApplyMeshReplacementSkillVariants);
                    c.Emit(OpCodes.Dup);
                    c.Emit(OpCodes.Stloc, newLocal + 1);
                    c.Emit(OpCodes.Call, AccessTools.Method(typeof(UnityEngine.Object), "op_Implicit"));
                    c.Emit(OpCodes.Brfalse_S, instruction2);
                    c.Emit(OpCodes.Pop);
                    c.Emit(OpCodes.Ldloc, newLocal + 1);
                    c.Emit(OpCodes.Br, instruction);
                }
                else
                {
                    Debug.LogError(il.Method.Name + " IL Hook 4 failed!");
                }
            }
            else
            {
                Debug.LogError(il.Method.Name + " IL Hook 1 failed!");
            }
            c.Goto(lastInstruction);
            c.Index--;
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldfld, fieldReference);
            c.Emit(OpCodes.Ldloc, 1);
            c.Emit(OpCodes.Ldloc, newLocal);
            c.EmitDelegate(RunOnSkinApplied);
        }
        [HarmonyPatch(typeof(SkinCatalog), nameof(SkinCatalog.Init), MethodType.Enumerator)]
        [HarmonyILManipulator]
        private static void SkinCatalog_Init(MonoMod.Cil.ILContext il)
        {
            ILCursor c = new ILCursor(il);
            if (
                c.TryGotoNext(MoveType.After,
                    x => x.MatchLdloc(1),
                    x => x.MatchLdloc(6),
                    x => x.MatchCallvirt(out _)
                    //x => x.MatchCallvirt<List<SkinDef>>(nameof(List<SkinDef>.Add))
                ))
            {
                c.Emit(OpCodes.Ldloc, 6);
                c.Emit(OpCodes.Ldloc, 2);
                c.EmitDelegate(AddSkinToBodyDictionary);
            }
            else
            {
                Debug.LogError(il.Method.Name + " IL Hook 1 failed!");
            }
            if (
                c.TryGotoNext(MoveType.Before,
                    x => x.MatchLdarg(0),
                    x => x.MatchLdcI4(0),
                    x => x.MatchStfld(out _),
                    x => x.MatchBr(out _)
                    //x => x.MatchCallvirt<List<SkinDef>>(nameof(List<SkinDef>.Add))
                ))
            {
                c.EmitDelegate(ApplyPendingSkinSkillVariations);
            }
            else
            {
                Debug.LogError(il.Method.Name + " IL Hook 2 failed!");
            }
        }
        [HarmonyPatch(typeof(SkinDef), nameof(SkinDef.ApplyAsync), MethodType.Enumerator)]
        [HarmonyILManipulator]
        private static void SkinDef_BakeAsync(MonoMod.Cil.ILContext il)
        {
            ILCursor c = new ILCursor(il);
            if (
                c.TryGotoNext(MoveType.Before,
                    x => x.MatchLdarg(0),
                    x => x.MatchLdloc(1),
                    x => x.MatchCall(typeof(SkinDef).GetPropertyGetter(nameof(SkinDef.runtimeSkin)))
                ))
            {
                c.Emit(OpCodes.Ldloc, 1);
                c.EmitDelegate(AddSkinDefToRuntimeSkin);
            }
            else
            {
                Debug.LogError(il.Method.Name + " IL Hook failed!");
            }
        }
        [HarmonyPatch(typeof(ModelSkinController), nameof(ModelSkinController.ApplySkinAsync), MethodType.Enumerator)]
        [HarmonyILManipulator]
        private static void ModelSkinController_ApplySkinAsync(MonoMod.Cil.ILContext il)
        {
            ILCursor c = new ILCursor(il);
            if (
                c.TryGotoNext(MoveType.Before,
                    x => x.MatchLdarg(0),
                    x => x.MatchLdfld(out _),
                    x => x.MatchLdloc(1),
                    x => x.MatchCall(typeof(ModelSkinController).GetPropertyGetter(nameof(ModelSkinController.currentSkinIndex))),
                    x => x.MatchBeq(out _)
                ))
            {
                c.RemoveRange(5);
            }
            else
            {
                Debug.LogError(il.Method.Name + " IL Hook failed!");
            }
        }
    }
    #endregion

    #region Delegates
    public delegate void OnSkinApplied(GameObject gameObject, List<SkillDef> skillDefs);
    #endregion

    #region Structs
    public interface ISkillVariantStruct<T>
    {
        public bool lowPriority { get; set; }
        public abstract bool NullCheck();
        public abstract bool Compare(T t);
        public abstract void Add(ref T t);
        public abstract void AddPending(SkinDefParams skinDefParams, T t);
        public abstract void AddPending(ModelSkinController modelSkinController, T t);
    }
    [Serializable]
    public struct RendererInfoSkillVariant : ISkillVariantStruct<CharacterModel.RendererInfo>
    {
        [PrefabReference]
        public Renderer renderer;
        public SkillDef skillDef;
        public Material material;
        public ShadowCastingMode defaultShadowCastingMode;
        public bool ignoreOverlays;
        public bool hideOnDeath;
        public bool ignoresMaterialOverrides;
        private bool _lowPriority;
        public bool lowPriority { get => _lowPriority; set => _lowPriority = value; }
        public bool Compare(CharacterModel.RendererInfo t) => t.renderer == renderer;
        public bool NullCheck() => renderer == null;
        public void Add(ref CharacterModel.RendererInfo t) => t.AddSkillVariant(this);
        public void AddPending(SkinDefParams skinDefParams, CharacterModel.RendererInfo t) => AddPendingSkillVariant(skinDefParams, t, this);
        public void AddPending(ModelSkinController modelSkinController, CharacterModel.RendererInfo t) => AddPendingSkillVariant(modelSkinController, t, this);
    }
    [Serializable]
    public struct MeshReplacementSkillVariant : ISkillVariantStruct<SkinDefParams.MeshReplacement>
    {
        [PrefabReference]
        public Renderer renderer;
        public SkillDef skillDef;
        public Mesh mesh;
        private bool _lowPriority;
        public bool lowPriority { get => _lowPriority; set => _lowPriority = value; }
        public bool Compare(SkinDefParams.MeshReplacement t) => t.renderer == renderer;
        public bool NullCheck() => renderer == null;
        public void Add(ref SkinDefParams.MeshReplacement t) => t.AddSkillVariant(this);
        public void AddPending(SkinDefParams skinDefParams, SkinDefParams.MeshReplacement t) => AddPendingSkillVariant(skinDefParams, t, this);
        public void AddPending(ModelSkinController modelSkinController, SkinDefParams.MeshReplacement t) => AddPendingSkillVariant(modelSkinController, t, this);
    }
    [Serializable]
    public struct LightInfoSkillVariant : ISkillVariantStruct<CharacterModel.LightInfo>
    {
        [PrefabReference]
        public Light originalLight;
        public SkillDef skillDef;
        public CharacterModel.LightInfo lightInfo;
        private bool _lowPriority;
        public bool lowPriority { get => _lowPriority; set => _lowPriority = value; }
        public bool Compare(CharacterModel.LightInfo t) => t.light == originalLight;
        public bool NullCheck() => originalLight == null;
        public void Add(ref CharacterModel.LightInfo t) => t.AddSkillVariant(this);
        public void AddPending(SkinDefParams skinDefParams, CharacterModel.LightInfo t) => AddPendingSkillVariant(skinDefParams, t, this);
        public void AddPending(ModelSkinController modelSkinController, CharacterModel.LightInfo t) => AddPendingSkillVariant(modelSkinController, t, this);
    }
    [Serializable]
    public struct ProjectileGhostReplacementSkillVariant : ISkillVariantStruct<SkinDefParams.ProjectileGhostReplacement>
    {
        public GameObject projectilePrefab;
        public SkillDef skillDef;
        public GameObject projectileGhostReplacementPrefab;
        private bool _lowPriority;
        public bool lowPriority { get => _lowPriority; set => _lowPriority = value; }
        public bool Compare(SkinDefParams.ProjectileGhostReplacement t) => t.projectilePrefab == projectilePrefab;
        public bool NullCheck() => projectilePrefab == null;
        public void Add(ref SkinDefParams.ProjectileGhostReplacement t) => t.AddSkillVariant(this);
        public void AddPending(SkinDefParams skinDefParams, SkinDefParams.ProjectileGhostReplacement t) => AddPendingSkillVariant(skinDefParams, t, this);
        public void AddPending(ModelSkinController modelSkinController, SkinDefParams.ProjectileGhostReplacement t) => AddPendingSkillVariant(modelSkinController, t, this);
    }
    [Serializable]
    public struct MinionSkinReplacementSkillVariant : ISkillVariantStruct<SkinDefParams.MinionSkinReplacement>
    {
        public GameObject minionBodyPrefab;
        public SkillDef skillDef;
        public SkinDef minionSkin;
        private bool _lowPriority;
        public bool lowPriority { get => _lowPriority; set => _lowPriority = value; }
        public bool Compare(SkinDefParams.MinionSkinReplacement t) => t.minionBodyPrefab == minionBodyPrefab;
        public bool NullCheck() => minionBodyPrefab == null;
        public void Add(ref SkinDefParams.MinionSkinReplacement t) => t.AddSkillVariant(this);
        public void AddPending(SkinDefParams skinDefParams, SkinDefParams.MinionSkinReplacement t) => AddPendingSkillVariant(skinDefParams, t, this);
        public void AddPending(ModelSkinController modelSkinController, SkinDefParams.MinionSkinReplacement t) => AddPendingSkillVariant(modelSkinController, t, this);
    }
    #endregion

    #region Internal
    private static List<SkillDef> CacheSkillDefs(GameObject gameObject, SkinDef.RuntimeSkin runtimeSkin)
    {
        HurtBoxGroup hurtBoxGroup = gameObject.GetComponent<HurtBoxGroup>();
        if (hurtBoxGroup == null) return CacheSkillDefsInLobby(gameObject, runtimeSkin);
        HurtBox hurtBox = hurtBoxGroup.mainHurtBox;
        if (hurtBox == null) return CacheSkillDefsInLobby(gameObject, runtimeSkin);
        HealthComponent healthComponent = hurtBox.healthComponent;
        if (healthComponent == null) return CacheSkillDefsInLobby(gameObject, runtimeSkin);
        CharacterBody characterBody = healthComponent.body;
        if (characterBody == null) return null;
        return CollectSkills(characterBody);
    }
    private static List<SkillDef> CollectSkills(CharacterBody characterBody)
    {
        SkillLocator skillLocator = characterBody.skillLocator;
        if(skillLocator == null) return null;
        GenericSkill[] genericSkills = skillLocator.allSkills;
        if (genericSkills == null) return null;
        List<SkillDef> skillDefs = [];
        foreach (GenericSkill genericSkill in skillLocator.allSkills)
        {
            if (genericSkill == null || genericSkill.baseSkill == null) continue;
            skillDefs.Add(genericSkill.baseSkill);
        }
        return skillDefs;
    }
    private static List<SkillDef> CacheSkillDefsInLobby(GameObject gameObject, SkinDef.RuntimeSkin runtimeSkin)
    {
        BodyIndex bodyIndex = skinToBody[runtimeSkin.GetSkinDef()];
        GameObject body = BodyCatalog.GetBodyPrefab(bodyIndex);
        if (body == null) return null;
        SurvivorMannequinSlotController componentInParent = gameObject.GetComponentInParent<SurvivorMannequinSlotController>();
        if (componentInParent == null || componentInParent.networkUser == null) return null;
        Loadout loadout = Loadout.RequestInstance();
        componentInParent.networkUser.networkLoadout.CopyLoadout(loadout);
        GenericSkill[] genericSkills = body.GetComponents<GenericSkill>();
        if (genericSkills == null || genericSkills.Length == 0) return null;
        List<SkillDef> skillDefs2 = [];
        for (int i = 0; i < genericSkills.Length; i++)
        {
            GenericSkill genericSkill = genericSkills[i];
            if (genericSkill == null) continue;
            SkillDef skillDef = genericSkill.skillFamily.variants[loadout.bodyLoadoutManager.GetSkillVariant(bodyIndex, i)].skillDef;
            if (skillDef == null) continue;
            skillDefs2.Add(skillDef);
        }
        return skillDefs2;
    }
    private static void ApplyRendererInfoSkillVariants(ref CharacterModel.RendererInfo rendererInfoData, List<SkillDef> skillDefs)
    {
        if (skillDefs == null) return;
        object[] objects = rendererInfoData.GetSkillVariants();
        if (objects == null) return;
        bool set = false;
        foreach (object obj in objects)
        {
            if (obj == null) continue;
            RendererInfoSkillVariant rendererInfoSkillVariant = (RendererInfoSkillVariant)obj;
            SkillDef skillDef = rendererInfoSkillVariant.skillDef;
            if (skillDef == null) continue;
            if (!skillDefs.Contains(skillDef)) continue;
            if (set && rendererInfoSkillVariant.lowPriority) continue;
            rendererInfoData.defaultMaterial = rendererInfoSkillVariant.material;
            rendererInfoData.defaultShadowCastingMode = rendererInfoSkillVariant.defaultShadowCastingMode;
            rendererInfoData.ignoreOverlays = rendererInfoSkillVariant.ignoreOverlays;
            rendererInfoData.hideOnDeath = rendererInfoSkillVariant.hideOnDeath;
            rendererInfoData.ignoresMaterialOverrides = rendererInfoSkillVariant.ignoresMaterialOverrides;
            if (!rendererInfoSkillVariant.lowPriority) continue;
        }
    }

    private static Mesh ApplyMeshReplacementSkillVariants(ref SkinDef.MeshReplacementTemplate meshReplacementTemplate, List<SkillDef> skillDefs)
    {
        Mesh mesh = null;
        if (skillDefs == null) return mesh;
        object[] objects = meshReplacementTemplate.GetSkillVariants();
        if (objects == null) return mesh;
        bool set = false;
        foreach (object obj in objects)
        {
            if (obj == null) continue;
            MeshReplacementSkillVariant meshReplacementSkillVariant = (MeshReplacementSkillVariant)obj;
            SkillDef skillDef = meshReplacementSkillVariant.skillDef;
            if (skillDef == null) continue;
            if (!skillDefs.Contains(skillDef)) continue;
            if (set && meshReplacementSkillVariant.lowPriority) continue;
            mesh = meshReplacementSkillVariant.mesh;
            if (!meshReplacementSkillVariant.lowPriority) set = true;
        }
        return mesh;
    }
    private static bool CheckMinionSkinSkillVariants(ref SkinDef.MinionSkinTemplate minionSkinTemplate, List<SkillDef> skillDefs)
    {
        if (skillDefs == null) return false;
        object[] objects = minionSkinTemplate.GetSkillVariants();
        if (objects == null) return false;
        foreach (object obj in objects)
        {
            if (obj == null) continue;
            MinionSkinReplacementSkillVariant minionSkinReplacementSkillVariant = (MinionSkinReplacementSkillVariant)obj;
            SkillDef skillDef = minionSkinReplacementSkillVariant.skillDef;
            if (skillDef == null) continue;
            if (!skillDefs.Contains(skillDef)) continue;
            return true;
        }
        return false;
    }
    private static SkinDef ApplyMinionSkinVariants(ref SkinDef.MinionSkinTemplate minionSkinTemplate, List<SkillDef> skillDefs)
    {
        SkinDef skinDef = minionSkinTemplate.minionSkin;
        if (skillDefs == null) return skinDef;
        object[] objects = minionSkinTemplate.GetSkillVariants();
        if (objects == null) return skinDef;
        bool set = false;
        foreach (object obj in objects)
        {
            if (obj == null) continue;
            MinionSkinReplacementSkillVariant minionSkinReplacementSkillVariant = (MinionSkinReplacementSkillVariant)obj;
            SkillDef skillDef = minionSkinReplacementSkillVariant.skillDef;
            if (skillDef == null) continue;
            if (!skillDefs.Contains(skillDef)) continue;
            if (set && minionSkinReplacementSkillVariant.lowPriority) continue;
            skinDef = minionSkinReplacementSkillVariant.minionSkin;
            if (minionSkinReplacementSkillVariant.lowPriority) set = true;
        }
        return skinDef;
    }
    private static bool CheckForLightInfoSkillVariants(ref SkinDef.LightReplacementTemplate lightReplacementTemplate, List<SkillDef> skillDefs)
    {
        if (skillDefs == null) return false;
        object[] objects = lightReplacementTemplate.GetSkillVariants();
        if (objects == null) return false;
        foreach (object obj in objects)
        {
            if (obj == null) continue;
            LightInfoSkillVariant lightInfoSkillVariant = (LightInfoSkillVariant)obj;
            SkillDef skillDef = lightInfoSkillVariant.skillDef;
            if (skillDef == null) continue;
            if (!skillDefs.Contains(skillDef)) continue;
            return true;
        }
        return false;
    }
    private static CharacterModel.LightInfo ApplyLightInfoSkillVariants(ref SkinDef.LightReplacementTemplate lightReplacementTemplate, List<SkillDef> skillDefs)
    {
        CharacterModel.LightInfo lightInfo = lightReplacementTemplate.data;
        if (skillDefs == null) return lightInfo;
        object[] objects = lightReplacementTemplate.GetSkillVariants();
        if (objects == null) return lightInfo;
        bool set = false;
        foreach (object obj in objects)
        {
            if (obj == null) continue;
            LightInfoSkillVariant lightInfoSkillVariant = (LightInfoSkillVariant)obj;
            SkillDef skillDef = lightInfoSkillVariant.skillDef;
            if (skillDef == null) continue;
            if (!skillDefs.Contains(skillDef)) continue;
            if (set && lightInfoSkillVariant.lowPriority) continue;
            lightInfo = lightInfoSkillVariant.lightInfo;
            if(!lightInfoSkillVariant.lowPriority) set = true;
        }
        return lightInfo;
    }
    private static void AddSkinToBodyDictionary(SkinDef skinDef, BodyIndex bodyIndex)
    {
        if (!skinToBody.ContainsKey(skinDef)) skinToBody.Add(skinDef, bodyIndex);
    }
    private static void ApplyPendingSkinSkillVariations()
    {
        _isLoaded = true;
        foreach (SkinSkillVariantsDef skinSkillVariantsDef in SkinSkillVariantsDef.pendingSkinSkillVariantsDefs)
        {
            if (skinSkillVariantsDef == null) continue;
            skinSkillVariantsDef.Apply();
        }
    }
    private static void AddSkinDefToRuntimeSkin(SkinDef skinDef) => skinDef.runtimeSkin.SetSkinDef(skinDef);
    internal static SkinDefParams GetSkinDefParams(SkinDef skinDef) => skinDef.skinDefParams == null ? skinDef.skinDefParamsAddress.LoadAsset().Result : skinDef.skinDefParams;
    internal static SkinDefParams GetOptimizedSkinDefParams(SkinDef skinDef) => skinDef.optimizedSkinDefParams == null ? skinDef.optimizedSkinDefParamsAddress.LoadAsset().Result : skinDef.optimizedSkinDefParams;
    private static ref CharacterModel.RendererInfo GetRendererInfoByName(SkinDefParams skinDefParams, string rendererName)
    {
        CharacterModel.RendererInfo[] rendererInfos = skinDefParams.rendererInfos;
        for (int i = 0; i < rendererInfos.Length; i++)
        {
            ref CharacterModel.RendererInfo rendererInfo = ref rendererInfos[i];
            if(rendererInfo.renderer == null) continue;
            if(rendererInfo.renderer.name == rendererName) return ref rendererInfo;
        }
        return ref rendererInfos[0];
    }
    private static ref SkinDefParams.MeshReplacement GetMeshReplacementByName(SkinDefParams skinDefParams, string meshRendererName)
    {
        SkinDefParams.MeshReplacement[] meshReplacements = skinDefParams.meshReplacements;
        for (int i = 0; i < meshReplacements.Length; i++)
        {
            ref SkinDefParams.MeshReplacement meshReplacement = ref meshReplacements[i];
            if (meshReplacement.renderer == null) continue;
            if (meshReplacement.renderer.name == meshRendererName) return ref meshReplacement;
        }
        return ref meshReplacements[0];
    }
    private static ref CharacterModel.LightInfo GetLightInfoByName(SkinDefParams skinDefParams, string lightInfoName)
    {
        CharacterModel.LightInfo[] lightInfos = skinDefParams.lightReplacements;
        for (int i = 0; i < lightInfos.Length; i++)
        {
            ref CharacterModel.LightInfo lightInfo = ref lightInfos[i];
            if (lightInfo.light == null) continue;
            if (lightInfo.light.name == lightInfoName) return ref lightInfo;
        }
        return ref lightInfos[0];
    }
    private static ref SkinDefParams.ProjectileGhostReplacement GetProjectileGhostReplacementByName(SkinDefParams skinDefParams, string projectileName)
    {
        SkinDefParams.ProjectileGhostReplacement[] projectileghostReplacements = skinDefParams.projectileGhostReplacements;
        for (int i = 0; i < projectileghostReplacements.Length; i++)
        {
            ref SkinDefParams.ProjectileGhostReplacement projectileGhostReplacement = ref projectileghostReplacements[i];
            if (projectileGhostReplacement.projectilePrefab == null) continue;
            if (projectileGhostReplacement.projectilePrefab.name == projectileName) return ref projectileGhostReplacement;
        }
        return ref projectileghostReplacements[0];
    }
    private static ref SkinDefParams.MinionSkinReplacement GetMinionSkinReplacementByName(SkinDefParams skinDefParams, string minionBodyName)
    {
        SkinDefParams.MinionSkinReplacement[] minionSkinReplacements = skinDefParams.minionSkinReplacements;
        for (int i = 0; i < minionSkinReplacements.Length; i++)
        {
            ref SkinDefParams.MinionSkinReplacement minionSkinReplacement = ref minionSkinReplacements[i];
            if (minionSkinReplacement.minionBodyPrefab == null) continue;
            if (minionSkinReplacement.minionBodyPrefab.name == minionBodyName) return ref minionSkinReplacement;
        }
        return ref minionSkinReplacements[0];
    }
    internal static void AddItemToObjects<T>(ref object[] objects, T t)
    {
        if (objects == null)
        {
            objects = [t];
        }
        else
        {
            int arraySize = objects.Length;
            Array.Resize(ref objects, arraySize + 1);
            objects[arraySize] = t;
        }
    }

    private static void RunOnSkinApplied(GameObject gameObject, SkinDef.RuntimeSkin runtimeSkin, List<SkillDef> skillDefs)
    {
        SkinDef skinDef = runtimeSkin.GetSkinDef();
        if (skinDef == null) return;
        OnSkinApplied onSkinApplied = skinDef.GetOnSkinApplied();
        if (onSkinApplied == null) return;
        onSkinApplied.Invoke(gameObject, skillDefs);
    }
    private static GameObject ReplaceProjectileGhost(SkinDef.GhostReplacementTemplate ghostReplacementTemplate, CharacterBody characterBody)
    {
        object[] objects = ghostReplacementTemplate.GetSkillVariants();
        if (objects == null || objects.Length == 0) return null;
        SkillLocator skillLocator = characterBody.skillLocator;
        if (skillLocator == null) return null;
        GenericSkill[] genericSkills = skillLocator.allSkills;
        if (genericSkills == null || genericSkills.Length == 0) return null;
        List<SkillDef> skillDefs = new List<SkillDef>();
        foreach (GenericSkill genericSkill in genericSkills)
        {
            if (genericSkill == null || genericSkill.baseSkill == null) continue;
            skillDefs.Add(genericSkill.baseSkill);
        }
        GameObject gameObject = null;
        bool set = false;
        for (int i = 0; i < objects.Length; i++)
        {
            ProjectileGhostReplacementSkillVariant projectileGhostReplacementSkillVariant = (ProjectileGhostReplacementSkillVariant)objects[i];
            if (projectileGhostReplacementSkillVariant.skillDef == null) continue;
            if (!skillDefs.Contains(projectileGhostReplacementSkillVariant.skillDef)) continue;
            if (set && projectileGhostReplacementSkillVariant.lowPriority) continue;
            gameObject = projectileGhostReplacementSkillVariant.projectileGhostReplacementPrefab;
            if (!projectileGhostReplacementSkillVariant.lowPriority) set = true;
        }
        return gameObject;
    }

    private static SkinSkillVariantsDef GetOrCreateSkinSkillVariantsDef(SkinDefParams skinDefParams)
    {
        SkinSkillVariantsDef skinSkillVariantsDef = skinDefParams.GetSkinSkillVariantsDef();
        if (skinSkillVariantsDef == null)
        {
            skinSkillVariantsDef = ScriptableObject.CreateInstance<SkinSkillVariantsDef>();
            (skinSkillVariantsDef as ScriptableObject).name = skinDefParams.name;
            skinSkillVariantsDef.Register();
            skinSkillVariantsDef.AddSkinDefParams(skinDefParams);
            skinDefParams.SetSkinSkillVariantsDef(skinSkillVariantsDef);
        }
        return skinSkillVariantsDef;
    }
    private static SkinSkillVariantsDef GetOrCreateSkinSkillVariantsDef(ModelSkinController modelSkinController)
    {
        SkinSkillVariantsDef skinSkillVariantsDef = modelSkinController.GetSkinSkillVariantsDef();
        if (skinSkillVariantsDef == null)
        {
            skinSkillVariantsDef = ScriptableObject.CreateInstance<SkinSkillVariantsDef>();
            (skinSkillVariantsDef as ScriptableObject).name = modelSkinController.name;
            skinSkillVariantsDef.Register();
            skinSkillVariantsDef.modelSkinController = modelSkinController;
            modelSkinController.SetSkinSkillVariantsDef(skinSkillVariantsDef);
        }
        return skinSkillVariantsDef;
    }
    private static void AddPendingSkillVariant(SkinDefParams skinDefParams, CharacterModel.RendererInfo rendererInfo, RendererInfoSkillVariant rendererInfoSkillVariant)
    {
        SkinSkillVariantsDef skinSkillVariantsDef = GetOrCreateSkinSkillVariantsDef(skinDefParams);
        rendererInfoSkillVariant.renderer = rendererInfo.renderer;
        skinSkillVariantsDef.AddRendererInfoSkillVariant(rendererInfoSkillVariant);
    }
    private static void AddPendingSkillVariant(ModelSkinController modelSkinController, CharacterModel.RendererInfo rendererInfo, RendererInfoSkillVariant rendererInfoSkillVariant)
    {
        SkinSkillVariantsDef skinSkillVariantsDef = GetOrCreateSkinSkillVariantsDef(modelSkinController);
        rendererInfoSkillVariant.renderer = rendererInfo.renderer;
        skinSkillVariantsDef.AddRendererInfoSkillVariant(rendererInfoSkillVariant);
    }
    private static void AddPendingSkillVariant(SkinDefParams skinDefParams, SkinDefParams.MeshReplacement meshReplacement, MeshReplacementSkillVariant meshReplacementSkillVariant)
    {
        SkinSkillVariantsDef skinSkillVariantsDef = GetOrCreateSkinSkillVariantsDef(skinDefParams);
        meshReplacementSkillVariant.renderer = meshReplacement.renderer;
        skinSkillVariantsDef.AddMeshReplacementSkillVariant(meshReplacementSkillVariant);
    }
    private static void AddPendingSkillVariant(ModelSkinController modelSkinController, SkinDefParams.MeshReplacement meshReplacement, MeshReplacementSkillVariant meshReplacementSkillVariant)
    {
        SkinSkillVariantsDef skinSkillVariantsDef = GetOrCreateSkinSkillVariantsDef(modelSkinController);
        meshReplacementSkillVariant.renderer = meshReplacement.renderer;
        skinSkillVariantsDef.AddMeshReplacementSkillVariant(meshReplacementSkillVariant);
    }
    private static void AddPendingSkillVariant(SkinDefParams skinDefParams, CharacterModel.LightInfo lightInfo, LightInfoSkillVariant lightInfoSkillVariant)
    {
        SkinSkillVariantsDef skinSkillVariantsDef = GetOrCreateSkinSkillVariantsDef(skinDefParams);
        lightInfoSkillVariant.originalLight = lightInfo.light;
        skinSkillVariantsDef.AddLightInfoSkillVariant(lightInfoSkillVariant);
    }
    private static void AddPendingSkillVariant(ModelSkinController modelSkinController, CharacterModel.LightInfo lightInfo, LightInfoSkillVariant lightInfoSkillVariant)
    {
        SkinSkillVariantsDef skinSkillVariantsDef = GetOrCreateSkinSkillVariantsDef(modelSkinController);
        lightInfoSkillVariant.originalLight = lightInfo.light;
        skinSkillVariantsDef.AddLightInfoSkillVariant(lightInfoSkillVariant);
    }
    private static void AddPendingSkillVariant(SkinDefParams skinDefParams, SkinDefParams.ProjectileGhostReplacement projectileGhostReplacement, ProjectileGhostReplacementSkillVariant projectileGhostReplacementSkillVariant)
    {
        SkinSkillVariantsDef skinSkillVariantsDef = GetOrCreateSkinSkillVariantsDef(skinDefParams);
        projectileGhostReplacementSkillVariant.projectilePrefab = projectileGhostReplacement.projectilePrefab;
        skinSkillVariantsDef.AddProjectileGhostReplacementSkillVariant(projectileGhostReplacementSkillVariant);
    }
    private static void AddPendingSkillVariant(ModelSkinController modelSkinController, SkinDefParams.ProjectileGhostReplacement projectileGhostReplacement, ProjectileGhostReplacementSkillVariant projectileGhostReplacementSkillVariant)
    {
        SkinSkillVariantsDef skinSkillVariantsDef = GetOrCreateSkinSkillVariantsDef(modelSkinController);
        projectileGhostReplacementSkillVariant.projectilePrefab = projectileGhostReplacement.projectilePrefab;
        skinSkillVariantsDef.AddProjectileGhostReplacementSkillVariant(projectileGhostReplacementSkillVariant);
    }
    private static void AddPendingSkillVariant(SkinDefParams skinDefParams, SkinDefParams.MinionSkinReplacement minionSkinReplacement, MinionSkinReplacementSkillVariant minionSkinReplacementSkillVariant)
    {
        SkinSkillVariantsDef skinSkillVariantsDef = GetOrCreateSkinSkillVariantsDef(skinDefParams);
        minionSkinReplacementSkillVariant.minionBodyPrefab = minionSkinReplacement.minionBodyPrefab;
        skinSkillVariantsDef.AddMinionSkinReplacementSkillVariant(minionSkinReplacementSkillVariant);
    }
    private static void AddPendingSkillVariant(ModelSkinController modelSkinController, SkinDefParams.MinionSkinReplacement minionSkinReplacement, MinionSkinReplacementSkillVariant minionSkinReplacementSkillVariant)
    {
        SkinSkillVariantsDef skinSkillVariantsDef = GetOrCreateSkinSkillVariantsDef(modelSkinController);
        minionSkinReplacementSkillVariant.minionBodyPrefab = minionSkinReplacement.minionBodyPrefab;
        skinSkillVariantsDef.AddMinionSkinReplacementSkillVariant(minionSkinReplacementSkillVariant);
    }
    private static void HandleAddition<T1, T2>(SkinDefParams skinDefParams, T1 t1, ref T2 t2) where T1 : ISkillVariantStruct<T2>
    {
        if (_isLoaded)
        {
            t1.Add(ref t2);
        }
        else
        {
            t1.AddPending(skinDefParams, t2);
        }
    }
    private static void HandleAddition<T1, T2>(ModelSkinController modelSkinController, T1 t1, ref T2 t2) where T1 : ISkillVariantStruct<T2>
    {
        if (_isLoaded)
        {
            t1.Add(ref t2);
        }
        else
        {
            t1.AddPending(modelSkinController, t2);
        }
    }
    internal static SkinSkillVariantsDef GetSkinSkillVariantsDef(this SkinDefParams skinDefParams) => SkinAPIInterop.GetSkinSkillVariantsDef(skinDefParams) == null ? null : SkinAPIInterop.GetSkinSkillVariantsDef(skinDefParams) as SkinSkillVariantsDef;
    internal static void SetSkinSkillVariantsDef(this SkinDefParams skinDefParams, SkinSkillVariantsDef skinSkillVariantsDef) => SkinAPIInterop.SetSkinSkillVariantsDef(skinDefParams, skinSkillVariantsDef);
    internal static SkinSkillVariantsDef GetSkinSkillVariantsDef(this ModelSkinController modelSkinController) => SkinAPIInterop.GetSkinSkillVariantsDef(modelSkinController) == null ? null : SkinAPIInterop.GetSkinSkillVariantsDef(modelSkinController) as SkinSkillVariantsDef;
    internal static void SetSkinSkillVariantsDef(this ModelSkinController modelSkinController, SkinSkillVariantsDef skinSkillVariantsDef) => SkinAPIInterop.SetSkinSkillVariantsDef(modelSkinController, skinSkillVariantsDef);
    #endregion

    #region Public
    /// <summary>
    /// Finds the renderer by its name and adds RendererInfoSkillVariant to it.
    /// </summary>
    /// <param name="skinDef">SkinDef for RendererInfoSkillVariant to apply to.</param>
    /// <param name="rendererName">Name filter to find needed RendererInfo.</param>
    /// <param name="rendererInfoSkillVariant">Info struct.</param>
    public static void AddRenderInfoSkillVariant(this SkinDef skinDef, string rendererName, RendererInfoSkillVariant rendererInfoSkillVariant) => GetSkinDefParams(skinDef).AddRenderInfoSkillVariant(rendererName, rendererInfoSkillVariant);
    /// <summary>
    /// Finds the renderer by its name and adds RendererInfoSkillVariant to it.
    /// </summary>
    /// <param name="skinDefParams">SkinDefParams for RendererInfoSkillVariant to apply to.</param>
    /// <param name="rendererName">Name filter to find needed RendererInfo.</param>
    /// <param name="rendererInfoSkillVariant">Info struct.</param>
    public static void AddRenderInfoSkillVariant(this SkinDefParams skinDefParams, string rendererName, RendererInfoSkillVariant rendererInfoSkillVariant)
    {
        SkinSkillVariants.SetHooks();
        ref CharacterModel.RendererInfo rendererInfo = ref GetRendererInfoByName(skinDefParams, rendererName);
        HandleAddition(skinDefParams, rendererInfoSkillVariant, ref rendererInfo);
    }
    /// <summary>
    /// Finds the renderer by its name and adds RendererInfoSkillVariant to it.
    /// RendererInfoSkillVariant added with ModelSkinController will apply to all skins and have lower priority.
    /// </summary>
    /// <param name="modelSkinController">ModelSkinController for RendererInfoSkillVariant to apply to.</param>
    /// <param name="rendererName">Name filter to find needed RendererInfo.</param>
    /// <param name="rendererInfoSkillVariant">Info struct.</param>
    public static void AddRenderInfoSkillVariant(this ModelSkinController modelSkinController, string rendererName, RendererInfoSkillVariant rendererInfoSkillVariant)
    {
        SkinSkillVariants.SetHooks();
        ref CharacterModel.RendererInfo rendererInfo = ref GetRendererInfoByName(GetSkinDefParams(modelSkinController.skins[0]), rendererName);
        HandleAddition(modelSkinController, rendererInfoSkillVariant, ref rendererInfo);
    }
    /// <summary>
    /// Finds the renderer by its index and adds RendererInfoSkillVariant to it.
    /// </summary>
    /// <param name="skinDef">SkinDef for RendererInfoSkillVariant to apply to.</param>
    /// <param name="rendererIndex">Index filter to find needed RendererInfo.</param>
    /// <param name="rendererInfoSkillVariant">Info struct.</param>
    public static void AddRenderInfoSkillVariant(this SkinDef skinDef, int rendererIndex, RendererInfoSkillVariant rendererInfoSkillVariant) => GetSkinDefParams(skinDef).AddRenderInfoSkillVariant(rendererIndex, rendererInfoSkillVariant);
    /// <summary>
    /// Finds the renderer by its index and adds RendererInfoSkillVariant to it.
    /// </summary>
    /// <param name="skinDefParams">SkinDefParams for RendererInfoSkillVariant to apply to.</param>
    /// <param name="rendererIndex">Index filter to find needed RendererInfo.</param>
    /// <param name="rendererInfoSkillVariant">Info struct.</param>
    public static void AddRenderInfoSkillVariant(this SkinDefParams skinDefParams, int rendererIndex, RendererInfoSkillVariant rendererInfoSkillVariant)
    {
        SkinSkillVariants.SetHooks();
        ref CharacterModel.RendererInfo rendererInfo = ref skinDefParams.rendererInfos[rendererIndex];
        HandleAddition(skinDefParams, rendererInfoSkillVariant, ref rendererInfo);
    }
    /// <summary>
    /// Finds the renderer by its index and adds RendererInfoSkillVariant to it.
    /// RendererInfoSkillVariant added with ModelSkinController will apply to all skins and have lower priority.
    /// </summary>
    /// <param name="modelSkinController">ModelSkinController for RendererInfoSkillVariant to apply to.</param>
    /// <param name="rendererIndex">Index filter to find needed RendererInfo.</param>
    /// <param name="rendererInfoSkillVariant">Info struct.</param>
    public static void AddRenderInfoSkillVariant(this ModelSkinController modelSkinController, int rendererIndex, RendererInfoSkillVariant rendererInfoSkillVariant)
    {
        SkinSkillVariants.SetHooks();
        ref CharacterModel.RendererInfo rendererInfo = ref GetSkinDefParams(modelSkinController.skins[0]).rendererInfos[rendererIndex];
        HandleAddition(modelSkinController, rendererInfoSkillVariant, ref rendererInfo);
    }
    /// <summary>
    /// Directly adds RendererInfoSkillVariant to RendererInfo.
    /// Please don't use it unless you know what you are doing.
    /// </summary>
    /// <param name="rendererInfo">The RendererInfo itself.</param>
    /// <param name="rendererInfoSkillVariant">Info struct.</param>
    public static void AddSkillVariant(this ref CharacterModel.RendererInfo rendererInfo, RendererInfoSkillVariant rendererInfoSkillVariant)
    {
        SkinSkillVariants.SetHooks();
        ref object[] objects = ref rendererInfo.GetSkillVariants();
        AddItemToObjects(ref objects, rendererInfoSkillVariant);
    }
    public static void AddMeshReplacementSkillVariant(this SkinDef skinDef, string meshRendererName, MeshReplacementSkillVariant meshReplacementSkillVariant) => GetSkinDefParams(skinDef).AddMeshReplacementSkillVariant(meshRendererName, meshReplacementSkillVariant);
    public static void AddMeshReplacementSkillVariant(this SkinDefParams skinDefParams, string meshRendererName, MeshReplacementSkillVariant meshReplacementSkillVariant)
    {
        SkinSkillVariants.SetHooks();
        ref SkinDefParams.MeshReplacement meshReplacement = ref GetMeshReplacementByName(skinDefParams, meshRendererName);
        HandleAddition(skinDefParams, meshReplacementSkillVariant, ref meshReplacement);
    }
    public static void AddMeshReplacementSkillVariant(this ModelSkinController modelSkinController, string meshRendererName, MeshReplacementSkillVariant meshReplacementSkillVariant)
    {
        SkinSkillVariants.SetHooks();
        ref SkinDefParams.MeshReplacement meshReplacement = ref GetMeshReplacementByName(GetSkinDefParams(modelSkinController.skins[0]), meshRendererName);
        HandleAddition(modelSkinController, meshReplacementSkillVariant, ref meshReplacement);
    }
    public static void AddMeshReplacementSkillVariant(this SkinDef skinDef, int meshRendererIndex, MeshReplacementSkillVariant meshReplacementSkillVariant) => GetSkinDefParams(skinDef).AddMeshReplacementSkillVariant(meshRendererIndex, meshReplacementSkillVariant);
    public static void AddMeshReplacementSkillVariant(this SkinDefParams skinDefParams, int meshRendererIndex, MeshReplacementSkillVariant meshReplacementSkillVariant)
    {
        SkinSkillVariants.SetHooks();
        ref SkinDefParams.MeshReplacement meshReplacement = ref skinDefParams.meshReplacements[meshRendererIndex];
        HandleAddition(skinDefParams, meshReplacementSkillVariant, ref meshReplacement);
    }
    public static void AddMeshReplacementSkillVariant(this ModelSkinController modelSkinController, int meshRendererIndex, MeshReplacementSkillVariant meshReplacementSkillVariant)
    {
        SkinSkillVariants.SetHooks();
        ref SkinDefParams.MeshReplacement meshReplacement = ref GetSkinDefParams(modelSkinController.skins[0]).meshReplacements[meshRendererIndex];
        HandleAddition(modelSkinController, meshReplacementSkillVariant, ref meshReplacement);
    }
    public static void AddSkillVariant(this ref SkinDefParams.MeshReplacement meshReplacement, MeshReplacementSkillVariant meshReplacementSkillVariant)
    {
        SkinSkillVariants.SetHooks();
        ref object[] objects = ref meshReplacement.GetSkillVariants();
        AddItemToObjects(ref objects, meshReplacementSkillVariant);
    }
    public static void AddLightInfoSkillVariant(this SkinDef skinDef, string lightName, LightInfoSkillVariant lightInfoSkillVariant) => GetSkinDefParams(skinDef).AddLightInfoSkillVariant(lightName, lightInfoSkillVariant);
    public static void AddLightInfoSkillVariant(this SkinDefParams skinDefParams, string lightName, LightInfoSkillVariant lightInfoSkillVariant)
    {
        SkinSkillVariants.SetHooks();
        ref CharacterModel.LightInfo lightInfo = ref GetLightInfoByName(skinDefParams, lightName);
        HandleAddition(skinDefParams, lightInfoSkillVariant, ref lightInfo);
    }
    public static void AddLightInfoSkillVariant(this ModelSkinController modelSkinController, string lightName, LightInfoSkillVariant lightInfoSkillVariant)
    {
        SkinSkillVariants.SetHooks();
        ref CharacterModel.LightInfo lightInfo = ref GetLightInfoByName(GetSkinDefParams(modelSkinController.skins[0]), lightName);
        HandleAddition(modelSkinController, lightInfoSkillVariant, ref lightInfo);
    }
    public static void AddLightInfoSkillVariant(this SkinDef skinDef, int lightIndex, LightInfoSkillVariant lightInfoSkillVariant) => GetSkinDefParams(skinDef).AddLightInfoSkillVariant(lightIndex, lightInfoSkillVariant);
    public static void AddLightInfoSkillVariant(this SkinDefParams skinDefParams, int lightIndex, LightInfoSkillVariant lightInfoSkillVariant)
    {
        SkinSkillVariants.SetHooks();
        ref CharacterModel.LightInfo lightInfo = ref skinDefParams.lightReplacements[lightIndex];
        HandleAddition(skinDefParams, lightInfoSkillVariant, ref lightInfo);
    }
    public static void AddLightInfoSkillVariant(this ModelSkinController modelSkinController, int lightIndex, LightInfoSkillVariant lightInfoSkillVariant)
    {
        SkinSkillVariants.SetHooks();
        ref CharacterModel.LightInfo lightInfo = ref GetSkinDefParams(modelSkinController.skins[0]).lightReplacements[lightIndex];
        HandleAddition(modelSkinController, lightInfoSkillVariant, ref lightInfo);
    }
    public static void AddSkillVariant(this ref CharacterModel.LightInfo lightInfo, LightInfoSkillVariant lightInfoSkillVariant)
    {
        SkinSkillVariants.SetHooks();
        ref object[] objects = ref lightInfo.GetSkillVariants();
        AddItemToObjects(ref objects, lightInfoSkillVariant);
    }
    public static void AddProjectileGhostReplacementSkillVariant(this SkinDef skinDef, string projectileName, ProjectileGhostReplacementSkillVariant projectileGhostReplacementSkillVariant) => GetSkinDefParams(skinDef).AddProjectileGhostReplacementSkillVariant(projectileName, projectileGhostReplacementSkillVariant);
    public static void AddProjectileGhostReplacementSkillVariant(this SkinDefParams skinDefParams, string projectileName, ProjectileGhostReplacementSkillVariant projectileGhostReplacementSkillVariant)
    {
        SkinSkillVariants.SetHooks();
        ref SkinDefParams.ProjectileGhostReplacement projectileGhostReplacement = ref GetProjectileGhostReplacementByName(skinDefParams, projectileName);
        HandleAddition(skinDefParams, projectileGhostReplacementSkillVariant, ref projectileGhostReplacement);
    }
    public static void AddProjectileGhostReplacementSkillVariant(this ModelSkinController modelSkinController, string projectileName, ProjectileGhostReplacementSkillVariant projectileGhostReplacementSkillVariant)
    {
        SkinSkillVariants.SetHooks();
        ref SkinDefParams.ProjectileGhostReplacement projectileGhostReplacement = ref GetProjectileGhostReplacementByName(GetSkinDefParams(modelSkinController.skins[0]), projectileName);
        HandleAddition(modelSkinController, projectileGhostReplacementSkillVariant, ref projectileGhostReplacement);
    }
    public static void AddProjectileGhostReplacementSkillVariant(this SkinDef skinDef, int index, ProjectileGhostReplacementSkillVariant projectileGhostReplacementSkillVariant) => GetSkinDefParams(skinDef).AddProjectileGhostReplacementSkillVariant(index, projectileGhostReplacementSkillVariant);
    public static void AddProjectileGhostReplacementSkillVariant(this SkinDefParams skinDefParams, int index, ProjectileGhostReplacementSkillVariant projectileGhostReplacementSkillVariant)
    {
        SkinSkillVariants.SetHooks();
        ref SkinDefParams.ProjectileGhostReplacement projectileGhostReplacement = ref skinDefParams.projectileGhostReplacements[index];
        HandleAddition(skinDefParams, projectileGhostReplacementSkillVariant, ref projectileGhostReplacement);
    }
    public static void AddProjectileGhostReplacementSkillVariant(this ModelSkinController modelSkinController, int index, ProjectileGhostReplacementSkillVariant projectileGhostReplacementSkillVariant)
    {
        SkinSkillVariants.SetHooks();
        ref SkinDefParams.ProjectileGhostReplacement projectileGhostReplacement = ref GetSkinDefParams(modelSkinController.skins[0]).projectileGhostReplacements[index];
        HandleAddition(modelSkinController, projectileGhostReplacementSkillVariant, ref projectileGhostReplacement);
    }
    public static void AddSkillVariant(this ref SkinDefParams.ProjectileGhostReplacement projectileGhostReplacement, ProjectileGhostReplacementSkillVariant projectileGhostReplacementSkillVariant)
    {
        SkinSkillVariants.SetHooks();
        ref object[] objects = ref projectileGhostReplacement.GetSkillVariants();
        AddItemToObjects(ref objects, projectileGhostReplacementSkillVariant);
    }
    public static void AddMinionSkinReplacementSkillVariant(this SkinDef skinDef, string minionBodyName, MinionSkinReplacementSkillVariant minionSkinReplacementSkillVariant) => GetSkinDefParams(skinDef).AddMinionSkinReplacementSkillVariant(minionBodyName, minionSkinReplacementSkillVariant);
    public static void AddMinionSkinReplacementSkillVariant(this SkinDefParams skinDefParams, string minionBodyName, MinionSkinReplacementSkillVariant minionSkinReplacementSkillVariant)
    {
        SkinSkillVariants.SetHooks();
        ref SkinDefParams.MinionSkinReplacement minionSkinReplacement = ref GetMinionSkinReplacementByName(skinDefParams, minionBodyName);
        HandleAddition(skinDefParams, minionSkinReplacementSkillVariant, ref minionSkinReplacement);
    }
    public static void AddMinionSkinReplacementSkillVariant(this ModelSkinController modelSkinController, string minionBodyName, MinionSkinReplacementSkillVariant minionSkinReplacementSkillVariant)
    {
        SkinSkillVariants.SetHooks();
        ref SkinDefParams.MinionSkinReplacement minionSkinReplacement = ref GetMinionSkinReplacementByName(GetSkinDefParams(modelSkinController.skins[0]), minionBodyName);
        HandleAddition(modelSkinController, minionSkinReplacementSkillVariant, ref minionSkinReplacement);
    }
    public static void AddMinionSkinReplacementSkillVariant(this SkinDef skinDef, int index, MinionSkinReplacementSkillVariant minionSkinReplacementSkillVariant) => GetSkinDefParams(skinDef).AddMinionSkinReplacementSkillVariant(index, minionSkinReplacementSkillVariant);
    public static void AddMinionSkinReplacementSkillVariant(this SkinDefParams skinDefParams, int index, MinionSkinReplacementSkillVariant minionSkinReplacementSkillVariant)
    {
        SkinSkillVariants.SetHooks();
        ref SkinDefParams.MinionSkinReplacement minionSkinReplacement = ref skinDefParams.minionSkinReplacements[index];
        HandleAddition(skinDefParams, minionSkinReplacementSkillVariant, ref minionSkinReplacement);
    }
    public static void AddMinionSkinReplacementSkillVariant(this ModelSkinController modelSkinController, int index, MinionSkinReplacementSkillVariant minionSkinReplacementSkillVariant)
    {
        SkinSkillVariants.SetHooks();
        ref SkinDefParams.MinionSkinReplacement minionSkinReplacement = ref GetSkinDefParams(modelSkinController.skins[0]).minionSkinReplacements[index];
        HandleAddition(modelSkinController, minionSkinReplacementSkillVariant, ref minionSkinReplacement);
    }
    public static void AddSkillVariant(this ref SkinDefParams.MinionSkinReplacement minionSkinReplacement, MinionSkinReplacementSkillVariant minionSkinReplacementSkillVariant)
    {
        SkinSkillVariants.SetHooks();
        ref object[] objects = ref minionSkinReplacement.GetSkillVariants();
        AddItemToObjects(ref objects, minionSkinReplacementSkillVariant);
    }
    public static ref object[] GetSkillVariants(this ref CharacterModel.RendererInfo rendererInfo) => ref SkinAPIInterop.GetSkillVariants(ref rendererInfo);
    public static void SetSkillVariants(this ref CharacterModel.RendererInfo rendererInfo, object[] value) => SkinAPIInterop.SetSkillVariants(ref rendererInfo, value);
    public static ref object[] GetSkillVariants(this ref SkinDefParams.MeshReplacement meshReplacement) => ref SkinAPIInterop.GetSkillVariants(ref meshReplacement);
    public static void SetSkillVariants(this ref SkinDefParams.MeshReplacement meshReplacement, object[] value) => SkinAPIInterop.SetSkillVariants(ref meshReplacement, value);
    public static ref object[] GetSkillVariants(this ref CharacterModel.LightInfo lightInfo) => ref SkinAPIInterop.GetSkillVariants(ref lightInfo);
    public static void SetSkillVariants(this ref CharacterModel.LightInfo lightInfo, object[] value) => SkinAPIInterop.SetSkillVariants(ref lightInfo, value);
    public static ref object[] GetSkillVariants(this ref SkinDefParams.ProjectileGhostReplacement projectileGhostReplacement) => ref SkinAPIInterop.GetSkillVariants(ref projectileGhostReplacement);
    public static void SetSkillVariants(this ref SkinDefParams.ProjectileGhostReplacement projectileGhostReplacement, object[] value) => SkinAPIInterop.SetSkillVariants(ref projectileGhostReplacement, value);
    public static ref object[] GetSkillVariants(this ref SkinDefParams.MinionSkinReplacement minionSkinReplacement) => ref SkinAPIInterop.GetSkillVariants(ref minionSkinReplacement);
    public static void SetSkillVariants(this ref SkinDefParams.MinionSkinReplacement minionSkinReplacement, object[] value) => SkinAPIInterop.SetSkillVariants(ref minionSkinReplacement, value);
    public static ref object[] GetSkillVariants(this ref SkinDef.MeshReplacementTemplate meshReplacementTemplate) => ref SkinAPIInterop.GetSkillVariants(ref meshReplacementTemplate);
    public static void SetSkillVariants(this ref SkinDef.MeshReplacementTemplate meshReplacementTemplate, object[] value) => SkinAPIInterop.SetSkillVariants(ref meshReplacementTemplate, value);
    public static ref object[] GetSkillVariants(this ref SkinDef.LightReplacementTemplate lightReplacementTemplate) => ref SkinAPIInterop.GetSkillVariants(ref lightReplacementTemplate);
    public static void SetSkillVariants(this ref SkinDef.LightReplacementTemplate lightReplacementTemplate, object[] value) => SkinAPIInterop.SetSkillVariants(ref lightReplacementTemplate, value);
    public static ref object[] GetSkillVariants(this ref SkinDef.GhostReplacementTemplate ghostReplacementTemplate) => ref SkinAPIInterop.GetSkillVariants(ref ghostReplacementTemplate);
    public static void SetSkillVariants(this ref SkinDef.GhostReplacementTemplate ghostReplacementTemplate, object[] value) => SkinAPIInterop.SetSkillVariants(ref ghostReplacementTemplate, value);
    public static ref object[] GetSkillVariants(this ref SkinDef.MinionSkinTemplate minionSkinTemplate) => ref SkinAPIInterop.GetSkillVariants(ref minionSkinTemplate);
    public static void SetSkillVariants(this ref SkinDef.MinionSkinTemplate minionSkinTemplate, object[] value) => SkinAPIInterop.SetSkillVariants(ref minionSkinTemplate, value);
    public static SkinDef GetSkinDef(this SkinDef.RuntimeSkin runtimeSkin) => SkinAPIInterop.GetSkinDef(runtimeSkin);
    public static void SetSkinDef(this SkinDef.RuntimeSkin runtimeSkin, SkinDef skinDef) => SkinAPIInterop.SetSkinDef(runtimeSkin, skinDef);
    public static OnSkinApplied GetOnSkinApplied(this SkinDef skinDef) => SkinAPIInterop.GetOnSkinApplied(skinDef) == null ? null : SkinAPIInterop.GetOnSkinApplied(skinDef) as OnSkinApplied;
    public static void SetOnSkinApplied(this SkinDef skinDef, OnSkinApplied onSkinApplied) => SkinAPIInterop.SetOnSkinApplied(skinDef, onSkinApplied);
    #endregion
}
[CreateAssetMenu(menuName = "R2API/SkinsAPI/SkinSkillVariantsDef")]
public class SkinSkillVariantsDef : ScriptableObject
{
    internal static List<SkinSkillVariantsDef> pendingSkinSkillVariantsDefs = [];
    public SkinDefParams[] skinDefParameters = [];
    [PrefabReference] public ModelSkinController modelSkinController;
    public RendererInfoSkillVariant[] rendererInfoSkillVariants = [];
    public MeshReplacementSkillVariant[] meshReplacementSkillVariants = [];
    public LightInfoSkillVariant[] lightInfoSkillVariants = [];
    public ProjectileGhostReplacementSkillVariant[] projectileGhostReplacementSkillVariants = [];
    public MinionSkinReplacementSkillVariant[] minionSkinReplacementSkillVariants = [];
    private bool registered;
    private bool applied;
    public void AddSkinDefParams(SkinDefParams skinDefParams) => Add(ref skinDefParameters, skinDefParams);
    public void AddRendererInfoSkillVariant(RendererInfoSkillVariant rendererInfoSkillVariant) => Add(ref rendererInfoSkillVariants, rendererInfoSkillVariant);
    public void AddMeshReplacementSkillVariant(MeshReplacementSkillVariant meshReplacementSkillVariant) => Add(ref meshReplacementSkillVariants, meshReplacementSkillVariant);
    public void AddLightInfoSkillVariant(LightInfoSkillVariant lightInfoSkillVariant) => Add(ref lightInfoSkillVariants, lightInfoSkillVariant);
    public void AddProjectileGhostReplacementSkillVariant(ProjectileGhostReplacementSkillVariant projectileGhostReplacementSkillVariant) => Add(ref projectileGhostReplacementSkillVariants, projectileGhostReplacementSkillVariant);
    public void AddMinionSkinReplacementSkillVariant(MinionSkinReplacementSkillVariant minionSkinReplacementSkillVariant) => Add(ref minionSkinReplacementSkillVariants, minionSkinReplacementSkillVariant);
    private void Add<T>(ref T[] values, T value)
    {
        int count = values.Length;
        Array.Resize(ref values, count + 1);
        values[count] = value;
    }
    public void Register()
    {
        if (registered) return;
        pendingSkinSkillVariantsDefs.Add(this);
        registered = true;
    }
    internal void Apply()
    {
        IEnumerator enumerator = ApplyAsync();
        while (enumerator.MoveNext())
        {
        }
    }
    internal IEnumerator ApplyAsync()
    {
        yield return null;
        if (applied) yield break;
        if (modelSkinController != null)
        {
            List<SkinDefParams> skinDefParams1 = [];
            foreach (SkinDef skinDef in modelSkinController.skins)
            {
                SkinDefParams skinDefParams = GetSkinDefParams(skinDef);
                if (skinDefParams && !skinDefParams1.Contains(skinDefParams)) skinDefParams1.Add(skinDefParams);
                SkinDefParams optimisedSkinDefParams = GetOptimizedSkinDefParams(skinDef);
                if (optimisedSkinDefParams && !skinDefParams1.Contains(skinDefParams)) skinDefParams1.Add(optimisedSkinDefParams);
            }
            HandleArray([.. skinDefParams1], true);
        }
        HandleArray(skinDefParameters, false);
        applied = true;
        yield break;
    }
    private void HandleArray(SkinDefParams[] skinDefParamsArray, bool lowPriority)
    {
        for (int i = 0; i < skinDefParamsArray.Length; i++)
        {
            SkinDefParams skinDefParams = skinDefParamsArray[i];
            if (skinDefParams == null) continue;
            CharacterModel.RendererInfo[] rendererInfos = skinDefParams.rendererInfos;
            PopulateValues(ref rendererInfos, rendererInfoSkillVariants, lowPriority);
            SkinDefParams.MeshReplacement[] meshReplacements = skinDefParams.meshReplacements;
            PopulateValues(ref meshReplacements, meshReplacementSkillVariants, lowPriority);
            CharacterModel.LightInfo[] lightInfos = skinDefParams.lightReplacements;
            PopulateValues(ref lightInfos, lightInfoSkillVariants, lowPriority);
            SkinDefParams.ProjectileGhostReplacement[] projectileGhostReplacements = skinDefParams.projectileGhostReplacements;
            PopulateValues(ref projectileGhostReplacements, projectileGhostReplacementSkillVariants, lowPriority);
            SkinDefParams.MinionSkinReplacement[] minionSkinReplacements = skinDefParams.minionSkinReplacements;
            PopulateValues(ref minionSkinReplacements, minionSkinReplacementSkillVariants, lowPriority);
        }
    }
    private void PopulateValues<T1, T2>(ref T1[] t1s, T2[] t2s, bool lowPririty) where T2 : ISkillVariantStruct<T1>
    {
        if (t1s != null && t1s.Length > 0 && t2s != null && t2s.Length > 0)
            foreach (T2 t2 in t2s)
            {
                if (t2.NullCheck()) continue;
                for (int i = 0; i < t1s.Length; i++)
                {
                    ref T1 t1 = ref t1s[i];
                    if (t2.Compare(t1))
                    {
                        t2.lowPriority = lowPririty;
                        t2.Add(ref t1);
                    }
                }
            }
    }
}
