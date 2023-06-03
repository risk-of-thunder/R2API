using System;
using System.Collections;
using System.Collections.Generic;
using R2API.AutoVersionGen;
using R2API.MiscHelpers;
using R2API.Utils;
using RoR2;
using RoR2.Networking;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace R2API;

/// <summary>
/// API for retrieving assets from the game scenes.
/// </summary>
#pragma warning disable CS0436 // Type conflicts with imported type
[AutoVersion]
#pragma warning restore CS0436 // Type conflicts with imported type
public static partial class SceneAssetAPI
{
    public const string PluginGUID = R2API.PluginGUID + ".sceneasset";
    public const string PluginName = R2API.PluginName + ".SceneAsset";

    /// <summary>
    /// Return true if the submodule is loaded.
    /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
    [Obsolete(R2APISubmoduleDependency.PropertyObsolete)]
#pragma warning restore CS0618 // Type or member is obsolete
    public static bool Loaded => true;

    private static readonly Dictionary<string, List<Action<GameObject[]>>> SceneNameToAssetRequests =
        new Dictionary<string, List<Action<GameObject[]>>>();

    private static bool _hooksEnabled = false;

    internal static void SetHooks()
    {
        if (_hooksEnabled)
        {
            return;
        }

        On.RoR2.SplashScreenController.Finish += PrepareRequests;

        _hooksEnabled = true;
    }

    internal static void UnsetHooks()
    {
        On.RoR2.SplashScreenController.Finish -= PrepareRequests;

        _hooksEnabled = false;
    }

    private static void PrepareRequests(On.RoR2.SplashScreenController.orig_Finish orig, RoR2.SplashScreenController self)
    {
        orig(self);

        foreach (var (sceneName, actionList) in SceneNameToAssetRequests)
        {
            try
            {
                var sceneDef = SceneCatalog.FindSceneDef(sceneName);
                OptionalSceneInstance optionalSceneInstance = new() { HasValue = false };
                if (sceneDef)
                {
                    AssetReferenceScene sceneAddress = sceneDef.sceneAddress;
                    string addressableKey = (sceneAddress != null) ? sceneAddress.AssetGUID : null;
                    var isAStageThatHasAnAddressableKey = !string.IsNullOrEmpty(addressableKey);
                    if (isAStageThatHasAnAddressableKey)
                    {
                        if (NetworkManagerSystem.IsAddressablesKeyValid(addressableKey, typeof(SceneInstance)))
                        {
                            optionalSceneInstance.Value = Addressables.LoadSceneAsync(addressableKey, LoadSceneMode.Additive, false).WaitForCompletion();
                            optionalSceneInstance.HasValue = true;
                            SceneAssetPlugin.Logger.LogInfo($"Loaded the scene {sceneName} through Addressables.LoadSceneAsync");
                        }
                        else
                        {
                            SceneAssetPlugin.Logger.LogError($"Addressable key Scene address is invalid for sceneName {sceneName} | {sceneAddress}");
                            continue;
                        }
                    }
                    else
                    {
                        SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
                        SceneAssetPlugin.Logger.LogInfo($"Loaded the scene {sceneName} through SceneManager.LoadScene");
                    }
                }
                else
                {
                    SceneAssetPlugin.Logger.LogError($"{sceneName} doesnt exist, available scene names:");
                    foreach (var kvp in SceneCatalog.nameToIndex)
                    {
                        SceneAssetPlugin.Logger.LogError($"{kvp.Key}");
                    }
                    continue;
                }

                R2API.Instance.StartCoroutine(ExecuteRequest(sceneName, actionList, optionalSceneInstance));
            }
            catch (Exception e)
            {
                SceneAssetPlugin.Logger.LogError($"Exception in ExecuteRequests : {e}");
            }
        }
    }

    struct OptionalSceneInstance
    {
        public SceneInstance Value;
        public bool HasValue;
    }

    private static IEnumerator ExecuteRequest(string sceneName, List<Action<GameObject[]>> actionList, OptionalSceneInstance optionalSceneInstance)
    {
        // Wait for next frame so that the scene is loaded
        yield return 0;
        yield return 0;

        Scene scene;
        if (optionalSceneInstance.HasValue)
        {
            scene = optionalSceneInstance.Value.Scene;
        }
        else
        {
            scene = SceneManager.GetSceneByName(sceneName);
        }

        var rootObjects = scene.GetRootGameObjects();
        foreach (var action in actionList)
        {
            action(rootObjects);
        }

        if (optionalSceneInstance.HasValue)
        {
            Addressables.UnloadSceneAsync(optionalSceneInstance.Value);
        }
        else
        {
            SceneManager.UnloadSceneAsync(sceneName);
        }

        yield return null;
    }

    /// <summary>
    /// Add a request that will be executed when the scene is loaded.
    /// Will throw an exception if the submodule isn't requested with R2APISubmoduleDependency.
    /// </summary>
    /// <param name="sceneName">The name of scene you want to retrieve assets from.</param>
    /// <param name="onSceneObjectsLoaded">Your action delegate that will be executed when the scene is loaded,
    /// the GameObject[] will contains the scene root game objects.</param>
    /// <exception cref="InvalidOperationException"></exception>
    public static void AddAssetRequest(string? sceneName, Action<GameObject[]>? onSceneObjectsLoaded)
    {
        SceneAssetAPI.SetHooks();
        if (SceneNameToAssetRequests.TryGetValue(sceneName, out var actionList))
        {
            actionList.Add(onSceneObjectsLoaded);
        }
        else
        {
            SceneNameToAssetRequests[sceneName] = new List<Action<GameObject[]>> { onSceneObjectsLoaded };
        }
    }
}
