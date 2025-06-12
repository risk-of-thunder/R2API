using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EntityStates;
using HG;
using R2API.AutoVersionGen;
using R2API.Utils;
using RoR2;
using RoR2.Skills;
using RoR2.UI.MainMenu;
using Unity.Jobs;
using UnityEngine;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

namespace R2API;
#pragma warning disable CS0436 // Type conflicts with imported type
[AutoVersion]
#pragma warning restore CS0436 // Type conflicts with imported type
public static partial class Skins
{
    public const string PluginGUID = R2API.PluginGUID + ".skins";
    public const string PluginName = R2API.PluginName + ".Skins";

    private static readonly HashSet<SkinDef> AddedSkins = new HashSet<SkinDef>();

    private static bool _hooksSet;

    internal static void SetHooks()
    {
        if (_hooksSet)
            return;

        _hooksSet = true;

        MainMenuController.OnMainMenuInitialised += OnLoad;
    }
    internal static void UnsetHooks()
    {
        _hooksSet = false;

        MainMenuController.OnMainMenuInitialised += OnLoad;
    }

    private static void OnLoad()
    {
        MainMenuController.OnMainMenuInitialised -= OnLoad;

        try
        {
            foreach (var survivor in SurvivorCatalog.survivorDefs)
            {
                var display = survivor?.displayPrefab;
                var body = survivor?.bodyPrefab;

                if (!(display && body))
                    continue;

                var bodySkins = body.GetComponentInChildren<ModelSkinController>();
                if (bodySkins?.skins?.Any() != true)
                    continue;

                var displayModel = display.GetComponentInChildren<CharacterModel>();
                if (!displayModel)
                {
                    SkinsPlugin.Logger.LogWarning($"Display prefab {display.name} is missing the CharacterModel component! Skipping...");
                    continue;
                }

                var displaySkins = displayModel.GetComponent<ModelSkinController>();
                if (!displaySkins)
                {
                    SkinsPlugin.Logger.LogWarning($"Display prefab {displayModel.name} is missing a ModelSkinController component! Adding...");
                    displaySkins = displayModel.gameObject.AddComponent<ModelSkinController>();
                }

                if (displaySkins.skins?.Length != bodySkins.skins.Length)
                {
                    SkinsPlugin.Logger.LogWarning($"Display prefab {displayModel.name} ModelSkinController.skins array is incorrect! Cloning from body prefab...");
                    displaySkins.skins = ArrayUtils.Clone(bodySkins.skins);
                }
            }
        }
        catch (System.Exception e)
        {
            SkinsPlugin.Logger.LogError(e);
        }
    }

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
        SetHooks();
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
        SetHooks();
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

    private static Sprite CreateDefaultSkinIcon() => CreateSkinIcon(Color.red, Color.green, Color.blue, Color.black);

    /// <summary>
    /// Creates a new SkinDef from a SkinDefInfo.
    /// </summary>
    /// <param name="skin"></param>
    /// <returns></returns>
    public static SkinDef CreateNewSkinDef(SkinDefParamsInfo skin)
    {
        SetHooks();
        var newSkin = ScriptableObject.CreateInstance<SkinDef>();

        newSkin.name = skin.Name;
        newSkin.nameToken = skin.NameToken;
        newSkin.icon = skin.Icon;
        newSkin.baseSkins = skin.BaseSkins ?? [];
        newSkin.unlockableDef = skin.UnlockableDef;
        newSkin.rootObject = skin.RootObject;
        newSkin.skinDefParams = skin.SkinDefParams;

        if (newSkin.skinDefParams == null)
        {
            newSkin.skinDefParams = ScriptableObject.CreateInstance<SkinDefParams>();
            newSkin.skinDefParams.rendererInfos = skin.RendererInfos ?? [];
            newSkin.skinDefParams.gameObjectActivations = skin.GameObjectActivations ?? [];
            newSkin.skinDefParams.meshReplacements = skin.MeshReplacements ?? [];
            newSkin.skinDefParams.projectileGhostReplacements = skin.ProjectileGhostReplacements ?? [];
            newSkin.skinDefParams.minionSkinReplacements = skin.MinionSkinReplacements ?? [];
        }

        AddedSkins.Add(newSkin);
        return newSkin;
    }

    [System.Obsolete]
    public static SkinDef CreateNewSkinDef(SkinDefInfo skin) => CreateNewSkinDef((SkinDefParamsInfo) skin);

    /// <summary>
    /// Adds a skin to the body prefab for a character.
    /// Will attempt to create a default skin if one is not present.
    /// Must be called during plugin Awake or OnEnable. If called afterwards the new skins must be added to bodycatalog manually.
    /// </summary>
    /// <param name="bodyPrefab">The body to add the skin to</param>
    /// <param name="skin">The SkinDefInfo for the skin to add</param>
    /// <returns>True if successful</returns>
    public static bool AddSkinToCharacter(GameObject? bodyPrefab, SkinDefParamsInfo skin)
    {
        SetHooks();
        var skinDef = CreateNewSkinDef(skin);
        return AddSkinToCharacter(bodyPrefab, skinDef);
    }

    [System.Obsolete]
    public static bool AddSkinToCharacter(GameObject? bodyPrefab, SkinDefInfo skin) => AddSkinToCharacter(bodyPrefab, (SkinDefParamsInfo)skin);

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
        SetHooks();
        if (bodyPrefab == null)
        {
            SkinsPlugin.Logger.LogError("Tried to add skin to null body prefab.");
            return false;
        }

        if (skin == null)
        {
            SkinsPlugin.Logger.LogError("Tried to add invalid skin.");
            return false;
        }
        AddedSkins.Add(skin);

        var modelLocator = bodyPrefab.GetComponent<ModelLocator>();
        if (modelLocator == null)
        {
            SkinsPlugin.Logger.LogError("Tried to add skin to invalid body prefab (No ModelLocator).");
            return false;
        }

        var model = modelLocator.modelTransform;
        if (model == null)
        {
            SkinsPlugin.Logger.LogError("Tried to add skin to body prefab with no modelTransform.");
            return false;
        }

        if (skin.rootObject != model.gameObject)
        {
            SkinsPlugin.Logger.LogError("Tried to add skin with improper root object set.");
            return false;
        }

        var modelSkins = model.GetComponent<ModelSkinController>();
        if (modelSkins == null)
        {
            SkinsPlugin.Logger.LogWarning(bodyPrefab.name + " does not have a modelSkinController.\nAdding a new one and attempting to populate the default skin.\nHighly recommended you set the controller up manually.");
            var charModel = model.GetComponent<CharacterModel>();
            if (charModel == null)
            {
                SkinsPlugin.Logger.LogError("Unable to locate CharacterModel, default skin creation aborted.");
                return false;
            }

            var skinnedRenderer = charModel.mainSkinnedMeshRenderer;
            if (skinnedRenderer == null)
            {
                SkinsPlugin.Logger.LogError("CharacterModel did not contain a main SkinnedMeshRenderer, default skin creation aborted.");
                return false;
            }

            var baseRenderInfos = charModel.baseRendererInfos;
            if (baseRenderInfos == null || baseRenderInfos.Length == 0)
            {
                SkinsPlugin.Logger.LogError("CharacterModel rendererInfos are invalid, default skin creation aborted.");
                return false;
            }

            modelSkins = model.gameObject.AddComponent<ModelSkinController>();

            var skinDefInfo = new SkinDefParamsInfo
            {
                Icon = CreateDefaultSkinIcon(),
                Name = "skin" + bodyPrefab.name + "Default",
                NameToken = bodyPrefab.name.ToUpper() + "_DEFAULT_SKIN_NAME",
                RootObject = model.gameObject,
                UnlockableDef = null,
                MeshReplacements =
                [
                    new SkinDefParams.MeshReplacement
                    {
                        renderer = skinnedRenderer,
                        mesh = skinnedRenderer.sharedMesh
                    }
                ],
                RendererInfos = charModel.baseRendererInfos,
                BaseSkins = [],
                GameObjectActivations = [],
                ProjectileGhostReplacements = [],
                MinionSkinReplacements = []
            };

            var defaultSkinDef = CreateNewSkinDef(skinDefInfo);

            modelSkins.skins =
            [
                defaultSkinDef
            ];
        }

        var skinsArray = modelSkins.skins;
        var index = skinsArray.Length;
        Array.Resize(ref skinsArray, index + 1);
        skinsArray[index] = skin;
        modelSkins.skins = skinsArray;
        return true;
    }
}
