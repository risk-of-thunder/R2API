using HarmonyLib;
using HG.Coroutines;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.Utils;
using R2API.SkinsAPI.Interop;
using R2API.Utils;
using RoR2;
using RoR2.ContentManagement;
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
using UnityEngine.AddressableAssets;
using UnityEngine.Rendering;
using UnityEngine.ResourceManagement.AsyncOperations;
using static R2API.SkinSkillVariants;
using static Rewired.InputMapper;
using static UnityEngine.GridBrushBase;

namespace R2API;
public static partial class SkinSkillVariants
{
    public static Dictionary<SkinDef, BodyIndex> skinToBody = [];
    private static Dictionary<SkinDef, SkinDef> lobbySkinDefToBodySkinDef = [];
    private static ParallelCoroutine parallelCoroutine;
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
        IL.RoR2.SurvivorCatalog.SetSurvivorDefs += SurvivorCatalog_SetSurvivorDefs;
        On.RoR2.SurvivorCatalog.SetSurvivorDefs += SurvivorCatalog_SetSurvivorDefs1;
        
    }

    private static void SurvivorCatalog_SetSurvivorDefs1(On.RoR2.SurvivorCatalog.orig_SetSurvivorDefs orig, SurvivorDef[] newSurvivorDefs)
    {
        orig(newSurvivorDefs);
        if (parallelCoroutine == null) return;
        IEnumerator runLoadCoroutine()
        {
            yield return parallelCoroutine;
        }
        RoR2Application.instance.StartCoroutine(runLoadCoroutine());
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
        IL.RoR2.SurvivorCatalog.SetSurvivorDefs -= SurvivorCatalog_SetSurvivorDefs;
    }
    private static void SurvivorCatalog_SetSurvivorDefs(ILContext il)
    {
        ILCursor c = new ILCursor(il);
        int locID = 4;
        if (
            c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld(typeof(SurvivorCatalog), nameof(SurvivorCatalog.survivorDefs)),
                x => x.MatchLdloc(out _),
                x => x.MatchLdelemRef(),
                x => x.MatchStloc(out locID)
            ))
        {
            c.Emit(OpCodes.Ldloc, locID);
            c.EmitDelegate(SetLobbySkinToBodySkin);
        }
        else
        {
            SkinsPlugin.Logger.LogError(il.Method.Name + " IL Hook failed!");
        }
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
                SkinsPlugin.Logger.LogError(il.Method.Name + " IL Hook 2 failed!");
            }
        }
        else
        {
            SkinsPlugin.Logger.LogError(il.Method.Name + " IL Hook 1 failed!");
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
            SkinsPlugin.Logger.LogError(il.Method.Name + " IL Hook failed!");
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
        [HarmonyPatch([typeof(GameObject), typeof(List<AssetReferenceT<Material>>), typeof(List<AssetReferenceT<Mesh>>), typeof(List<AssetReferenceT<GameObject>>), typeof(AsyncReferenceHandleUnloadType)])]
        [HarmonyILManipulator]
        private static void SkinDef_RuntimeSkin_ApplyAsync(MonoMod.Cil.ILContext il)
        {
            int newLocal = il.Body.Variables.Count;
            il.Body.Variables.Add(new(il.Import(typeof(List<SkillDef>))));
            il.Body.Variables.Add(new(il.Import(typeof(Mesh))));
            FieldReference fieldReference = null;
            FieldReference fieldReference2 = null;
            int locID = 0;
            Instruction lastInstruction = il.Instrs[il.Instrs.Count - 1];
            int i = 0;
            ILCursor c = new ILCursor(il);
            if (
                c.TryGotoNext(MoveType.After,
                    x => x.MatchLdfld(out fieldReference),
                    x => x.MatchCallvirt(typeof(GameObject).GetPropertyGetter(nameof(GameObject.transform)))
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
                    SkinsPlugin.Logger.LogError(il.Method.Name + " IL Hook 2 failed!");
                }
                if (
                c.TryGotoNext(MoveType.After,
                    x => x.MatchStloc(7)
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
                    SkinsPlugin.Logger.LogError(il.Method.Name + " IL Hook 3 failed!");
                }
                if (
                c.TryGotoNext(MoveType.After,
                    x => x.MatchStloc(19)
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
                    SkinsPlugin.Logger.LogError(il.Method.Name + " IL Hook 4 failed!");
                }
            }
            else
            {
                SkinsPlugin.Logger.LogError(il.Method.Name + " IL Hook 1 failed!");
            }
            if (
                c.TryGotoPrev(MoveType.After,
                    x => x.MatchLdfld(out fieldReference2),
                    x => x.MatchLdcI4(1),
                    x => x.MatchStfld<CharacterModel>(nameof(CharacterModel.forceUpdate))
                ))
            {
                if (
                c.TryGotoNext(MoveType.After,
                    x => x.MatchLdfld<CharacterModel>(nameof(CharacterModel.customGameObjectActivationTransforms)),
                    x => x.MatchLdloc(out locID)
                ))
                {
                    c.Emit(OpCodes.Ldarg_0);
                    c.Emit(OpCodes.Ldfld, fieldReference2);
                    c.Emit(OpCodes.Ldloc, locID);
                    c.EmitDelegate(HandleSkinCustomGameobjectComponent);
                }
                else
                {
                    SkinsPlugin.Logger.LogError(il.Method.Name + " IL Hook 6 failed!");
                }   
            }
            else
            {
                SkinsPlugin.Logger.LogError(il.Method.Name + " IL Hook 5 failed!");
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
                ))
            {
                c.Emit(OpCodes.Ldloc, 6);
                c.Emit(OpCodes.Ldloc, 2);
                c.EmitDelegate(AddSkinToBodyDictionary);
            }
            else
            {
                SkinsPlugin.Logger.LogError(il.Method.Name + " IL Hook 1 failed!");
            }
            if (
                c.TryGotoNext(MoveType.Before,
                    x => x.MatchLdarg(0),
                    x => x.MatchLdcI4(0),
                    x => x.MatchStfld(out _),
                    x => x.MatchBr(out _)
                ))
            {
                c.EmitDelegate(ApplyPendingSkinSkillVariations);
            }
            else
            {
                SkinsPlugin.Logger.LogError(il.Method.Name + " IL Hook 2 failed!");
            }
        }
        [HarmonyPatch(typeof(SkinDef), nameof(SkinDef.BakeAsync), MethodType.Enumerator)]
        [HarmonyILManipulator]
        private static void SkinDef_BakeAsync(MonoMod.Cil.ILContext il)
        {
            ILCursor c = new ILCursor(il);
            if (
                c.TryGotoNext(MoveType.After,
                    x => x.MatchStfld<SkinDef>(nameof(SkinDef._runtimeSkin))
                ))
            {
                c.Emit(OpCodes.Ldloc, 1);
                c.EmitDelegate(AddSkinDefToRuntimeSkin);
            }
            else
            {
                SkinsPlugin.Logger.LogError(il.Method.Name + " IL Hook failed!");
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
                    x => x.MatchBneUn(out _)
                ))
            {
                c.RemoveRange(7);
            }
            else
            {
                SkinsPlugin.Logger.LogError(il.Method.Name + " IL Hook failed!");
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
        /// <summary>
        /// Use this if you are creating <see cref="SkinSkillVariantsDef"/>.
        /// Otherwise use <see cref="AddRenderInfoSkillVariant(SkinDefParams, string, RendererInfoSkillVariant)"/>.
        /// </summary>
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
        /// <summary>
        /// Use this if you are creating <see cref="SkinSkillVariantsDef"/>.
        /// Otherwise use <see cref="AddMeshReplacementSkillVariant(SkinDefParams, string, MeshReplacementSkillVariant)"/>.
        /// </summary>
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
        /// <summary>
        /// Use this if you are creating <see cref="SkinSkillVariantsDef"/>.
        /// Otherwise use <see cref="AddLightInfoSkillVariant(SkinDefParams, string, LightInfoSkillVariant)"/>.
        /// </summary>
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
        /// <summary>
        /// Use this if you are creating <see cref="SkinSkillVariantsDef"/>.
        /// Otherwise use <see cref="AddProjectileGhostReplacementSkillVariant(SkinDefParams, GameObject, ProjectileGhostReplacementSkillVariant)"/>.
        /// </summary>
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
        /// <summary>
        /// Use this if you are creating <see cref="SkinSkillVariantsDef"/>.
        /// Otherwise use <see cref="AddMinionSkinReplacementSkillVariant(SkinDefParams, GameObject, MinionSkinReplacementSkillVariant)"/>.
        /// </summary>
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

    private static void HandleSkinCustomGameobjectComponent(CharacterModel characterModel, Transform transform)
    {
        SkinCustomGameobjectComponent skinCustomGameobjectComponent = transform.GetComponent<SkinCustomGameobjectComponent>();
        if (!skinCustomGameobjectComponent) return;
        skinCustomGameobjectComponent.characterModel = characterModel;
        if (characterModel.baseLightInfos != null && skinCustomGameobjectComponent.extraRendererInfos != null && skinCustomGameobjectComponent.extraLightInfos.Length > 0)
        {
            int LightInfosSizeBefore = characterModel.baseLightInfos.Length;
            Array.Resize(ref characterModel.baseLightInfos, LightInfosSizeBefore + skinCustomGameobjectComponent.extraLightInfos.Length);
            for (int i = 0; i < skinCustomGameobjectComponent.extraLightInfos.Length; i++) characterModel.baseLightInfos[i + LightInfosSizeBefore] = skinCustomGameobjectComponent.extraLightInfos[i];
        }
        if (characterModel.baseRendererInfos == null || skinCustomGameobjectComponent.extraRendererInfos == null || skinCustomGameobjectComponent.extraRendererInfos.Length == 0) return;
        int RendererInfosSizeBefore = characterModel.baseRendererInfos.Length;
        Array.Resize(ref characterModel.baseRendererInfos, RendererInfosSizeBefore + skinCustomGameobjectComponent.extraRendererInfos.Length);
        for (int i = 0; i < skinCustomGameobjectComponent.extraRendererInfos.Length; i++) characterModel.baseRendererInfos[i + RendererInfosSizeBefore] = skinCustomGameobjectComponent.extraRendererInfos[i];
    }
    private static void SetLobbySkinToBodySkin(SurvivorDef survivorDef)
    {
        if (!survivorDef) return;
        if (parallelCoroutine == null) parallelCoroutine = new ParallelCoroutine();
        parallelCoroutine.Add(SetLobbySkinToBodySkinAsync(survivorDef));
    }
    private static IEnumerator SetLobbySkinToBodySkinAsync(SurvivorDef survivorDef)
    {
        ModelLocator modelLocator = survivorDef.bodyPrefab ? survivorDef.bodyPrefab.GetComponent<ModelLocator>() : null;
        yield return null;
        if (!modelLocator) yield break;
        ModelSkinController bodyPrefabModelSkinController = modelLocator._modelTransform ? modelLocator._modelTransform.GetComponent<ModelSkinController>() : null;
        yield return null;
        if (!bodyPrefabModelSkinController) yield break;
        ModelSkinController displayPrefabModelSkinController = survivorDef.displayPrefab ? survivorDef.displayPrefab.GetComponentInChildren<ModelSkinController>() : null;
        yield return null;
        if (!displayPrefabModelSkinController) yield break;
        SkinDef[] bodyPrefabModelSkinControllerSkins = bodyPrefabModelSkinController.skins;
        SkinDef[] displayPrefabModelSkinControllerSkins = displayPrefabModelSkinController.skins;
        if (bodyPrefabModelSkinControllerSkins == null || displayPrefabModelSkinControllerSkins == null || bodyPrefabModelSkinControllerSkins.Length != displayPrefabModelSkinControllerSkins.Length) yield break;
        for (int i = 0; i < bodyPrefabModelSkinControllerSkins.Length; i++)
        {
            SkinDef bodySkinDef = bodyPrefabModelSkinControllerSkins[i];
            SkinDef lobbySkinDef = displayPrefabModelSkinControllerSkins[i];
            if (!bodySkinDef || !lobbySkinDef) continue;
            if (!lobbySkinDefToBodySkinDef.ContainsKey(lobbySkinDef)) lobbySkinDefToBodySkinDef.Add(lobbySkinDef, bodySkinDef);
        }
    }
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
        SkinDef skinDef = runtimeSkin.GetSkinDef();
        if (skinDef == null) return null;
        BodyIndex bodyIndex;
        if (skinToBody.ContainsKey(skinDef))
        {
            bodyIndex = skinToBody[skinDef];
        }
        else
        {
            bodyIndex = BodyIndex.None;
            if (lobbySkinDefToBodySkinDef.TryGetValue(skinDef, out SkinDef lobbySkinDef))
            {
                if (skinToBody.ContainsKey(lobbySkinDef))
                {
                    bodyIndex = skinToBody[lobbySkinDef];
                }
            }
        }
        if (bodyIndex == BodyIndex.None) return null;
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
    internal static SkinDefParams GetSkinDefParams(SkinDef skinDef)
    {
        SkinDefParams skinDefParams = skinDef.skinDefParams;
        if (skinDefParams == null)
        {
            if (skinDef.skinDefParamsAddress != null)
            {
                if (skinDef.skinDefParamsAddress.Asset)
                {
                    skinDefParams = skinDef.skinDefParamsAddress.Asset as SkinDefParams;
                }
                else
                {
                    skinDefParams = skinDef.skinDefParamsAddress.LoadAsset().WaitForCompletion();
                }
            }
        }
        return skinDefParams;
    }
    internal static SkinDefParams GetOptimizedSkinDefParams(SkinDef skinDef)
    {
        SkinDefParams skinDefParams = skinDef.skinDefParams;
        if (skinDefParams == null)
        {
            if (skinDef.optimizedSkinDefParamsAddress != null)
            {
                if (skinDef.optimizedSkinDefParamsAddress.Asset)
                {
                    skinDefParams = skinDef.optimizedSkinDefParamsAddress.Asset as SkinDefParams;
                }
                else
                {
                    skinDefParams = skinDef.optimizedSkinDefParamsAddress.LoadAsset().WaitForCompletion();
                }
            }
        }
        return skinDefParams;
    }
    private static ref CharacterModel.RendererInfo GetRendererInfoByName(SkinDefParams skinDefParams, string rendererName)
    {
        CharacterModel.RendererInfo[] rendererInfos = skinDefParams.rendererInfos;
        for (int i = 0; i < rendererInfos.Length; i++)
        {
            ref CharacterModel.RendererInfo rendererInfo = ref rendererInfos[i];
            if(rendererInfo.renderer == null) continue;
            if(rendererInfo.renderer.name == rendererName) return ref rendererInfo;
        }
        NullReferenceException nullReferenceException = new($"Couldn't find renderer \"{rendererName}\" for \"{skinDefParams}\"");
        throw nullReferenceException;
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
        NullReferenceException nullReferenceException = new($"Couldn't find renderer \"{meshRendererName}\" for \"{skinDefParams}\"");
        throw nullReferenceException;
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
        NullReferenceException nullReferenceException = new($"Couldn't find light \"{lightInfoName}\" for \"{skinDefParams}\"");
        throw nullReferenceException;
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
        NullReferenceException nullReferenceException = new($"Couldn't find projectile \"{projectileName}\" for \"{skinDefParams}\"");
        throw nullReferenceException;
    }
    private static ref SkinDefParams.ProjectileGhostReplacement GetProjectileGhostReplacementByPrefab(SkinDefParams skinDefParams, GameObject projectilePrefab)
    {
        SkinDefParams.ProjectileGhostReplacement[] projectileghostReplacements = skinDefParams.projectileGhostReplacements;
        for (int i = 0; i < projectileghostReplacements.Length; i++)
        {
            ref SkinDefParams.ProjectileGhostReplacement projectileGhostReplacement = ref projectileghostReplacements[i];
            if (projectileGhostReplacement.projectilePrefab == null) continue;
            if (projectileGhostReplacement.projectilePrefab == projectilePrefab) return ref projectileGhostReplacement;
        }
        NullReferenceException nullReferenceException = new($"Couldn't find projectile \"{projectilePrefab.name}\" for \"{skinDefParams}\"");
        throw nullReferenceException;
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
        NullReferenceException nullReferenceException = new($"Couldn't find minion body \"{minionBodyName}\" for \"{skinDefParams}\"");
        throw nullReferenceException;
    }
    private static ref SkinDefParams.MinionSkinReplacement GetMinionSkinReplacementByPrefab(SkinDefParams skinDefParams, GameObject minionBodyPrefab)
    {
        SkinDefParams.MinionSkinReplacement[] minionSkinReplacements = skinDefParams.minionSkinReplacements;
        for (int i = 0; i < minionSkinReplacements.Length; i++)
        {
            ref SkinDefParams.MinionSkinReplacement minionSkinReplacement = ref minionSkinReplacements[i];
            if (minionSkinReplacement.minionBodyPrefab == null) continue;
            if (minionSkinReplacement.minionBodyPrefab == minionBodyPrefab) return ref minionSkinReplacement;
        }
        NullReferenceException nullReferenceException = new($"Couldn't find minion body \"{minionBodyPrefab}\" for \"{skinDefParams}\"");
        throw nullReferenceException;
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
    private static ref object[] GetSkillVariants(this ref SkinDef.MeshReplacementTemplate meshReplacementTemplate) => ref SkinAPIInterop.GetSkillVariants(ref meshReplacementTemplate);
    private static void SetSkillVariants(this ref SkinDef.MeshReplacementTemplate meshReplacementTemplate, object[] value) => SkinAPIInterop.SetSkillVariants(ref meshReplacementTemplate, value);
    private static ref object[] GetSkillVariants(this ref SkinDef.LightReplacementTemplate lightReplacementTemplate) => ref SkinAPIInterop.GetSkillVariants(ref lightReplacementTemplate);
    private static void SetSkillVariants(this ref SkinDef.LightReplacementTemplate lightReplacementTemplate, object[] value) => SkinAPIInterop.SetSkillVariants(ref lightReplacementTemplate, value);
    private static ref object[] GetSkillVariants(this ref SkinDef.GhostReplacementTemplate ghostReplacementTemplate) => ref SkinAPIInterop.GetSkillVariants(ref ghostReplacementTemplate);
    private static void SetSkillVariants(this ref SkinDef.GhostReplacementTemplate ghostReplacementTemplate, object[] value) => SkinAPIInterop.SetSkillVariants(ref ghostReplacementTemplate, value);
    private static ref object[] GetSkillVariants(this ref SkinDef.MinionSkinTemplate minionSkinTemplate) => ref SkinAPIInterop.GetSkillVariants(ref minionSkinTemplate);
    private static void SetSkillVariants(this ref SkinDef.MinionSkinTemplate minionSkinTemplate, object[] value) => SkinAPIInterop.SetSkillVariants(ref minionSkinTemplate, value);
    private static SkinDef GetSkinDef(this SkinDef.RuntimeSkin runtimeSkin) => SkinAPIInterop.GetSkinDef(runtimeSkin);
    private static void SetSkinDef(this SkinDef.RuntimeSkin runtimeSkin, SkinDef skinDef) => SkinAPIInterop.SetSkinDef(runtimeSkin, skinDef);
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
    /// <param name="index">Index filter to find needed RendererInfo.</param>
    /// <param name="rendererInfoSkillVariant">Info struct.</param>
    public static void AddRenderInfoSkillVariant(this SkinDef skinDef, int index, RendererInfoSkillVariant rendererInfoSkillVariant) => GetSkinDefParams(skinDef).AddRenderInfoSkillVariant(index, rendererInfoSkillVariant);
    /// <summary>
    /// Finds the renderer by its index and adds RendererInfoSkillVariant to it.
    /// </summary>
    /// <param name="skinDefParams">SkinDefParams for RendererInfoSkillVariant to apply to.</param>
    /// <param name="index">Index filter to find needed RendererInfo.</param>
    /// <param name="rendererInfoSkillVariant">Info struct.</param>
    public static void AddRenderInfoSkillVariant(this SkinDefParams skinDefParams, int index, RendererInfoSkillVariant rendererInfoSkillVariant)
    {
        SkinSkillVariants.SetHooks();
        ref CharacterModel.RendererInfo rendererInfo = ref skinDefParams.rendererInfos[index];
        HandleAddition(skinDefParams, rendererInfoSkillVariant, ref rendererInfo);
    }
    /// <summary>
    /// Finds the renderer by its index and adds RendererInfoSkillVariant to it.
    /// RendererInfoSkillVariant added with ModelSkinController will apply to all skins and have lower priority.
    /// </summary>
    /// <param name="modelSkinController">ModelSkinController for RendererInfoSkillVariant to apply to.</param>
    /// <param name="index">Index filter to find needed RendererInfo.</param>
    /// <param name="rendererInfoSkillVariant">Info struct.</param>
    public static void AddRenderInfoSkillVariant(this ModelSkinController modelSkinController, int index, RendererInfoSkillVariant rendererInfoSkillVariant)
    {
        SkinSkillVariants.SetHooks();
        ref CharacterModel.RendererInfo rendererInfo = ref GetSkinDefParams(modelSkinController.skins[0]).rendererInfos[index];
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
    /// <summary>
    /// Finds the mesh replacement by its name and adds MeshReplacementSkillVariant to it.
    /// </summary>
    /// <param name="skinDef">SkinDef for MeshReplacementSkillVariant to apply to.</param>
    /// <param name="meshRendererName">Name filter to find needed MeshReplacement.</param>
    /// <param name="meshReplacementSkillVariant">Info struct.</param>
    public static void AddMeshReplacementSkillVariant(this SkinDef skinDef, string meshRendererName, MeshReplacementSkillVariant meshReplacementSkillVariant) => GetSkinDefParams(skinDef).AddMeshReplacementSkillVariant(meshRendererName, meshReplacementSkillVariant);
    /// <summary>
    /// Finds the mesh replacement by its name and adds MeshReplacementSkillVariant to it.
    /// </summary>
    /// <param name="skinDefParams">SkinDefParams for MeshReplacementSkillVariant to apply to.</param>
    /// <param name="meshRendererName">Name filter to find needed MeshReplacement.</param>
    /// <param name="meshReplacementSkillVariant">Info struct.</param>
    public static void AddMeshReplacementSkillVariant(this SkinDefParams skinDefParams, string meshRendererName, MeshReplacementSkillVariant meshReplacementSkillVariant)
    {
        SkinSkillVariants.SetHooks();
        ref SkinDefParams.MeshReplacement meshReplacement = ref GetMeshReplacementByName(skinDefParams, meshRendererName);
        HandleAddition(skinDefParams, meshReplacementSkillVariant, ref meshReplacement);
    }
    /// <summary>
    /// Finds the mesh replacement by its name and adds MeshReplacementSkillVariant to it.
    /// MeshReplacementSkillVariant added with ModelSkinController will apply to all skins and have lower priority.
    /// </summary>
    /// <param name="modelSkinController">ModelSkinController for MeshReplacementSkillVariant to apply to.</param>
    /// <param name="meshRendererName">Name filter to find needed MeshReplacement.</param>
    /// <param name="meshReplacementSkillVariant">Info struct.</param>
    public static void AddMeshReplacementSkillVariant(this ModelSkinController modelSkinController, string meshRendererName, MeshReplacementSkillVariant meshReplacementSkillVariant)
    {
        SkinSkillVariants.SetHooks();
        ref SkinDefParams.MeshReplacement meshReplacement = ref GetMeshReplacementByName(GetSkinDefParams(modelSkinController.skins[0]), meshRendererName);
        HandleAddition(modelSkinController, meshReplacementSkillVariant, ref meshReplacement);
    }
    /// <summary>
    /// Finds the mesh replacement by its index and adds MeshReplacementSkillVariant to it.
    /// </summary>
    /// <param name="skinDef">SkinDef for MeshReplacementSkillVariant to apply to.</param>
    /// <param name="index">Index filter to find needed MeshReplacement.</param>
    /// <param name="meshReplacementSkillVariant">Info struct.</param>
    public static void AddMeshReplacementSkillVariant(this SkinDef skinDef, int index, MeshReplacementSkillVariant meshReplacementSkillVariant) => GetSkinDefParams(skinDef).AddMeshReplacementSkillVariant(index, meshReplacementSkillVariant);
    /// <summary>
    /// Finds the mesh replacement by its index and adds MeshReplacementSkillVariant to it.
    /// </summary>
    /// <param name="skinDefParams">SkinDefParams for MeshReplacementSkillVariant to apply to.</param>
    /// <param name="index">Index filter to find needed MeshReplacement.</param>
    /// <param name="meshReplacementSkillVariant">Info struct.</param>
    public static void AddMeshReplacementSkillVariant(this SkinDefParams skinDefParams, int index, MeshReplacementSkillVariant meshReplacementSkillVariant)
    {
        SkinSkillVariants.SetHooks();
        ref SkinDefParams.MeshReplacement meshReplacement = ref skinDefParams.meshReplacements[index];
        HandleAddition(skinDefParams, meshReplacementSkillVariant, ref meshReplacement);
    }
    /// <summary>
    /// Finds the mesh replacement by its index and adds MeshReplacementSkillVariant to it.
    /// MeshReplacementSkillVariant added with ModelSkinController will apply to all skins and have lower priority.
    /// </summary>
    /// <param name="modelSkinController">ModelSkinController for MeshReplacementSkillVariant to apply to.</param>
    /// <param name="index">Index filter to find needed MeshReplacement.</param>
    /// <param name="meshReplacementSkillVariant">Info struct.</param>
    public static void AddMeshReplacementSkillVariant(this ModelSkinController modelSkinController, int index, MeshReplacementSkillVariant meshReplacementSkillVariant)
    {
        SkinSkillVariants.SetHooks();
        ref SkinDefParams.MeshReplacement meshReplacement = ref GetSkinDefParams(modelSkinController.skins[0]).meshReplacements[index];
        HandleAddition(modelSkinController, meshReplacementSkillVariant, ref meshReplacement);
    }
    /// <summary>
    /// Directly adds MeshReplacementSkillVariant to MeshReplacement.
    /// Please don't use it unless you know what you are doing.
    /// </summary>
    /// <param name="meshReplacement">The MeshReplacement itself.</param>
    /// <param name="meshReplacementSkillVariant">Info struct.</param>
    public static void AddSkillVariant(this ref SkinDefParams.MeshReplacement meshReplacement, MeshReplacementSkillVariant meshReplacementSkillVariant)
    {
        SkinSkillVariants.SetHooks();
        ref object[] objects = ref meshReplacement.GetSkillVariants();
        AddItemToObjects(ref objects, meshReplacementSkillVariant);
    }
    /// <summary>
    /// Finds the light by its name and adds LightInfoSkillVariant to it.
    /// </summary>
    /// <param name="skinDef">SkinDef for LightInfoSkillVariant to apply to.</param>
    /// <param name="lightName">Name filter to find needed LightInfo.</param>
    /// <param name="lightInfoSkillVariant">Info struct.</param>
    public static void AddLightInfoSkillVariant(this SkinDef skinDef, string lightName, LightInfoSkillVariant lightInfoSkillVariant) => GetSkinDefParams(skinDef).AddLightInfoSkillVariant(lightName, lightInfoSkillVariant);
    /// <summary>
    /// Finds the light by its name and adds LightInfoSkillVariant to it.
    /// </summary>
    /// <param name="skinDefParams">SkinDefParams for LightInfoSkillVariant to apply to.</param>
    /// <param name="lightName">Name filter to find needed LightInfo.</param>
    /// <param name="lightInfoSkillVariant">Info struct.</param>
    public static void AddLightInfoSkillVariant(this SkinDefParams skinDefParams, string lightName, LightInfoSkillVariant lightInfoSkillVariant)
    {
        SkinSkillVariants.SetHooks();
        ref CharacterModel.LightInfo lightInfo = ref GetLightInfoByName(skinDefParams, lightName);
        HandleAddition(skinDefParams, lightInfoSkillVariant, ref lightInfo);
    }
    /// <summary>
    /// Finds the light by its name and adds LightInfoSkillVariant to it.
    /// LightInfoSkillVariant added with ModelSkinController will apply to all skins and have lower priority.
    /// </summary>
    /// <param name="modelSkinController">ModelSkinController for LightInfoSkillVariant to apply to.</param>
    /// <param name="lightName">Name filter to find needed LightInfo.</param>
    /// <param name="lightInfoSkillVariant">Info struct.</param>
    public static void AddLightInfoSkillVariant(this ModelSkinController modelSkinController, string lightName, LightInfoSkillVariant lightInfoSkillVariant)
    {
        SkinSkillVariants.SetHooks();
        ref CharacterModel.LightInfo lightInfo = ref GetLightInfoByName(GetSkinDefParams(modelSkinController.skins[0]), lightName);
        HandleAddition(modelSkinController, lightInfoSkillVariant, ref lightInfo);
    }
    /// <summary>
    /// Finds the light by its index and adds LightInfoSkillVariant to it.
    /// </summary>
    /// <param name="skinDef">SkinDef for LightInfoSkillVariant to apply to.</param>
    /// <param name="index">Index filter to find needed LightInfo.</param>
    /// <param name="lightInfoSkillVariant">Info struct.</param>
    public static void AddLightInfoSkillVariant(this SkinDef skinDef, int index, LightInfoSkillVariant lightInfoSkillVariant) => GetSkinDefParams(skinDef).AddLightInfoSkillVariant(index, lightInfoSkillVariant);
    /// <summary>
    /// Finds the light by its index and adds LightInfoSkillVariant to it.
    /// </summary>
    /// <param name="skinDefParams">SkinDefParams for LightInfoSkillVariant to apply to.</param>
    /// <param name="index">Index filter to find needed LightInfo.</param>
    /// <param name="lightInfoSkillVariant">Info struct.</param>
    public static void AddLightInfoSkillVariant(this SkinDefParams skinDefParams, int index, LightInfoSkillVariant lightInfoSkillVariant)
    {
        SkinSkillVariants.SetHooks();
        ref CharacterModel.LightInfo lightInfo = ref skinDefParams.lightReplacements[index];
        HandleAddition(skinDefParams, lightInfoSkillVariant, ref lightInfo);
    }
    /// <summary>
    /// Finds the light by its index and adds LightInfoSkillVariant to it.
    /// LightInfoSkillVariant added with ModelSkinController will apply to all skins and have lower priority.
    /// </summary>
    /// <param name="modelSkinController">ModelSkinController for LightInfoSkillVariant to apply to.</param>
    /// <param name="index">Name filter to find needed LightInfo.</param>
    /// <param name="lightInfoSkillVariant">Info struct.</param>
    public static void AddLightInfoSkillVariant(this ModelSkinController modelSkinController, int index, LightInfoSkillVariant lightInfoSkillVariant)
    {
        SkinSkillVariants.SetHooks();
        ref CharacterModel.LightInfo lightInfo = ref GetSkinDefParams(modelSkinController.skins[0]).lightReplacements[index];
        HandleAddition(modelSkinController, lightInfoSkillVariant, ref lightInfo);
    }
    /// <summary>
    /// Directly adds LightInfoSkillVariant to LightInfo.
    /// Please don't use it unless you know what you are doing.
    /// </summary>
    /// <param name="lightInfo">The LightInfo itself.</param>
    /// <param name="lightInfoSkillVariant">Info struct.</param>
    public static void AddSkillVariant(this ref CharacterModel.LightInfo lightInfo, LightInfoSkillVariant lightInfoSkillVariant)
    {
        SkinSkillVariants.SetHooks();
        ref object[] objects = ref lightInfo.GetSkillVariants();
        AddItemToObjects(ref objects, lightInfoSkillVariant);
    }
    /// <summary>
    /// Finds the projectile ghost replacement by its name and adds ProjectileGhostReplacementSkillVariant to it.
    /// </summary>
    /// <param name="skinDef">SkinDef for ProjectileGhostReplacementSkillVariant to apply to.</param>
    /// <param name="projectileName">Name filter to find needed ProjectileGhostReplacement.</param>
    /// <param name="projectileGhostReplacementSkillVariant">Info struct.</param>
    public static void AddProjectileGhostReplacementSkillVariant(this SkinDef skinDef, string projectileName, ProjectileGhostReplacementSkillVariant projectileGhostReplacementSkillVariant) => GetSkinDefParams(skinDef).AddProjectileGhostReplacementSkillVariant(projectileName, projectileGhostReplacementSkillVariant);
    /// <summary>
    /// Finds the projectile ghost replacement by its name and adds ProjectileGhostReplacementSkillVariant to it.
    /// </summary>
    /// <param name="skinDefParams">SkinDefParams for ProjectileGhostReplacementSkillVariant to apply to.</param>
    /// <param name="projectileName">Name filter to find needed ProjectileGhostReplacement.</param>
    /// <param name="projectileGhostReplacementSkillVariant">Info struct.</param>
    public static void AddProjectileGhostReplacementSkillVariant(this SkinDefParams skinDefParams, string projectileName, ProjectileGhostReplacementSkillVariant projectileGhostReplacementSkillVariant)
    {
        SkinSkillVariants.SetHooks();
        ref SkinDefParams.ProjectileGhostReplacement projectileGhostReplacement = ref GetProjectileGhostReplacementByName(skinDefParams, projectileName);
        HandleAddition(skinDefParams, projectileGhostReplacementSkillVariant, ref projectileGhostReplacement);
    }
    /// <summary>
    /// Finds the projectile ghost replacement by its name and adds ProjectileGhostReplacementSkillVariant to it.
    /// ProjectileGhostReplacementSkillVariant added with ModelSkinController will apply to all skins and have lower priority.
    /// </summary>
    /// <param name="modelSkinController">ModelSkinController for ProjectileGhostReplacementSkillVariant to apply to.</param>
    /// <param name="projectileName">Name filter to find needed ProjectileGhostReplacement.</param>
    /// <param name="projectileGhostReplacementSkillVariant">Info struct.</param>
    public static void AddProjectileGhostReplacementSkillVariant(this ModelSkinController modelSkinController, string projectileName, ProjectileGhostReplacementSkillVariant projectileGhostReplacementSkillVariant)
    {
        SkinSkillVariants.SetHooks();
        ref SkinDefParams.ProjectileGhostReplacement projectileGhostReplacement = ref GetProjectileGhostReplacementByName(GetSkinDefParams(modelSkinController.skins[0]), projectileName);
        HandleAddition(modelSkinController, projectileGhostReplacementSkillVariant, ref projectileGhostReplacement);
    }
    /// <summary>
    /// Finds the projectile ghost replacement by its projectile prefab and adds ProjectileGhostReplacementSkillVariant to it.
    /// </summary>
    /// <param name="skinDef">SkinDef for ProjectileGhostReplacementSkillVariant to apply to.</param>
    /// <param name="projectilePrefab">Prefab filter to find needed ProjectileGhostReplacement.</param>
    /// <param name="projectileGhostReplacementSkillVariant">Info struct.</param>
    public static void AddProjectileGhostReplacementSkillVariant(this SkinDef skinDef, GameObject projectilePrefab, ProjectileGhostReplacementSkillVariant projectileGhostReplacementSkillVariant) => GetSkinDefParams(skinDef).AddProjectileGhostReplacementSkillVariant(projectilePrefab, projectileGhostReplacementSkillVariant);
    /// <summary>
    /// Finds the projectile ghost replacement by its projectile prefab and adds ProjectileGhostReplacementSkillVariant to it.
    /// </summary>
    /// <param name="skinDefParams">SkinDefParams for ProjectileGhostReplacementSkillVariant to apply to.</param>
    /// <param name="projectilePrefab">Prefab filter to find needed ProjectileGhostReplacement.</param>
    /// <param name="projectileGhostReplacementSkillVariant">Info struct.</param>
    public static void AddProjectileGhostReplacementSkillVariant(this SkinDefParams skinDefParams, GameObject projectilePrefab, ProjectileGhostReplacementSkillVariant projectileGhostReplacementSkillVariant)
    {
        SkinSkillVariants.SetHooks();
        ref SkinDefParams.ProjectileGhostReplacement projectileGhostReplacement = ref GetProjectileGhostReplacementByPrefab(skinDefParams, projectilePrefab);
        HandleAddition(skinDefParams, projectileGhostReplacementSkillVariant, ref projectileGhostReplacement);
    }
    /// <summary>
    /// Finds the projectile ghost replacement by its projectile prefab and adds ProjectileGhostReplacementSkillVariant to it.
    /// ProjectileGhostReplacementSkillVariant added with ModelSkinController will apply to all skins and have lower priority.
    /// </summary>
    /// <param name="modelSkinController">ModelSkinController for ProjectileGhostReplacementSkillVariant to apply to.</param>
    /// <param name="projectilePrefab">Prefab filter to find needed ProjectileGhostReplacement.</param>
    /// <param name="projectileGhostReplacementSkillVariant">Info struct.</param>
    public static void AddProjectileGhostReplacementSkillVariant(this ModelSkinController modelSkinController, GameObject projectilePrefab, ProjectileGhostReplacementSkillVariant projectileGhostReplacementSkillVariant)
    {
        SkinSkillVariants.SetHooks();
        ref SkinDefParams.ProjectileGhostReplacement projectileGhostReplacement = ref GetProjectileGhostReplacementByPrefab(GetSkinDefParams(modelSkinController.skins[0]), projectilePrefab);
        HandleAddition(modelSkinController, projectileGhostReplacementSkillVariant, ref projectileGhostReplacement);
    }
    /// <summary>
    /// Finds the projectile ghost replacement by its index and adds ProjectileGhostReplacementSkillVariant to it.
    /// </summary>
    /// <param name="skinDef">SkinDef for ProjectileGhostReplacementSkillVariant to apply to.</param>
    /// <param name="index">Index filter to find needed ProjectileGhostReplacement.</param>
    /// <param name="projectileGhostReplacementSkillVariant">Info struct.</param>
    public static void AddProjectileGhostReplacementSkillVariant(this SkinDef skinDef, int index, ProjectileGhostReplacementSkillVariant projectileGhostReplacementSkillVariant) => GetSkinDefParams(skinDef).AddProjectileGhostReplacementSkillVariant(index, projectileGhostReplacementSkillVariant);
    /// <summary>
    /// Finds the projectile ghost replacement by its index and adds ProjectileGhostReplacementSkillVariant to it.
    /// </summary>
    /// <param name="skinDefParams">SkinDefParams for ProjectileGhostReplacementSkillVariant to apply to.</param>
    /// <param name="index">Index filter to find needed ProjectileGhostReplacement.</param>
    /// <param name="projectileGhostReplacementSkillVariant">Info struct.</param>
    public static void AddProjectileGhostReplacementSkillVariant(this SkinDefParams skinDefParams, int index, ProjectileGhostReplacementSkillVariant projectileGhostReplacementSkillVariant)
    {
        SkinSkillVariants.SetHooks();
        ref SkinDefParams.ProjectileGhostReplacement projectileGhostReplacement = ref skinDefParams.projectileGhostReplacements[index];
        HandleAddition(skinDefParams, projectileGhostReplacementSkillVariant, ref projectileGhostReplacement);
    }
    /// <summary>
    /// Finds the projectile ghost replacement by its index and adds ProjectileGhostReplacementSkillVariant to it.
    /// ProjectileGhostReplacementSkillVariant added with ModelSkinController will apply to all skins and have lower priority.
    /// </summary>
    /// <param name="modelSkinController">ModelSkinController for ProjectileGhostReplacementSkillVariant to apply to.</param>
    /// <param name="index">Index filter to find needed ProjectileGhostReplacement.</param>
    /// <param name="projectileGhostReplacementSkillVariant">Info struct.</param>
    public static void AddProjectileGhostReplacementSkillVariant(this ModelSkinController modelSkinController, int index, ProjectileGhostReplacementSkillVariant projectileGhostReplacementSkillVariant)
    {
        SkinSkillVariants.SetHooks();
        ref SkinDefParams.ProjectileGhostReplacement projectileGhostReplacement = ref GetSkinDefParams(modelSkinController.skins[0]).projectileGhostReplacements[index];
        HandleAddition(modelSkinController, projectileGhostReplacementSkillVariant, ref projectileGhostReplacement);
    }
    /// <summary>
    /// Directly adds ProjectileGhostReplacementSkillVariant to ProjectileGhostReplacement.
    /// Please don't use it unless you know what you are doing.
    /// </summary>
    /// <param name="projectileGhostReplacement">The ProjectileGhostReplacement itself.</param>
    /// <param name="projectileGhostReplacementSkillVariant">Info struct.</param>
    public static void AddSkillVariant(this ref SkinDefParams.ProjectileGhostReplacement projectileGhostReplacement, ProjectileGhostReplacementSkillVariant projectileGhostReplacementSkillVariant)
    {
        SkinSkillVariants.SetHooks();
        ref object[] objects = ref projectileGhostReplacement.GetSkillVariants();
        AddItemToObjects(ref objects, projectileGhostReplacementSkillVariant);
    }
    /// <summary>
    /// Finds the minion skill replacement by its name and adds MinionSkinReplacementSkillVariant to it.
    /// </summary>
    /// <param name="skinDef">SkinDef for MinionSkinReplacementSkillVariant to apply to.</param>
    /// <param name="minionBodyName">Name filter to find needed MinionSkinReplacement.</param>
    /// <param name="minionSkinReplacementSkillVariant">Info struct.</param>
    public static void AddMinionSkinReplacementSkillVariant(this SkinDef skinDef, string minionBodyName, MinionSkinReplacementSkillVariant minionSkinReplacementSkillVariant) => GetSkinDefParams(skinDef).AddMinionSkinReplacementSkillVariant(minionBodyName, minionSkinReplacementSkillVariant);
    /// <summary>
    /// Finds the minion skill replacement by its name and adds MinionSkinReplacementSkillVariant to it.
    /// </summary>
    /// <param name="skinDefParams">SkinDefParams for MinionSkinReplacementSkillVariant to apply to.</param>
    /// <param name="minionBodyName">Name filter to find needed MinionSkinReplacement.</param>
    /// <param name="minionSkinReplacementSkillVariant">Info struct.</param>
    public static void AddMinionSkinReplacementSkillVariant(this SkinDefParams skinDefParams, string minionBodyName, MinionSkinReplacementSkillVariant minionSkinReplacementSkillVariant)
    {
        SkinSkillVariants.SetHooks();
        ref SkinDefParams.MinionSkinReplacement minionSkinReplacement = ref GetMinionSkinReplacementByName(skinDefParams, minionBodyName);
        HandleAddition(skinDefParams, minionSkinReplacementSkillVariant, ref minionSkinReplacement);
    }
    /// <summary>
    /// Finds the minion skill replacement by its name and adds MinionSkinReplacementSkillVariant to it.
    /// MinionSkinReplacementSkillVariant added with ModelSkinController will apply to all skins and have lower priority.
    /// </summary>
    /// <param name="modelSkinController">ModelSkinController for ProjectileGhostReplacementSkillVariant to apply to.</param>
    /// <param name="minionBodyName">Name filter to find needed MinionSkinReplacement.</param>
    /// <param name="minionSkinReplacementSkillVariant">Info struct.</param>
    public static void AddMinionSkinReplacementSkillVariant(this ModelSkinController modelSkinController, string minionBodyName, MinionSkinReplacementSkillVariant minionSkinReplacementSkillVariant)
    {
        SkinSkillVariants.SetHooks();
        ref SkinDefParams.MinionSkinReplacement minionSkinReplacement = ref GetMinionSkinReplacementByName(GetSkinDefParams(modelSkinController.skins[0]), minionBodyName);
        HandleAddition(modelSkinController, minionSkinReplacementSkillVariant, ref minionSkinReplacement);
    }
    /// <summary>
    /// Finds the minion skill replacement by its minion body prefab and adds MinionSkinReplacementSkillVariant to it.
    /// </summary>
    /// <param name="skinDef">SkinDef for MinionSkinReplacementSkillVariant to apply to.</param>
    /// <param name="minionBodyPrefab">Prefab filter to find needed MinionSkinReplacement.</param>
    /// <param name="minionSkinReplacementSkillVariant">Info struct.</param>
    public static void AddMinionSkinReplacementSkillVariant(this SkinDef skinDef, GameObject minionBodyPrefab, MinionSkinReplacementSkillVariant minionSkinReplacementSkillVariant) => GetSkinDefParams(skinDef).AddMinionSkinReplacementSkillVariant(minionBodyPrefab, minionSkinReplacementSkillVariant);
    /// <summary>
    /// Finds the minion skill replacement by its minion body prefab and adds MinionSkinReplacementSkillVariant to it.
    /// </summary>
    /// <param name="skinDefParams">SkinDefParams for MinionSkinReplacementSkillVariant to apply to.</param>
    /// <param name="minionBodyPrefab">Prefab filter to find needed MinionSkinReplacement.</param>
    /// <param name="minionSkinReplacementSkillVariant">Info struct.</param>
    public static void AddMinionSkinReplacementSkillVariant(this SkinDefParams skinDefParams, GameObject minionBodyPrefab, MinionSkinReplacementSkillVariant minionSkinReplacementSkillVariant)
    {
        SkinSkillVariants.SetHooks();
        ref SkinDefParams.MinionSkinReplacement minionSkinReplacement = ref GetMinionSkinReplacementByPrefab(skinDefParams, minionBodyPrefab);
        HandleAddition(skinDefParams, minionSkinReplacementSkillVariant, ref minionSkinReplacement);
    }
    /// <summary>
    /// Finds the minion skill replacement by its minion body prefab and adds MinionSkinReplacementSkillVariant to it.
    /// MinionSkinReplacementSkillVariant added with ModelSkinController will apply to all skins and have lower priority.
    /// </summary>
    /// <param name="modelSkinController">ModelSkinController for ProjectileGhostReplacementSkillVariant to apply to.</param>
    /// <param name="minionBodyPrefab">Prefab filter to find needed MinionSkinReplacement.</param>
    /// <param name="minionSkinReplacementSkillVariant">Info struct.</param>
    public static void AddMinionSkinReplacementSkillVariant(this ModelSkinController modelSkinController, GameObject minionBodyPrefab, MinionSkinReplacementSkillVariant minionSkinReplacementSkillVariant)
    {
        SkinSkillVariants.SetHooks();
        ref SkinDefParams.MinionSkinReplacement minionSkinReplacement = ref GetMinionSkinReplacementByPrefab(GetSkinDefParams(modelSkinController.skins[0]), minionBodyPrefab);
        HandleAddition(modelSkinController, minionSkinReplacementSkillVariant, ref minionSkinReplacement);
    }
    /// <summary>
    /// Finds the minion skill replacement by its index and adds MinionSkinReplacementSkillVariant to it.
    /// </summary>
    /// <param name="skinDef">SkinDef for MinionSkinReplacementSkillVariant to apply to.</param>
    /// <param name="index">Index filter to find needed MinionSkinReplacement.</param>
    /// <param name="minionSkinReplacementSkillVariant">Info struct.</param>
    public static void AddMinionSkinReplacementSkillVariant(this SkinDef skinDef, int index, MinionSkinReplacementSkillVariant minionSkinReplacementSkillVariant) => GetSkinDefParams(skinDef).AddMinionSkinReplacementSkillVariant(index, minionSkinReplacementSkillVariant);
    /// <summary>
    /// Finds the minion skill replacement by its index and adds MinionSkinReplacementSkillVariant to it.
    /// </summary>
    /// <param name="skinDefParams">SkinDefParams for MinionSkinReplacementSkillVariant to apply to.</param>
    /// <param name="index">Index filter to find needed MinionSkinReplacement.</param>
    /// <param name="minionSkinReplacementSkillVariant">Info struct.</param>
    public static void AddMinionSkinReplacementSkillVariant(this SkinDefParams skinDefParams, int index, MinionSkinReplacementSkillVariant minionSkinReplacementSkillVariant)
    {
        SkinSkillVariants.SetHooks();
        ref SkinDefParams.MinionSkinReplacement minionSkinReplacement = ref skinDefParams.minionSkinReplacements[index];
        HandleAddition(skinDefParams, minionSkinReplacementSkillVariant, ref minionSkinReplacement);
    }
    /// <summary>
    /// Finds the minion skill replacement by its index and adds MinionSkinReplacementSkillVariant to it.
    /// MinionSkinReplacementSkillVariant added with ModelSkinController will apply to all skins and have lower priority.
    /// </summary>
    /// <param name="modelSkinController">ModelSkinController for ProjectileGhostReplacementSkillVariant to apply to.</param>
    /// <param name="index">Index filter to find needed MinionSkinReplacement.</param>
    /// <param name="minionSkinReplacementSkillVariant">Info struct.</param>
    public static void AddMinionSkinReplacementSkillVariant(this ModelSkinController modelSkinController, int index, MinionSkinReplacementSkillVariant minionSkinReplacementSkillVariant)
    {
        SkinSkillVariants.SetHooks();
        ref SkinDefParams.MinionSkinReplacement minionSkinReplacement = ref GetSkinDefParams(modelSkinController.skins[0]).minionSkinReplacements[index];
        HandleAddition(modelSkinController, minionSkinReplacementSkillVariant, ref minionSkinReplacement);
    }
    /// <summary>
    /// Directly adds MinionSkinReplacementSkillVariant to MinionSkinReplacement.
    /// Please don't use it unless you know what you are doing.
    /// </summary>
    /// <param name="minionSkinReplacement">The MinionSkinReplacement itself.</param>
    /// <param name="minionSkinReplacementSkillVariant">Info struct.</param>
    public static void AddSkillVariant(this ref SkinDefParams.MinionSkinReplacement minionSkinReplacement, MinionSkinReplacementSkillVariant minionSkinReplacementSkillVariant)
    {
        SkinSkillVariants.SetHooks();
        ref object[] objects = ref minionSkinReplacement.GetSkillVariants();
        AddItemToObjects(ref objects, minionSkinReplacementSkillVariant);
    }
    public static ref object[] GetSkillVariants(this ref CharacterModel.RendererInfo rendererInfo) => ref SkinAPIInterop.GetSkillVariants(ref rendererInfo);
    /// <summary>
    /// Directly sets RendererInfoSkillVariant array to RendererInfo.
    /// Please don't use it unless you know what you are doing.
    /// </summary>
    public static void SetSkillVariants(this ref CharacterModel.RendererInfo rendererInfo, object[] value) => SkinAPIInterop.SetSkillVariants(ref rendererInfo, value);
    public static ref object[] GetSkillVariants(this ref SkinDefParams.MeshReplacement meshReplacement) => ref SkinAPIInterop.GetSkillVariants(ref meshReplacement);
    /// <summary>
    /// Directly sets MeshReplacementSkillVariant array to MeshReplacement.
    /// Please don't use it unless you know what you are doing.
    /// </summary>
    public static void SetSkillVariants(this ref SkinDefParams.MeshReplacement meshReplacement, object[] value) => SkinAPIInterop.SetSkillVariants(ref meshReplacement, value);
    public static ref object[] GetSkillVariants(this ref CharacterModel.LightInfo lightInfo) => ref SkinAPIInterop.GetSkillVariants(ref lightInfo);
    /// <summary>
    /// Directly sets LightInfoSkillVariant array to LightInfo.
    /// Please don't use it unless you know what you are doing.
    /// </summary>
    public static void SetSkillVariants(this ref CharacterModel.LightInfo lightInfo, object[] value) => SkinAPIInterop.SetSkillVariants(ref lightInfo, value);
    public static ref object[] GetSkillVariants(this ref SkinDefParams.ProjectileGhostReplacement projectileGhostReplacement) => ref SkinAPIInterop.GetSkillVariants(ref projectileGhostReplacement);
    /// <summary>
    /// Directly sets ProjectileGhostReplacementSkillVariant array to ProjectileGhostReplacement.
    /// Please don't use it unless you know what you are doing.
    /// </summary>
    public static void SetSkillVariants(this ref SkinDefParams.ProjectileGhostReplacement projectileGhostReplacement, object[] value) => SkinAPIInterop.SetSkillVariants(ref projectileGhostReplacement, value);
    public static ref object[] GetSkillVariants(this ref SkinDefParams.MinionSkinReplacement minionSkinReplacement) => ref SkinAPIInterop.GetSkillVariants(ref minionSkinReplacement);
    /// <summary>
    /// Directly sets MinionSkinReplacementSkillVariant array to MinionSkinReplacement.
    /// Please don't use it unless you know what you are doing.
    /// </summary>
    public static void SetSkillVariants(this ref SkinDefParams.MinionSkinReplacement minionSkinReplacement, object[] value) => SkinAPIInterop.SetSkillVariants(ref minionSkinReplacement, value);
    public static OnSkinApplied GetOnSkinApplied(this SkinDef skinDef) => SkinAPIInterop.GetOnSkinApplied(skinDef) == null ? null : SkinAPIInterop.GetOnSkinApplied(skinDef) as OnSkinApplied;
    public static void SetOnSkinApplied(this SkinDef skinDef, OnSkinApplied onSkinApplied) => SkinAPIInterop.SetOnSkinApplied(skinDef, onSkinApplied);
    #endregion
}
[CreateAssetMenu(menuName = "R2API/SkinsAPI/SkinSkillVariantsDef")]
public class SkinSkillVariantsDef : ScriptableObject
{
    internal static List<SkinSkillVariantsDef> pendingSkinSkillVariantsDefs = [];
    [Tooltip("SkinDefParams to apply SkillVariants")]
    public SkinDefParams[] skinDefParameters = [];
    [Tooltip("Put ModelSkinController from body prefab if you want this to be applied to all possible skins")]
    [PrefabReference]
    public ModelSkinController modelSkinController;
    public RendererInfoSkillVariant[] rendererInfoSkillVariants = [];
    public MeshReplacementSkillVariant[] meshReplacementSkillVariants = [];
    public LightInfoSkillVariant[] lightInfoSkillVariants = [];
    public ProjectileGhostReplacementSkillVariant[] projectileGhostReplacementSkillVariants = [];
    public MinionSkinReplacementSkillVariant[] minionSkinReplacementSkillVariants = [];
    public bool lowPriority;
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
            HandleArray([.. skinDefParams1]);
        }
        HandleArray(skinDefParameters);
        applied = true;
        yield break;
    }
    private void HandleArray(SkinDefParams[] skinDefParamsArray)
    {
        for (int i = 0; i < skinDefParamsArray.Length; i++)
        {
            SkinDefParams skinDefParams = skinDefParamsArray[i];
            if (skinDefParams == null) continue;
            CharacterModel.RendererInfo[] rendererInfos = skinDefParams.rendererInfos;
            PopulateValues(ref rendererInfos, rendererInfoSkillVariants);
            SkinDefParams.MeshReplacement[] meshReplacements = skinDefParams.meshReplacements;
            PopulateValues(ref meshReplacements, meshReplacementSkillVariants);
            CharacterModel.LightInfo[] lightInfos = skinDefParams.lightReplacements;
            PopulateValues(ref lightInfos, lightInfoSkillVariants);
            SkinDefParams.ProjectileGhostReplacement[] projectileGhostReplacements = skinDefParams.projectileGhostReplacements;
            PopulateValues(ref projectileGhostReplacements, projectileGhostReplacementSkillVariants);
            SkinDefParams.MinionSkinReplacement[] minionSkinReplacements = skinDefParams.minionSkinReplacements;
            PopulateValues(ref minionSkinReplacements, minionSkinReplacementSkillVariants);
        }
    }
    private void PopulateValues<T1, T2>(ref T1[] t1s, T2[] t2s) where T2 : ISkillVariantStruct<T1>
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
                        t2.lowPriority = lowPriority;
                        t2.Add(ref t1);
                    }
                }
            }
    }
}
