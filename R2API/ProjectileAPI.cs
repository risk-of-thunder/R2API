using R2API.ContentManagement;
using R2API.Utils;
using RoR2.Projectile;
using System;
using System.Reflection;
using UnityEngine;

namespace R2API {

    /// <summary>
    /// API for adding custom projectile to the game.
    /// </summary>
    [R2APISubmodule]
    [Obsolete($"The {nameof(ProjectileAPI)} is obsolete, please add your Projectiles via R2API.ContentManagment.R2APIContentManager.AddContent()")]
    public static class ProjectileAPI {
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
        /// Add a custom projectile to the list of available projectiles.
        /// If this is called after the ProjectileCatalog inits then this will return false and ignore the custom projectile.
        /// </summary>
        /// <param name="projectile">The projectile prefab to add.</param>
        /// <returns>true if added, false otherwise</returns>
        [Obsolete($"Add is obsolete, please add your Projectiles via R2API.ContentManagement.ContentAdditionHelpers.AddProjectile()")]
        public static bool Add(GameObject? projectile) {
            if (!Loaded) {
                throw new InvalidOperationException($"{nameof(ProjectileAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(ProjectileAPI)})]");
            }

            if (!CatalogBlockers.GetAvailability<ProjectileController>()) {
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
