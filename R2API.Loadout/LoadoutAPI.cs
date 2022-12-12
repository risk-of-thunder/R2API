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

    public const string ObsoleteMessage = "The R2API version 4.x.x has made LoadoutAPI's Skill and entity state related methods and implementations obsolette.\n" +
        "For adding new SkillDefs, SkillFamilies and EntityStates, use the ContentAddition class found in the R2API.ContentManagement assembly.\n" +
        "The Skin methods of LoadoutAPI will be salvaged into an upcoming SkinAPI";
    /// <summary>
    /// Return true if the submodule is loaded.
    /// </summary>
    // ReSharper disable once ConvertToAutoProperty
#pragma warning disable CS0618 // Type or member is obsolete
    [Obsolete(R2APISubmoduleDependency.PropertyObsolete)]
#pragma warning restore CS0618 // Type or member is obsolete
    public static bool Loaded => true;

    private static readonly HashSet<SkinDef> AddedSkins = new HashSet<SkinDef>();

    #region Adding Skills

    /// <summary>
    /// Adds a type for a skill EntityState to the SkillsCatalog.
    /// State must derive from EntityStates.EntityState.
    /// Note that SkillDefs and SkillFamiles must also be added seperately.
    /// </summary>
    /// <param name="t">The type to add</param>
    /// <returns>True if succesfully added</returns>
    [Obsolete($"AddSkill is obsolete, please add your SkillTypes via R2API.ContentManagement.ContentAdditionHelpers.AddEntityState<T>()")]
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

    /// <summary>
    /// Creates a SerializableEntityStateType with a much simpler syntax
    /// Effectively the same as new SerializableEntityStateType(typeof(T))
    /// </summary>
    /// <typeparam name="T">The state type</typeparam>
    /// <returns>The created SerializableEntityStateType</returns>
    [Obsolete($"StateTypeOf<T> is obsolete, please add your SkillTypes via R2API.ContentManagement.ContentAdditionHelpers.AddEntityState<T>()")]
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

    /// <summary>
    /// Registers an event to add a SkillDef to the SkillDefCatalog.
    /// Must be called before Catalog init (during Awake() or OnEnable())
    /// </summary>
    /// <param name="s">The SkillDef to add</param>
    /// <returns>True if the event was registered</returns>
    [Obsolete($"AddSkillDef is obsolete, please add your SkillDefs via R2API.ContentManagement.ContentAdditionHelpers.AddSkillDef()")]
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

    /// <summary>
    /// Registers an event to add a SkillFamily to the SkillFamiliesCatalog
    /// Must be called before Catalog init (during Awake() or OnEnable())
    /// </summary>
    /// <param name="sf">The skillfamily to add</param>
    /// <returns>True if the event was registered</returns>
    [Obsolete($"AddSkillFamily is obsolete, please add your SkillFamilies via R2API.ContentManagement.ContentAdditionHelpers.AddSkillFamily()")]
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

    /// <summary>
    /// Creates a skin icon sprite styled after the ones already in the game.
    /// </summary>
    /// <param name="top">The color of the top portion</param>
    /// <param name="right">The color of the right portion</param>
    /// <param name="bottom">The color of the bottom portion</param>
    /// <param name="left">The color of the left portion</param>
    /// <returns>The icon sprite</returns>
    public static Sprite CreateSkinIcon(Color top, Color right, Color bottom, Color left)
    {
        return CreateSkinIcon(top, right, bottom, left, new Color(0.6f, 0.6f, 0.6f));
    }

    /// <summary>
    /// Creates a skin icon sprite styled after the ones already in the game.
    /// </summary>
    /// <param name="top">The color of the top portion</param>
    /// <param name="right">The color of the right portion</param>
    /// <param name="bottom">The color of the bottom portion</param>
    /// <param name="left">The color of the left portion</param>
    /// <param name="line">The color of the dividing lines</param>
    /// <returns></returns>
    public static Sprite CreateSkinIcon(Color top, Color right, Color bottom, Color left, Color line)
    {
        var tex = new Texture2D(128, 128, TextureFormat.RGBA32, false);
        new IconTexJob
        {
            Top = top,
            Bottom = bottom,
            Right = right,
            Left = left,
            Line = line,
            TexOutput = tex.GetRawTextureData<Color32>()
        }.Schedule(16384, 1).Complete();
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f));
    }

    private struct IconTexJob : IJobParallelFor
    {

        [ReadOnly]
        public Color32 Top;

        [ReadOnly]
        public Color32 Right;

        [ReadOnly]
        public Color32 Bottom;

        [ReadOnly]
        public Color32 Left;

        [ReadOnly]
        public Color32 Line;

        public NativeArray<Color32> TexOutput;

        public void Execute(int index)
        {
            int x = index % 128 - 64;
            int y = index / 128 - 64;

            if (Math.Abs(Math.Abs(y) - Math.Abs(x)) <= 2)
            {
                TexOutput[index] = Line;
                return;
            }
            if (y > x && y > -x)
            {
                TexOutput[index] = Top;
                return;
            }
            if (y < x && y < -x)
            {
                TexOutput[index] = Bottom;
                return;
            }
            if (y > x && y < -x)
            {
                TexOutput[index] = Left;
                return;
            }
            if (y < x && y > -x)
            {
                TexOutput[index] = Right;
            }
        }
    }

    /// <summary>
    /// A container struct for all SkinDef parameters.
    /// Use this to set skinDef values, then call CreateNewSkinDef().
    /// </summary>
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
    }

    /// <summary>
    /// Creates a new SkinDef from a SkinDefInfo.
    /// Note that this prevents null-refs by disabling SkinDef awake while the SkinDef is being created.
    /// The things that occur during awake are performed when first applied to a character instead.
    /// </summary>
    /// <param name="skin"></param>
    /// <returns></returns>
    public static SkinDef CreateNewSkinDef(SkinDefInfo skin)
    {
        On.RoR2.SkinDef.Awake += DoNothing;

        var newSkin = ScriptableObject.CreateInstance<SkinDef>();

        newSkin.baseSkins = skin.BaseSkins ?? Array.Empty<SkinDef>();
        newSkin.icon = skin.Icon;
        newSkin.unlockableDef = skin.UnlockableDef;
        newSkin.rootObject = skin.RootObject;
        newSkin.rendererInfos = skin.RendererInfos ?? Array.Empty<CharacterModel.RendererInfo>();
        newSkin.gameObjectActivations = skin.GameObjectActivations ?? Array.Empty<SkinDef.GameObjectActivation>();
        newSkin.meshReplacements = skin.MeshReplacements ?? Array.Empty<SkinDef.MeshReplacement>();
        newSkin.projectileGhostReplacements = skin.ProjectileGhostReplacements ?? Array.Empty<SkinDef.ProjectileGhostReplacement>();
        newSkin.minionSkinReplacements = skin.MinionSkinReplacements ?? Array.Empty<SkinDef.MinionSkinReplacement>();
        newSkin.nameToken = skin.NameToken;
        newSkin.name = skin.Name;

        On.RoR2.SkinDef.Awake -= DoNothing;

        AddedSkins.Add(newSkin);
        return newSkin;
    }

    /// <summary>
    /// Adds a skin to the body prefab for a character.
    /// Will attempt to create a default skin if one is not present.
    /// Must be called during plugin Awake or OnEnable. If called afterwards the new skins must be added to bodycatalog manually.
    /// </summary>
    /// <param name="bodyPrefab">The body to add the skin to</param>
    /// <param name="skin">The SkinDefInfo for the skin to add</param>
    /// <returns>True if successful</returns>
    public static bool AddSkinToCharacter(GameObject? bodyPrefab, SkinDefInfo skin)
    {
        var skinDef = CreateNewSkinDef(skin);
        return AddSkinToCharacter(bodyPrefab, skinDef);
    }

    /// <summary>
    /// Adds a skin to the body prefab for a character.
    /// Will attempt to create a default skin if one is not present.
    /// Must be called during plugin Awake or OnEnable. If called afterwards the new skins must be added to bodycatalog manually.
    /// </summary>
    /// <param name="bodyPrefab">The body to add the skin to</param>
    /// <param name="skin">The SkinDef to add</param>
    /// <returns>True if successful</returns>
    public static bool AddSkinToCharacter(GameObject? bodyPrefab, SkinDef? skin)
    {
        if (bodyPrefab == null)
        {
            LoadoutPlugin.Logger.LogError("Tried to add skin to null body prefab.");
            return false;
        }

        if (skin == null)
        {
            LoadoutPlugin.Logger.LogError("Tried to add invalid skin.");
            return false;
        }
        AddedSkins.Add(skin);

        var modelLocator = bodyPrefab.GetComponent<ModelLocator>();
        if (modelLocator == null)
        {
            LoadoutPlugin.Logger.LogError("Tried to add skin to invalid body prefab (No ModelLocator).");
            return false;
        }

        var model = modelLocator.modelTransform;
        if (model == null)
        {
            LoadoutPlugin.Logger.LogError("Tried to add skin to body prefab with no modelTransform.");
            return false;
        }

        if (skin.rootObject != model.gameObject)
        {
            LoadoutPlugin.Logger.LogError("Tried to add skin with improper root object set.");
            return false;
        }

        var modelSkins = model.GetComponent<ModelSkinController>();
        if (modelSkins == null)
        {
            LoadoutPlugin.Logger.LogWarning(bodyPrefab.name + " does not have a modelSkinController.\nAdding a new one and attempting to populate the default skin.\nHighly recommended you set the controller up manually.");
            var charModel = model.GetComponent<CharacterModel>();
            if (charModel == null)
            {
                LoadoutPlugin.Logger.LogError("Unable to locate CharacterModel, default skin creation aborted.");
                return false;
            }

            var skinnedRenderer = charModel.mainSkinnedMeshRenderer;
            if (skinnedRenderer == null)
            {
                LoadoutPlugin.Logger.LogError("CharacterModel did not contain a main SkinnedMeshRenderer, default skin creation aborted.");
                return false;
            }

            var baseRenderInfos = charModel.baseRendererInfos;
            if (baseRenderInfos == null || baseRenderInfos.Length == 0)
            {
                LoadoutPlugin.Logger.LogError("CharacterModel rendererInfos are invalid, default skin creation aborted.");
                return false;
            }

            modelSkins = model.gameObject.AddComponent<ModelSkinController>();

            var skinDefInfo = new SkinDefInfo
            {
                BaseSkins = Array.Empty<SkinDef>(),
                GameObjectActivations = Array.Empty<SkinDef.GameObjectActivation>(),
                Icon = CreateDefaultSkinIcon(),
                Name = "skin" + bodyPrefab.name + "Default",
                NameToken = bodyPrefab.name.ToUpper() + "_DEFAULT_SKIN_NAME",
                RootObject = model.gameObject,
                UnlockableDef = null,
                MeshReplacements = new[]
                {
                    new SkinDef.MeshReplacement {
                        renderer = skinnedRenderer,
                        mesh = skinnedRenderer.sharedMesh
                    }
                },
                RendererInfos = charModel.baseRendererInfos,
                ProjectileGhostReplacements = Array.Empty<SkinDef.ProjectileGhostReplacement>(),
                MinionSkinReplacements = Array.Empty<SkinDef.MinionSkinReplacement>()
            };

            var defaultSkinDef = CreateNewSkinDef(skinDefInfo);

            modelSkins.skins = new[] {
                defaultSkinDef
            };
        }

        var skinsArray = modelSkins.skins;
        var index = skinsArray.Length;
        Array.Resize(ref skinsArray, index + 1);
        skinsArray[index] = skin;
        modelSkins.skins = skinsArray;
        return true;
    }

    private static Sprite CreateDefaultSkinIcon()
    {
        return CreateSkinIcon(Color.red, Color.green, Color.blue, Color.black);
    }

    private static void DoNothing(On.RoR2.SkinDef.orig_Awake orig, SkinDef self)
    {
        //Intentionally do nothing
    }

    #endregion Adding Skins
}
