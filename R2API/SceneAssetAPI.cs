using R2API.MiscHelpers;
using R2API.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace R2API {

    /// <summary>
    /// API for retrieving assets from the game scenes.
    /// </summary>
    [R2APISubmodule]
    public static class SceneAssetAPI {

        /// <summary>
        /// Return true if the submodule is loaded.
        /// </summary>
        public static bool Loaded {
            get => _loaded;
            internal set => _loaded = value;
        }

        private static bool _loaded;

        private static readonly Dictionary<string, List<Action<GameObject[]>>> SceneNameToAssetRequests =
            new Dictionary<string, List<Action<GameObject[]>>>();

        [R2APISubmoduleInit(Stage = InitStage.Load)]
        internal static void Load() {
            R2API.R2APIStart += ExecuteRequests;
        }

        private static void ExecuteRequests(object _, EventArgs __) {
            foreach (var (sceneName, actionList) in SceneNameToAssetRequests) {
                try {
                    var asyncStageLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                    asyncStageLoad.allowSceneActivation = false;
                    asyncStageLoad.completed += ___ => {
                        var scene = SceneManager.GetSceneByName(sceneName);

                        var rootObjects = scene.GetRootGameObjects();
                        foreach (var action in actionList) {
                            action(rootObjects);
                        }

                        SceneManager.UnloadSceneAsync(sceneName);
                    };
                }
                catch (Exception e) {
                    R2API.Logger.LogError($"Exception in ExecuteRequests : {e}");
                }
            }

            R2API.R2APIStart -= ExecuteRequests;
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
            if (!Loaded) {
                throw new InvalidOperationException($"{nameof(SceneAssetAPI)} is not loaded. " +
                                                    $"Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(SceneAssetAPI)})]");
            }

            if (SceneNameToAssetRequests.TryGetValue(sceneName, out var actionList)) {
                actionList.Add(onSceneObjectsLoaded);
            }
            else {
                SceneNameToAssetRequests[sceneName] = new List<Action<GameObject[]>> { onSceneObjectsLoaded };
            }
        }
    }
}
