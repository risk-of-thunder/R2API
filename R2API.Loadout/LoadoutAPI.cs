using System;
using System.Collections.Generic;
using System.Reflection;
using EntityStates;
using R2API.AutoVersionGen;
using R2API.ContentManagement;
using R2API.Utils;
using RoR2;
using RoR2.Skills;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using SkinDefInfoNew = R2API.SkinDefInfo;
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

namespace R2API;

[Obsolete(LoadoutAPI.ObsoleteMessage)]
#pragma warning disable CS0436 // Type conflicts with imported type
[AutoVersion]
#pragma warning restore CS0436 // Type conflicts with imported type
public static partial class LoadoutAPI
{
    public const string PluginGUID = R2API.PluginGUID + ".loadout";
    public const string PluginName = R2API.PluginName + ".Loadout";

    public const string ObsoleteMessage = "R2API 4.x.x has made LoadoutAPI Obsolete.\n" +
        "For adding Skills, SkillFamilies and EntityStates, utilize the \"R2API.ContentManagement\" submodule.\n" +
        "For adding Skins, utilize the \"R2API.Skins\" submodule.\n" +
        "This submodule will be removed on the next major R2API release";
    // ReSharper disable once ConvertToAutoProperty
#pragma warning disable CS0618 // Type or member is obsolete
    [Obsolete(R2APISubmoduleDependency.PropertyObsolete)]
#pragma warning restore CS0618 // Type or member is obsolete
    public static bool Loaded => true;
    #region Adding Skills

    [Obsolete($"AddSkill() is obsolete, please add your SkillTypes via R2API.ContentAddition.AddEntityState(Type, bool)")]
    public static bool AddSkill(Type? t)
    {
        if (!CatalogBlockers.GetAvailability<EntityState>())
        {
            LoadoutPlugin.Logger.LogError($"Too late ! Tried to add skill type {t} after the EntityStateCatalog has initialized!");
            return false;
        }
        if (t == null || !t.IsSubclassOf(typeof(EntityState)) || t.IsAbstract)
        {
            LoadoutPlugin.Logger.LogError("Invalid skill type.");
            return false;
        }

        R2APIContentManager.HandleEntityState(Assembly.GetCallingAssembly(), t);
        return true;
    }

    [Obsolete($"StateTypeOf<T> is obsolete, please add your SkillTypes via R2API.ContentAddition.AddEntityState<T>(bool)")]
    public static SerializableEntityStateType StateTypeOf<T>()
        where T : EntityState, new()
    {
        if (!CatalogBlockers.GetAvailability<EntityState>())
        {
            LoadoutPlugin.Logger.LogError($"Too late ! Tried to add skill type {typeof(T)} after the EntityStateCatalog has initialized!");
            return new SerializableEntityStateType();
        }
        R2APIContentManager.HandleEntityState(Assembly.GetCallingAssembly(), typeof(T));
        return new SerializableEntityStateType(typeof(T));
    }

    [Obsolete($"AddSkillDef is obsolete, please add your SkillDefs via R2API.ContentAddition.AddSkillDef(SkillDef)")]
    public static bool AddSkillDef(SkillDef? s)
    {
        if (!CatalogBlockers.GetAvailability<SkillDef>())
        {
            LoadoutPlugin.Logger.LogError($"Too late ! Tried to add skillDef {s.skillName} after the SkillCatalog has initialized!");
            return false;
        }
        if (!s)
        {
            LoadoutPlugin.Logger.LogError("Invalid SkillDef");
            return false;
        }
        R2APIContentManager.HandleContentAddition(Assembly.GetCallingAssembly(), s);
        return true;
    }

    [Obsolete($"AddSkillFamily is obsolete, please add your SkillFamilies via R2API.ContentAddition.AddSkillFamily(SkillFamily)")]
    public static bool AddSkillFamily(SkillFamily? sf)
    {
        if (!CatalogBlockers.GetAvailability<SkillFamily>())
        {
            LoadoutPlugin.Logger.LogError($"Too late ! Tried to add skillFamily after the SkillCatalog has initialized!");
        }
        if (!sf)
        {
            LoadoutPlugin.Logger.LogError("Invalid SkillFamily");
            return false;
        }
        R2APIContentManager.HandleContentAddition(Assembly.GetCallingAssembly(), sf);
        return true;
    }

    #endregion Adding Skills

    #region Adding Skins

    [Obsolete($"Create Skin Icons using R2API.Skins.CreateSkinIcon(Color, Color, Color, Color)")]
    public static Sprite CreateSkinIcon(Color top, Color right, Color bottom, Color left)
    {
        return Skins.CreateSkinIcon(top, right, bottom, left, new Color(0.6f, 0.6f, 0.6f));
    }

    [Obsolete($"Create Skin Icons using R2API.Skins.CreateSkinIcon(Color, Color, Color, Color, Color)")]
    public static Sprite CreateSkinIcon(Color top, Color right, Color bottom, Color left, Color line)
    {
        return Skins.CreateSkinIcon(top, right, bottom, left, new Color(0.6f, 0.6f, 0.6f));
    }

    [Obsolete($"Utilize the SkinDefInfo in the R2API namespace instead of this nested type.s")]
    public struct SkinDefInfo
    {
        public SkinDef?[]? BaseSkins;
        public Sprite? Icon;
        public string? NameToken;
        public UnlockableDef? UnlockableDef;
        public GameObject? RootObject;
        public CharacterModel.RendererInfo[]? RendererInfos;
        public SkinDef.MeshReplacement[]? MeshReplacements;
        public SkinDef.GameObjectActivation[]? GameObjectActivations;
        public SkinDef.ProjectileGhostReplacement[]? ProjectileGhostReplacements;
        public SkinDef.MinionSkinReplacement[]? MinionSkinReplacements;
        public string? Name;

        //For backwards compat
        public static implicit operator SkinDefInfoNew(SkinDefInfo orig)
        {
            return new SkinDefInfoNew
            {
                BaseSkins = orig.BaseSkins,
                Icon = orig.Icon,
                NameToken = orig.NameToken,
                UnlockableDef = orig.UnlockableDef,
                RootObject = orig.RootObject,
                RendererInfos = orig.RendererInfos,
                MeshReplacements = orig.MeshReplacements,
                GameObjectActivations = orig.GameObjectActivations,
                ProjectileGhostReplacements = orig.ProjectileGhostReplacements,
                MinionSkinReplacements = orig.MinionSkinReplacements,
                Name = orig.Name,
            };
        }
    }

    [Obsolete("Create SkinDefs using R2API.Skins.CreateNewSkinDef(SkinDefInfo)")]
    public static SkinDef CreateNewSkinDef(SkinDefInfo skin)
    {
        return Skins.CreateNewSkinDef(skin);
    }

    [Obsolete($"Add Skins to Characters using R2API.Skins.AddSkinToCharacter(GameObject, SkinDefInfo)")]
    public static bool AddSkinToCharacter(GameObject? bodyPrefab, SkinDefInfo skin)
    {
        return Skins.AddSkinToCharacter(bodyPrefab, skin);
    }

    [Obsolete($"Add Skins to Characters using R2API.Skins.AddSkinToCharacter(GameObject, SkinDef)")]
    public static bool AddSkinToCharacter(GameObject? bodyPrefab, SkinDef? skin)
    {
        return Skins.AddSkinToCharacter(bodyPrefab, skin);
    }
    #endregion Adding Skins
}
