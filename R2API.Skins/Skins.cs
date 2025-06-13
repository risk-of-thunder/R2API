using System;
using HG;
using R2API.AutoVersionGen;
using RoR2;
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

    #region Hooks
    private static bool _hooksSet;

    internal static void SetHooks()
    {
        if (_hooksSet)
            return;

        _hooksSet = true;

        MainMenuController.OnMainMenuInitialised += OnMainMenuInitialized;
    }

    internal static void UnsetHooks()
    {
        _hooksSet = false;

        MainMenuController.OnMainMenuInitialised += OnMainMenuInitialized;
    }

    // we want this to load REALLY late
    private static void OnMainMenuInitialized()
    {
        MainMenuController.OnMainMenuInitialised -= OnMainMenuInitialized;

        foreach (var survivor in SurvivorCatalog.survivorDefs)
        {
            if (!survivor)
                continue;

            var display = survivor.displayPrefab;
            var body = survivor.bodyPrefab;

            if (!(display && body))
            {
                SkinsPlugin.Logger.LogWarning($"SurvivorDef {survivor.cachedName} is missing a displayPrefab or bodyPrefab! You need to have this! Skipping...");
                continue;
            }

            var bodySkins = body.GetComponentInChildren<ModelSkinController>();
            if (!bodySkins || bodySkins.skins is null || bodySkins.skins.Length == 0)
            {
                SkinsPlugin.Logger.LogWarning($"BodyPrefab for {survivor.cachedName} is missing a ModelSkinController on the bodyPrefab! You need to have this! Skipping...");
                continue;
            }

            var displayModel = display.GetComponentInChildren<CharacterModel>();
            if (!displayModel)
            {
                SkinsPlugin.Logger.LogWarning($"Display prefab {display.name} is missing the CharacterModel component! You need to have this! Skipping...");
                continue;
            }

            var displaySkins = displayModel.GetComponent<ModelSkinController>();
            if (!displaySkins)
            {
                SkinsPlugin.Logger.LogWarning($"Display prefab {displayModel.name} is missing a ModelSkinController component!\r\nHighly recommended you set the controller up manually. Adding component...");
                displaySkins = displayModel.gameObject.AddComponent<ModelSkinController>();
            }

            displaySkins.skins ??= [];
            if (displaySkins.skins.Length != bodySkins.skins.Length)
            {
                SkinsPlugin.Logger.LogWarning($"ModelSkinController skins array on the displayPrefab {displayModel.name} array does not match the one on the bodyPrefab!" +
                    $"\r\nHighly recommended you set the controller up manually. Cloning from body prefab...");
                displaySkins.skins = ArrayUtils.Clone(bodySkins.skins);
            }
        }
    }
    #endregion

    #region Skin Creation
    /// <summary>
    /// Creates a new SkinDef from a SkinDefInfo.
    /// </summary>
    /// <param name="skin">Will be used to populate the SkinDef</param>
    /// <returns>The new SkinDef</returns>
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

        return newSkin;
    }

    /// <summary>
    /// Use <see cref="CreateNewSkinDef(SkinDefParamsInfo)"/> instead
    /// </summary>
    [Obsolete]
    public static SkinDef CreateNewSkinDef(SkinDefInfo skin)
    {
        SetHooks();

        // really dont like this but full compat is better here.
        var def = CreateNewSkinDef((SkinDefParamsInfo)skin);
        def.rendererInfos = skin.RendererInfos;
        def.gameObjectActivations = skin.GameObjectActivations;
        def.meshReplacements = skin.MeshReplacements;
        def.projectileGhostReplacements = skin.ProjectileGhostReplacements;
        def.minionSkinReplacements = skin.MinionSkinReplacements;

        return def;
    }

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

    /// <summary>
    /// Use <see cref="AddSkinToCharacter(GameObject?, SkinDefParamsInfo)"/> instead
    /// </summary>
    [Obsolete]
    public static bool AddSkinToCharacter(GameObject? bodyPrefab, SkinDefInfo skin)
    {
        SetHooks();
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

        if (string.IsNullOrEmpty(skin.name) || string.IsNullOrEmpty(skin.nameToken))
        {
            SkinsPlugin.Logger.LogError("Tried to add invalid skin. Please add a name and nameToken.");
            return false;
        }

        if (!bodyPrefab.TryGetComponent<ModelLocator>(out var modelLocator))
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

        if (!model.TryGetComponent<ModelSkinController>(out var modelSkins))
        {
            SkinsPlugin.Logger.LogWarning(bodyPrefab.name + " does not have a modelSkinController.\nAdding a new one and attempting to populate the default skin.\nHighly recommended you set the controller up manually.");
            if (!model.TryGetComponent<CharacterModel>(out var charModel))
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
            if (baseRenderInfos is null || baseRenderInfos.Length == 0)
            {
                SkinsPlugin.Logger.LogError("CharacterModel rendererInfos are invalid, default skin creation aborted.");
                return false;
            }

            modelSkins = model.gameObject.AddComponent<ModelSkinController>();
            var defaultSkinDef = CreateNewSkinDef(new SkinDefParamsInfo
            {
                Icon = CreateDefaultSkinIcon(),
                Name = "skin" + bodyPrefab.name + "Default",
                NameToken = bodyPrefab.name.ToUpper() + "_DEFAULT_SKIN_NAME",
                RootObject = model.gameObject,
                UnlockableDef = null,
                RendererInfos = ArrayUtils.Clone(charModel.baseRendererInfos),
                MeshReplacements =
                [
                    new SkinDefParams.MeshReplacement
                    {
                        renderer = skinnedRenderer,
                        mesh = skinnedRenderer.sharedMesh
                    }
                ]
            });

            modelSkins.skins =
            [
                defaultSkinDef
            ];
        }

        ArrayUtils.ArrayAppend(ref modelSkins.skins, skin);
        return true;
    }
    #endregion

    #region Icons
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
    /// <returns>The icon sprite</returns>
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
    #endregion

}
