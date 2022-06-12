using R2API.MiscHelpers;
using R2API.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace R2API {

    /// <summary>
    /// API for retrieving assets from the game scenes.
    /// </summary>
    public static class SceneAssetAPI {
        public const string PluginGUID = R2API.PluginGUID + ".sceneasset";
        public const string PluginName = R2API.PluginName + ".SceneAsset";
        public const string PluginVersion = "0.0.1";

        /// <summary>
        /// Return true if the submodule is loaded.
        /// </summary>
        [Obsolete(R2APISubmoduleDependency.propertyObsolete)]
        public static bool Loaded => true;

        private static readonly Dictionary<string, List<Action<GameObject[]>>> SceneNameToAssetRequests =
            new Dictionary<string, List<Action<GameObject[]>>>();

        internal static void SetHooks() {
            On.RoR2.SplashScreenController.Finish += PrepareRequests;
        }

        internal static void UnsetHooks() {
            On.RoR2.SplashScreenController.Finish -= PrepareRequests;
        }

        private static void PrepareRequests(On.RoR2.SplashScreenController.orig_Finish orig, RoR2.SplashScreenController self) {
            orig(self);

            foreach (var (sceneName, actionList) in SceneNameToAssetRequests) {
                try {
                    SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
                    R2API.Instance.StartCoroutine(ExecuteRequest(sceneName, actionList));
                }
                catch (Exception e) {
                    R2API.Logger.LogError($"Exception in ExecuteRequests : {e}");
                }
            }
        }

        private static IEnumerator ExecuteRequest(string sceneName, List<Action<GameObject[]>> actionList) {
            // Wait for next frame so that the scene is loaded
            yield return 0;

            var scene = SceneManager.GetSceneByName(sceneName);

            var rootObjects = scene.GetRootGameObjects();
            foreach (var action in actionList) {
                action(rootObjects);
            }

            SceneManager.UnloadSceneAsync(sceneName);

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
        public static void AddAssetRequest(string? sceneName, Action<GameObject[]>? onSceneObjectsLoaded) {

            if (SceneNameToAssetRequests.TryGetValue(sceneName, out var actionList)) {
                actionList.Add(onSceneObjectsLoaded);
            }
            else {
                SceneNameToAssetRequests[sceneName] = new List<Action<GameObject[]>> { onSceneObjectsLoaded };
            }
        }
    }
}
