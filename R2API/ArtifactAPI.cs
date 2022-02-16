using R2API.ContentManagement;
using R2API.Utils;
using RoR2;
using System;
using System.Reflection;
using UnityEngine;

namespace R2API {

    /// <summary>
    /// API for adding custom artifact to the game.
    /// </summary>
    [R2APISubmodule]
    [Obsolete($"The {nameof(ArtifactAPI)} is obsolete, please add your ArtifactDefs via R2API.ContentManagment.R2APIContentManager.AddContent()")]
    public static class ArtifactAPI {
        /// <summary>
        /// Return true if the submodule is loaded.
        /// </summary>
        public static bool Loaded {
            get => _loaded;
            internal set => _loaded = value;
        }

        private static bool _loaded;

        #region Add Methods

        /// <summary>
        /// Add a custom artifact to the list of available artifacts.
        /// If this is called after the ArtifactCatalog inits then this will return false and ignore the custom artifact.
        /// </summary>
        /// <param name="artifactDef">The artifactDef to add.</param>
        /// <returns>true if added, false otherwise</returns>
        [Obsolete($"Add is obsolete, please add your ArtifactDefs via R2API.ContentManagment.R2APIContentManager.AddContent()")]
        public static bool Add(ArtifactDef? artifactDef) {
            if (!Loaded) {
                throw new InvalidOperationException($"{nameof(ArtifactAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(ArtifactAPI)})]");
            }

            if (!CatalogBlockers.GetAvailability<ArtifactDef>()) {
                R2API.Logger.LogError(
                    $"Too late ! Tried to add artifact: {artifactDef.cachedName} after the ArtifactCatalog has initialized!");
                return false;
            }

            R2APIContentManager.HandleContentAddition(Assembly.GetCallingAssembly(), artifactDef);
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

            if (!CatalogBlockers.GetAvailability<ArtifactDef>()) {
                R2API.Logger.LogError(
                    $"Too late ! Tried to add artifact: {name} after the ArtifactCatalog has initialized!");
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

            R2APIContentManager.HandleContentAddition(Assembly.GetCallingAssembly(), artifactDef);
            return true;
        }

        #endregion Add Methods
    }
}
