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

namespace R2API;

/// <summary>
/// Class for registering SceneDefs into the loop of stages 1-5. Do not use this class for stages that aren't in the loop.
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

    private static bool _hooksEnabled = false;

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
    }

    internal static void UnsetHooks()
    {
        sceneCollections.Clear();
    }

    #endregion

    public static void AddSceneDef(SceneDef sceneDef)
    {
        StageRegistration.SetHooks();
        AddSceneDefInternal(Assembly.GetCallingAssembly(), sceneDef);
    }
    internal static void AddSceneDefInternal(Assembly addingAssembly, SceneDef sceneDef)
    {
        R2APIContentManager.HandleContentAddition(addingAssembly, sceneDef);
    }

    public static void RegisterSceneDef(SceneDef sceneDef)
    {
        StageRegistration.SetHooks();
        float weight = 1;
        weight = AddSceneOrVariant(sceneDef, weight);
        AppendSceneCollections(sceneDef, weight);
        RefreshPublicDictionary();
    }

    private static float AddSceneOrVariant(SceneDef sceneDef, float weight)
    {
        if (privateStageVariantDictionary.ContainsKey(sceneDef.baseSceneNameOverride))
        {
            List<SceneDef> variants = privateStageVariantDictionary[sceneDef.baseSceneNameOverride];
            variants.Add(sceneDef);
            weight = 1f / variants.Count;


            for (int i = 0; i < sceneCollections[sceneDef.stageOrder]._sceneEntries.Length; i++)
            {
                SceneCollection.SceneEntry sceneEntry = sceneCollections[sceneDef.stageOrder]._sceneEntries[i];
                if (sceneEntry.sceneDef.baseSceneNameOverride == sceneDef.baseSceneNameOverride)
                {
                    sceneEntry.weightMinusOne = weight;
                }
            }
        }
        else
        {
            List<SceneDef> list = new List<SceneDef>();
            list.Add(sceneDef);
            privateStageVariantDictionary.Add(sceneDef.baseSceneNameOverride, list);
        }
        return weight;
    }

    private static void AppendSceneCollections(SceneDef sceneDef, float weight)
    {
        var sceneCollection = sceneCollections[sceneDef.stageOrder]._sceneEntries.ToList();
        sceneCollection.Add(new SceneCollection.SceneEntry { sceneDef = sceneDef, weightMinusOne = weight - 1 });

        sceneDef.destinationsGroup = sceneCollections[sceneDef.stageOrder % numStageCollections + 1];
    }

    private static void RefreshPublicDictionary()
    {
        stageVariantDictionary = new ReadOnlyDictionary<string, List<SceneDef>>(privateStageVariantDictionary);
    }

    [SystemInitializer(typeof(SceneCatalog))]
    private static void SystemInit()
    {
        foreach(SceneCollection sceneCollection in sceneCollections)
        {
            foreach(SceneCollection.SceneEntry sceneEntry in sceneCollection.sceneEntries)
            {
                AddSceneOrVariant(sceneEntry.sceneDef, 1);
            }
        }
        RefreshPublicDictionary();
    }
}
