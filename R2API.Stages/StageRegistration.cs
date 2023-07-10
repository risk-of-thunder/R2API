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

    private static Dictionary<string, List<SceneDef>> privateStageVariantDictionary = new Dictionary<string,List<SceneDef>>();
    public static ReadOnlyDictionary<string, List<SceneDef>> stageVariantDictionary;
    private static List<SceneCollection> sceneCollections = new List<SceneCollection>();
    private static int numStageCollections = 5;

    private static Material baseBazaarSeerMaterial;

    private static bool _hooksEnabled = false;

    [SystemInitializer(typeof(SceneCatalog))]
    private static void SystemInit()
    {
        StagesPlugin.Logger.LogDebug($"Variant dictionary intializing...");

        foreach (SceneCollection sceneCollection in sceneCollections)
        {
            foreach (SceneCollection.SceneEntry sceneEntry in sceneCollection.sceneEntries)
            {
                SceneDef sceneDef = sceneEntry.sceneDef;
                if (privateStageVariantDictionary.ContainsKey(sceneDef.baseSceneName) && privateStageVariantDictionary[sceneDef.baseSceneName].Contains(sceneDef))
                    continue;
                AddSceneOrVariant(sceneDef, 1);
            }
        }
        RefreshPublicDictionary();
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
            sceneCollections.Add(stageSceneCollectionRequest);
        }

        baseBazaarSeerMaterial = Addressables.LoadAssetAsync<Material>("RoR2/Base/bazaar/matBazaarSeerWispgraveyard.mat").WaitForCompletion();

        _hooksEnabled = true;
    }

    internal static void UnsetHooks()
    {
        sceneCollections.Clear();
        _hooksEnabled = false;
    }

    #endregion

    /// <summary>
    /// Adds a SceneDef to your Mod's ContentPack
    /// </summary>
    /// <param name="sceneDef">The SceneDef to add</param>
    public static void AddSceneDef(SceneDef sceneDef)
    {
        StageRegistration.SetHooks();
        AddSceneDefInternal(Assembly.GetCallingAssembly(), sceneDef);
    }
    internal static void AddSceneDefInternal(Assembly addingAssembly, SceneDef sceneDef)
    {
        R2APIContentManager.HandleContentAddition(addingAssembly, sceneDef);
    }

    /// <summary>
    /// A debug util to print each SceneDef in a loop and their respective Weights. All variants of a locale should add up to 1.
    /// </summary>
    public static void PrintSceneCollections()
    {
        StageRegistration.SetHooks();
        for (int i = 1; i <= numStageCollections; i++)
        {
            StagesPlugin.Logger.LogDebug($"Stage {i}");
            foreach(SceneCollection.SceneEntry sceneEntry in sceneCollections[i - 1].sceneEntries)
            {
                StagesPlugin.Logger.LogDebug($"{sceneEntry.sceneDef.cachedName}, baseSceneName: {sceneEntry.sceneDef.baseSceneName}, Weight: {sceneEntry.weightMinusOne + 1}");
            }
        }
    }
    /// <summary>
    /// A debug util to print each SceneDef in a loop and their respective Weights. All variants of a locale should add up to 1.
    /// </summary>
    /// <param name="stageNumber">The stages in that stage position</param>
    public static void PrintSceneCollections(int stageNumber)
    {
        StageRegistration.SetHooks();
        StagesPlugin.Logger.LogDebug($"Stage {stageNumber}");
        foreach (SceneCollection.SceneEntry sceneEntry in sceneCollections[stageNumber - 1].sceneEntries)
        {
            StagesPlugin.Logger.LogDebug($"{sceneEntry.sceneDef.cachedName}, baseSceneName: {sceneEntry.sceneDef.baseSceneName}, Weight: {sceneEntry.weightMinusOne + 1}");
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

        StageRegistration.SetHooks();
        foreach(SceneDef sceneDef in stageVariantDictionary[key])
        {
            StagesPlugin.Logger.LogDebug($"{sceneDef.cachedName}, baseSceneName: {sceneDef.baseSceneName}");
        }
    }

    /// <summary>
    /// Registers the SceneDef into the loop. Any SceneDef registered with the same baseSceneName as another SceneDef will be counted as a variant.
    /// Variants will have their weights split equally amongst each other. Don't use this method for scenes that aren't part of the loop (Stages 1-5).
    /// </summary>
    /// <param name="sceneDef">The SceneDef being registered</param>
    public static void RegisterSceneDefToLoop(SceneDef sceneDef)
    {
        StageRegistration.SetHooks();
        StagesPlugin.Logger.LogDebug($"RegisterSceneDef called from {Assembly.GetCallingAssembly()}");

        if (privateStageVariantDictionary.ContainsKey(sceneDef.baseSceneName) && privateStageVariantDictionary[sceneDef.baseSceneName].Contains(sceneDef))
        {
            StagesPlugin.Logger.LogError($"SceneDef {sceneDef.cachedName} is already registered into the Scene Pool");
            return;
        }

        if(sceneDef.stageOrder < 1 || sceneDef.stageOrder > numStageCollections)
        {
            StagesPlugin.Logger.LogError($"SceneDef {sceneDef.cachedName} has a stage order not within 1-5. Please use this method only for stages within the loop.");
            return;
        }

        float weight = 1;
        weight = AddSceneOrVariant(sceneDef, weight);
        AppendSceneCollections(sceneDef, weight);
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

    private static float AddSceneOrVariant(SceneDef sceneDef, float weight)
    {
        int stageOrderIndex = sceneDef.stageOrder - 1;

        if (privateStageVariantDictionary.ContainsKey(sceneDef.baseSceneName))
        {
            privateStageVariantDictionary[sceneDef.baseSceneName].Add(sceneDef);
            weight = 1f / privateStageVariantDictionary[sceneDef.baseSceneName].Count;

            EqualizeVariantWeights(sceneDef.baseSceneName, stageOrderIndex, weight);
        }
        else
        {
            List<SceneDef> list = new List<SceneDef>();
            list.Add(sceneDef);
            privateStageVariantDictionary.Add(sceneDef.baseSceneName, list);
        }
        return weight;
    }

    //This method also equalizes all vanilla variants since they are already in the SceneCollection, hence why AppendSceneCollections isn't called on SystemInit.
    private static void EqualizeVariantWeights(string key, int stageOrderIndex, float weight)
    {
        for (int i = 0; i < sceneCollections[stageOrderIndex]._sceneEntries.Length; i++)
        {
            if (sceneCollections[stageOrderIndex]._sceneEntries[i].sceneDef.baseSceneName.Equals(key))
            {
                sceneCollections[stageOrderIndex]._sceneEntries[i].weightMinusOne = weight - 1;
            }
        }
    }

    private static void AppendSceneCollections(SceneDef sceneDef, float weight)
    {
        int stageOrderIndex = sceneDef.stageOrder - 1;
        ref var sceneEntries = ref sceneCollections[stageOrderIndex]._sceneEntries;
        HG.ArrayUtils.ArrayAppend(ref sceneEntries, new SceneCollection.SceneEntry { sceneDef = sceneDef, weightMinusOne = weight - 1 });

        sceneDef.destinationsGroup = sceneCollections[(stageOrderIndex + 1) % numStageCollections];
    }

    private static void RefreshPublicDictionary()
    {
        stageVariantDictionary = new ReadOnlyDictionary<string, List<SceneDef>>(privateStageVariantDictionary);
    }

    
}
