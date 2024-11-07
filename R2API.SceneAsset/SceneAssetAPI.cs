using System;
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

    private static readonly Dictionary<string, Action<GameObject[]>> SceneNameToAssetRequests = new();

    private static bool _hooksEnabled = false;
    private static bool _requestsDone = false;

    internal static void SetHooks()
    {
        if (_hooksEnabled)
        {
            return;
        }

        SceneManager.sceneLoaded += SceneManager_sceneLoaded;

        _hooksEnabled = true;
    }

    private static void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
    {
        if (_requestsDone)
            return;

        if (SceneCatalog.availability.available &&
            arg0.name != "loadingbasic" &&
            arg0.name != "splash" &&
            arg0.name != "intro")
        {
            PrepareRequests();
            _requestsDone = true;
        }
    }

    internal static void UnsetHooks()
    {
        _hooksEnabled = false;
    }

    private static void PrepareRequests()
    {
        foreach (var (sceneName, actionList) in SceneNameToAssetRequests)
        {
            try
            {
                var sceneDef = SceneCatalog.FindSceneDef(sceneName);
                if (sceneDef)
                {
                    var sceneAddress = sceneDef.sceneAddress;
                    string addressableKey = (sceneAddress != null) ? sceneAddress.AssetGUID : null;

                    var isAStageThatHasAnAddressableKey = !string.IsNullOrEmpty(addressableKey);
                    if (isAStageThatHasAnAddressableKey)
                    {
                        if (NetworkManagerSystem.IsAddressablesKeyValid(addressableKey, typeof(SceneInstance)))
                        {
                            var asyncOperationHandle = Addressables.LoadSceneAsync(addressableKey, LoadSceneMode.Additive, true);
                            asyncOperationHandle.Completed += (handle) =>
                                ExecuteAddressableRequest(sceneName, actionList, handle.Result);

                            SceneAssetPlugin.Logger.LogInfo($"Loaded the scene {sceneName} through Addressables.LoadSceneAsync");
                        }
                        else
                        {
                            SceneAssetPlugin.Logger.LogError($"Addressable key Scene address is invalid for sceneName {sceneName} | {sceneAddress}");
                        }
                    }
                    else
                    {
                        var scene = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                        scene.completed += (_) => ExecuteRequest(sceneName, actionList);

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
                }
            }
            catch (Exception e)
            {
                SceneAssetPlugin.Logger.LogError($"Exception in ExecuteRequests : {e}");
            }
        }
    }

    private static void ExecuteAddressableRequest(string sceneName, Action<GameObject[]> actionList, SceneInstance sceneInstance)
    {
        var scene = sceneInstance.Scene;

        ExecuteRequestInternal(actionList, scene);

        Addressables.UnloadSceneAsync(sceneInstance);
    }

    private static void ExecuteRequest(string sceneName, Action<GameObject[]> actionList)
    {
        var scene = SceneManager.GetSceneByName(sceneName);

        ExecuteRequestInternal(actionList, scene);

        SceneManager.UnloadSceneAsync(sceneName);
    }

    private static void ExecuteRequestInternal(Action<GameObject[]> actionList, Scene scene)
    {
        var rootObjects = scene.GetRootGameObjects();
        foreach (Action<GameObject[]> action in actionList.GetInvocationList())
        {
            try
            {
                action(rootObjects);
            }
            catch (Exception e)
            {
                SceneAssetPlugin.Logger.LogError(e);
            }
        }
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
        if (SceneNameToAssetRequests.TryGetValue(sceneName, out var _))
        {
            SceneNameToAssetRequests[sceneName] += onSceneObjectsLoaded;
        }
        else
        {
            SceneNameToAssetRequests[sceneName] = onSceneObjectsLoaded;
        }
    }
}
