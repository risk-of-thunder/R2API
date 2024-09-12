using System.Collections.Generic;
using System.Collections.ObjectModel;
using RoR2;
using UnityEngine;
using System;
using System.Linq;
using R2API.AutoVersionGen;
using System.Text;
using UnityEngine.AddressableAssets;
using R2API.ContentManagement;
using System.Reflection;
using BepInEx.Logging;
using HG.Reflection;
using BepInEx;

[assembly: HG.Reflection.SearchableAttribute.OptIn]

namespace R2API;

/// <summary>
/// Class for adding and registering SceneDefs
/// </summary>

#pragma warning disable CS0436 // Type conflicts with imported type
[AutoVersion]
#pragma warning restore CS0436 // Type conflicts with imported type
public static partial class StageRegistration
{
    public const string PluginGUID = R2API.PluginGUID + ".stages";
    public const string PluginName = R2API.PluginName + ".Stages";

    public const float defaultWeight = 1;

    private static Dictionary<string, List<SceneDef>> privateStageVariantDictionary = new Dictionary<string, List<SceneDef>>();
    public static ReadOnlyDictionary<string, List<SceneDef>> stageVariantDictionary;

    private static List<SceneCollection> preLoopSceneCollections = new List<SceneCollection>();
    private static List<SceneCollection> postLoopSceneCollections = new List<SceneCollection>();

    private static int numStageCollections = 5;

    private static Material baseBazaarSeerMaterial;

    private static bool _hooksEnabled = false;
    private static bool _sceneCatalogInitialized = false;


    [SystemInitializer(typeof(SceneCatalog))]
    private static void SystemInit()
    {
#if DEBUG
        StagesPlugin.Logger.LogDebug($"Variant dictionary intializing...");
#endif
        foreach (SceneCollection sceneCollection in preLoopSceneCollections)
        {
            foreach (SceneCollection.SceneEntry sceneEntry in sceneCollection.sceneEntries)
            {
                SceneDef sceneDef = sceneEntry.sceneDef;
                if (privateStageVariantDictionary.ContainsKey(sceneDef.baseSceneName) && privateStageVariantDictionary[sceneDef.baseSceneName].Contains(sceneDef)){
                    continue;
                }

                AddSceneOrVariant(sceneDef);
            }
        }

        foreach (SceneCollection sceneCollection in postLoopSceneCollections)
        {
            foreach (SceneCollection.SceneEntry sceneEntry in sceneCollection.sceneEntries)
            {
                SceneDef sceneDef = sceneEntry.sceneDef;
                if (privateStageVariantDictionary.ContainsKey(sceneDef.baseSceneName) && privateStageVariantDictionary[sceneDef.baseSceneName].Contains(sceneDef))
                {
                    continue;
                }

                AddSceneOrVariant(sceneDef);
            }
        }

        RefreshPublicDictionary();

        _sceneCatalogInitialized = true;
#if DEBUG
        PrintSceneCollections();
#endif
    }

    private static void LoopThroughCollection(SceneCollection sc)
    {

    }

    #region Hooks
    internal static void SetHooks()
    {
        if (_hooksEnabled)
        {
            return;
        }

        for (int i = 1; i <= numStageCollections; i++)
        {
            SceneCollection stageSceneCollectionRequest = Addressables.LoadAssetAsync<SceneCollection>("RoR2/Base/SceneGroups/sgStage" + i + ".asset").WaitForCompletion();
            preLoopSceneCollections.Add(stageSceneCollectionRequest);
            stageSceneCollectionRequest = Addressables.LoadAssetAsync<SceneCollection>("RoR2/Base/SceneGroups/loopSgStage" + i + ".asset").WaitForCompletion();
            postLoopSceneCollections.Add(stageSceneCollectionRequest);
        }

        baseBazaarSeerMaterial = Addressables.LoadAssetAsync<Material>("RoR2/Base/bazaar/matBazaarSeerWispgraveyard.mat").WaitForCompletion();

        _hooksEnabled = true;
    }

    internal static void UnsetHooks()
    {
        preLoopSceneCollections.Clear();
        _hooksEnabled = false;
    }

    #endregion

    /// <summary>
    /// Adds a SceneDef to your Mod's ContentPack
    /// </summary>
    /// <param name="sceneDef">The SceneDef to add</param>
    /// <param name="plugin">Your mod plugin</param>
    public static void AddSceneDef(SceneDef sceneDef, PluginInfo plugin)
    {
        StageRegistration.SetHooks();
        AddSceneDefInternal(sceneDef, plugin.Instance.GetType().Assembly);
    }
    internal static void AddSceneDefInternal(SceneDef sceneDef, Assembly addingAssembly)
    {
        R2APIContentManager.HandleContentAddition(addingAssembly, sceneDef);
    }

    /// <summary>
    /// A debug util to print each SceneDef in a loop and their respective Weights.
    /// </summary>
    public static void PrintSceneCollections()
    {
        StageRegistration.SetHooks();

        if (!_sceneCatalogInitialized)
        {
            StagesPlugin.Logger.LogDebug($"This log is printed before the SceneCatalog is initialized. Some results may not be accurate.");
        }

        for (int i = 1; i <= numStageCollections; i++)
        {
            StagesPlugin.Logger.LogDebug($"Stage {i}");
            foreach (SceneCollection.SceneEntry sceneEntry in preLoopSceneCollections[i - 1].sceneEntries)
            {
                StagesPlugin.Logger.LogDebug($"{sceneEntry.sceneDef.cachedName}, baseSceneName: {sceneEntry.sceneDef.baseSceneName}, Weight: {sceneEntry.weight}");
            }
        }
    }
    /// <summary>
    /// A debug util to print each SceneDef in a loop and their respective Weights.
    /// </summary>
    /// <param name="stageNumber">The stages in that stage position</param>
    public static void PrintSceneCollections(int stageNumber)
    {
        StageRegistration.SetHooks();

        if (!_sceneCatalogInitialized)
        {
            StagesPlugin.Logger.LogDebug($"This log is printed before the SceneCatalog is initialized. Some results may not be accurate.");
        }

        StagesPlugin.Logger.LogDebug($"Stage {stageNumber}");
        foreach (SceneCollection.SceneEntry sceneEntry in preLoopSceneCollections[stageNumber - 1].sceneEntries)
        {
            StagesPlugin.Logger.LogDebug($"{sceneEntry.sceneDef.cachedName}, baseSceneName: {sceneEntry.sceneDef.baseSceneName}, Weight: {sceneEntry.weight}");
        }
    }

    /// <summary>
    /// A debug util to print each Scene variant of a specific locale.
    /// </summary>
    /// <param name="key">The baseSceneName (ie. "golemplains")</param>
    public static void PrintSceneVariants(string key)
    {
        if (!stageVariantDictionary.ContainsKey(key))
        {
            StagesPlugin.Logger.LogError($"Entry {key} doesn't exist or isn't populated yet in the dictionary.");
            return;
        }

        if (!_sceneCatalogInitialized)
        {
            StagesPlugin.Logger.LogDebug($"This log is printed before the SceneCatalog is initialized. Some results may not be accurate.");
        }

        StageRegistration.SetHooks();
        foreach (SceneDef sceneDef in stageVariantDictionary[key])
        {
            StagesPlugin.Logger.LogDebug($"{sceneDef.cachedName}, baseSceneName: {sceneDef.baseSceneName}");
        }
    }

    /// <summary>
    /// Registers the SceneDef into the loop. Any SceneDef registered with the same baseSceneName as another SceneDef will be counted as a variant.
    /// </summary>
    /// <param name="sceneDef">The SceneDef being registered</param>
    /// <param name="weight">The weight of the SceneDef being rolled in the SceneCollection</param>
    /// <param name="preLoop">If the stage will roll pre-loop: 1 <= current stage <= 5 </param>
    /// <param name="postLoop">If the stage will roll post-loop: current stage > 5 </param>
    public static void RegisterSceneDefToLoop(SceneDef sceneDef, float weight = defaultWeight, bool preLoop = true, bool postLoop = true)
    {
        StageRegistration.SetHooks();
#if DEBUG
        StagesPlugin.Logger.LogDebug($"Registering {sceneDef.cachedName}.");
#endif

        if (privateStageVariantDictionary.ContainsKey(sceneDef.baseSceneName) && privateStageVariantDictionary[sceneDef.baseSceneName].Contains(sceneDef))
        {
            StagesPlugin.Logger.LogError($"SceneDef {sceneDef.cachedName} is already registered into the Scene Pool");
            return;
        }

        if (sceneDef.stageOrder < 1 || sceneDef.stageOrder > numStageCollections)
        {
            StagesPlugin.Logger.LogError($"SceneDef {sceneDef.cachedName} has a stage order not within 1-5. Please use this method only for stages within the loop.");
            return;
        }

        AddSceneOrVariant(sceneDef);
        AppendSceneCollections(sceneDef, weight, preLoop, postLoop);
        RefreshPublicDictionary();
    }

    /// <summary>
    /// Returns a material for the bazaar seers.
    /// </summary>
    /// <param name="texture">The texture to be put in the material</param>
    /// <returns></returns>
    public static Material MakeBazaarSeerMaterial(Texture2D texture)
    {
        StageRegistration.SetHooks();
        Material bazaarMaterial = UnityEngine.Object.Instantiate(baseBazaarSeerMaterial);
        bazaarMaterial.mainTexture = texture;
        return bazaarMaterial;
    }

    /// <summary>
    /// Returns a material for the bazaar seers.
    /// </summary>
    /// <param name="sceneDef">The SceneDef to be used to make the material. A preview texture must be in the SceneDef.</param>
    /// <returns></returns>
    public static Material MakeBazaarSeerMaterial(SceneDef sceneDef)
    {
        StageRegistration.SetHooks();
        Material bazaarMaterial = UnityEngine.Object.Instantiate(baseBazaarSeerMaterial);
        if(sceneDef.previewTexture == null)
        {
            StagesPlugin.Logger.LogError($"SceneDef {sceneDef.cachedName} does not have a preview texture.");
            return null;
        }
        bazaarMaterial.mainTexture = sceneDef.previewTexture;
        return bazaarMaterial;
    }

    private static void AddSceneOrVariant(SceneDef sceneDef)
    {

        if (privateStageVariantDictionary.ContainsKey(sceneDef.baseSceneName))
        {
            privateStageVariantDictionary[sceneDef.baseSceneName].Add(sceneDef);
        }
        else
        {
            List<SceneDef> list = new List<SceneDef>();
            list.Add(sceneDef);
            privateStageVariantDictionary.Add(sceneDef.baseSceneName, list);
        }
    }

    private static void AppendSceneCollections(SceneDef sceneDef, float weight, bool preLoop, bool postLoop)
    {
        int stageOrderIndex = sceneDef.stageOrder - 1;
        if (preLoop) {
            ref var sceneEntries = ref preLoopSceneCollections[stageOrderIndex]._sceneEntries;
            HG.ArrayUtils.ArrayAppend(ref sceneEntries, new SceneCollection.SceneEntry { sceneDef = sceneDef, weightMinusOne = weight - 1 });
        }

        if (postLoop)
        {
            ref var sceneEntries = ref postLoopSceneCollections[stageOrderIndex]._sceneEntries;
            HG.ArrayUtils.ArrayAppend(ref sceneEntries, new SceneCollection.SceneEntry { sceneDef = sceneDef, weightMinusOne = weight - 1 });
        }

        sceneDef.destinationsGroup = preLoopSceneCollections[(stageOrderIndex + 1) % numStageCollections];
        sceneDef.loopedDestinationsGroup = postLoopSceneCollections[(stageOrderIndex + 1) % numStageCollections];
    }

    private static void RefreshPublicDictionary()
    {
        stageVariantDictionary = new ReadOnlyDictionary<string, List<SceneDef>>(privateStageVariantDictionary);
    }
    
}
