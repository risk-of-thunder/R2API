using R2API.Utils;
using RoR2;
using RoR2.ContentManagement;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace R2API {

    /// <summary>
    /// API for adding custom artifact to the game.
    /// </summary>
    [R2APISubmodule]
    public static class ArtifactAPI {

        private static readonly List<ArtifactDef> Artifacts = new List<ArtifactDef>();

        private static bool _artifactCatalogInitialized;

        /// <summary>
        /// Return true if the submodule is loaded.
        /// </summary>
        public static bool Loaded {
            get => _loaded;
            internal set => _loaded = value;
        }

        private static bool _loaded;

        #region ModHelper Events and Hooks

        [R2APISubmoduleInit(Stage = InitStage.SetHooks)]
        internal static void SetHooks() {
            R2APIContentPackProvider.WhenContentPackReady += AddArtifactsToGame;
        }

        [R2APISubmoduleInit(Stage = InitStage.UnsetHooks)]
        internal static void UnsetHooks() {
            R2APIContentPackProvider.WhenContentPackReady -= AddArtifactsToGame;
        }

        private static void AddArtifactsToGame(ContentPack r2apiContentPack) {
            foreach (var artifact in Artifacts) {
                R2API.Logger.LogInfo($"Custom Artifact: {artifact.cachedName} added");
            }

            r2apiContentPack.artifactDefs.Add(Artifacts.ToArray());
            _artifactCatalogInitialized = true;
        }

        #endregion ModHelper Events and Hooks

        #region Add Methods

        /// <summary>
        /// Add a custom artifact to the list of available artifacts.
        /// If this is called after the ArtifactCatalog inits then this will return false and ignore the custom artifact.
        /// </summary>
        /// <param name="artifactDef">The artifactDef to add.</param>
        /// <returns>true if added, false otherwise</returns>
        public static bool Add(ArtifactDef? artifactDef) {
            if (!Loaded) {
                throw new InvalidOperationException($"{nameof(ArtifactAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(ArtifactAPI)})]");
            }

            if (_artifactCatalogInitialized) {
                R2API.Logger.LogError(
                    $"Too late ! Tried to add artifact: {artifactDef.cachedName} after the artifact list was created");
                return false;
            }

            Artifacts.Add(artifactDef);
            return true;
        }

        /// <summary>
        /// Add a custom artifact to the list of available artifacts.
        /// If this is called after the ArtifactCatalog inits then this will return false and ignore the custom artifact.
        /// </summary>
        /// <returns>true if added, false otherwise</returns>
        public static bool Add(
            string name,
            string descriptionToken, string nameToken,
            GameObject pickupModelPrefab,
            Sprite smallIconDeselectedSprite, Sprite smallIconSelectedSprite,
            UnlockableDef unlockableDef) {
            if (!Loaded) {
                throw new InvalidOperationException($"{nameof(ArtifactAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(ArtifactAPI)})]");
            }

            if (_artifactCatalogInitialized) {
                R2API.Logger.LogError(
                    $"Too late ! Tried to add artifact: {name} after the artifact list was created");
                return false;
            }

            var artifactDef = ScriptableObject.CreateInstance<ArtifactDef>();
            artifactDef.cachedName = name;
            artifactDef.descriptionToken = descriptionToken;
            artifactDef.nameToken = nameToken;
            artifactDef.pickupModelPrefab = pickupModelPrefab;
            artifactDef.smallIconDeselectedSprite = smallIconDeselectedSprite;
            artifactDef.smallIconSelectedSprite = smallIconSelectedSprite;
            artifactDef.unlockableDef = unlockableDef;

            Artifacts.Add(artifactDef);
            return true;
        }

        #endregion Add Methods
    }
}
