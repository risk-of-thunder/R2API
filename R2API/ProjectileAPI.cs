using R2API.ContentManagment;
using R2API.Utils;
using System;
using System.Reflection;
using UnityEngine;

namespace R2API {

    /// <summary>
    /// API for adding custom projectile to the game.
    /// </summary>
    [R2APISubmodule]
    public static class ProjectileAPI {

        private static bool _projectileCatalogInitialized;

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
            R2APIContentPackProvider.WhenAddingContentPacks += AvoidNewEntries;
        }

        [R2APISubmoduleInit(Stage = InitStage.UnsetHooks)]
        internal static void UnsetHooks() {
            R2APIContentPackProvider.WhenAddingContentPacks -= AvoidNewEntries;
        }

        private static void AvoidNewEntries() {
            _projectileCatalogInitialized = true;
        }

        #endregion ModHelper Events and Hooks

        #region Add Methods

        /// <summary>
        /// Add a custom projectile to the list of available projectiles.
        /// If this is called after the ProjectileCatalog inits then this will return false and ignore the custom projectile.
        /// </summary>
        /// <param name="projectile">The projectile prefab to add.</param>
        /// <returns>true if added, false otherwise</returns>
        public static bool Add(GameObject? projectile) {
            if (!Loaded) {
                throw new InvalidOperationException($"{nameof(ProjectileAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(ProjectileAPI)})]");
            }

            if (_projectileCatalogInitialized) {
                R2API.Logger.LogError(
                    $"Too late ! Tried to add projectile: {projectile.name} after the projectile list was created");
                return false;
            }

            R2APIContentManager.HandleContentAddition(Assembly.GetCallingAssembly(), projectile);
            return true;
        }

        #endregion Add Methods
    }
}
