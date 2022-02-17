using EntityStates;
using RoR2;
using RoR2.Projectile;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace R2API.ContentManagement {
    internal static class CatalogBlockers {
        private static Dictionary<Type, bool> CanAddContentToCatalog = new Dictionary<Type, bool> {
            {typeof(CharacterBody), true },
            {typeof(CharacterMaster), true },
            {typeof(ProjectileController), true },
            {typeof(Run), true },
            {typeof(NetworkIdentity), true },
            {typeof(EffectComponent), true },
            {typeof(SkillDef), true },
            {typeof(SkillFamily), true },
            {typeof(SceneDef), true },
            {typeof(ItemDef), true },
            {typeof(EquipmentDef), true },
            {typeof(BuffDef), true },
            {typeof(EliteDef), true },
            {typeof(UnlockableDef), true },
            {typeof(SurvivorDef), true },
            {typeof(ArtifactDef), true },
            {typeof(SurfaceDef), true },
            {typeof(NetworkSoundEventDef), true },
            {typeof(MusicTrackDef), true },
            {typeof(GameEndingDef), true },
            {typeof(EntityStateConfiguration), true },
            {typeof(EntityState), true },

            //The rest are catalogs that arent added by scriptable objects or game objects yet.
        };
        /// <summary>
        /// Returns if the Catalog that manages the type T has finished initializing or not.
        /// </summary>
        /// <typeparam name="T">The type that the catalog manages</typeparam>
        /// <returns>True or False depending on wether the catalog has initialized or not. False if the dictionary doesnt contain T as a Key</returns>
        internal static bool GetAvailability<T>() {
            Type t = typeof(T);
            if (CanAddContentToCatalog.ContainsKey(t)) {
                return CanAddContentToCatalog[t];
            }
            return false;
        }

        private static void SetAvailability<T>(bool availability) {
            Type t = typeof(T);
            if (CanAddContentToCatalog.ContainsKey(t)) {
                CanAddContentToCatalog[t] = availability;
            }
        }

        #region CatalogBlocker Methods
        [SystemInitializer(typeof(BodyCatalog))]
        private static void BlockBodies() => SetAvailability<CharacterBody>(false);
        [SystemInitializer(typeof(MasterCatalog))]
        private static void BlockMasters() => SetAvailability<CharacterMaster>(false);
        [SystemInitializer(typeof(ProjectileCatalog))]
        private static void BlockProjectiles() => SetAvailability<ProjectileController>(false);
        [SystemInitializer(typeof(GameModeCatalog))]
        private static void BlockGameModes() => SetAvailability<Run>(false);
        [SystemInitializer(typeof(RoR2.Networking.GameNetworkManager))]
        private static void BlockNetworkedPrefabs() => SetAvailability<NetworkIdentity>(false);
        [SystemInitializer(typeof(EffectCatalog))]
        private static void BlockEffects() => SetAvailability<EffectComponent>(false);
        [SystemInitializer(typeof(SkillCatalog))]
        private static void BlockSkills() => SetAvailability<SkillDef>(false);
        [SystemInitializer(typeof(SkillCatalog))]
        private static void BlockSkillFamilies() => SetAvailability<SkillFamily>(false);
        [SystemInitializer(typeof(SceneCatalog))]
        private static void BlockScenes() => SetAvailability<SceneDef>(false);
        [SystemInitializer(typeof(ItemCatalog))]
        private static void BlockItems() => SetAvailability<ItemDef>(false);
        [SystemInitializer(typeof(EquipmentCatalog))]
        private static void BlockEquipments() => SetAvailability<EquipmentDef>(false);
        [SystemInitializer(typeof(BuffCatalog))]
        private static void BlockBuffs() => SetAvailability<BuffDef>(false);
        [SystemInitializer(typeof(EliteCatalog))]
        private static void BlockElites() => SetAvailability<EliteDef>(false);
        [SystemInitializer(typeof(UnlockableCatalog))]
        private static void BlockUnlockables() => SetAvailability<UnlockableDef>(false);
        [SystemInitializer(typeof(SurvivorCatalog))]
        private static void BlockSurvivors() => SetAvailability<SurvivorDef>(false);
        [SystemInitializer(typeof(ArtifactCatalog))]
        private static void BlockArtifacts() => SetAvailability<ArtifactDef>(false);
        [SystemInitializer(typeof(SurfaceDefCatalog))]
        private static void BlockSurfaceDefs() => SetAvailability<SurfaceDef>(false);
        [SystemInitializer(typeof(RoR2.Audio.NetworkSoundEventCatalog))]
        private static void BlockNetworkSoundEvent() => SetAvailability<NetworkSoundEventDef>(false);
        [SystemInitializer(typeof(MusicTrackCatalog))]
        private static void BlockMusicTracks() => SetAvailability<MusicTrackDef>(false);
        [SystemInitializer(typeof(GameEndingCatalog))]
        private static void BlockGameEndings() => SetAvailability<GameEndingDef>(false);
        [SystemInitializer(typeof(EntityStateCatalog))]
        private static void BlockEntityStateConfigurations() => SetAvailability<EntityStateConfiguration>(false);
        [SystemInitializer(typeof(EntityStateCatalog))]
        private static void BlockEntityStates() => SetAvailability<EntityState>(false);
        #endregion
        //The rest are catalogs that arent added by scriptable objects or game objects yet.
    }
}
