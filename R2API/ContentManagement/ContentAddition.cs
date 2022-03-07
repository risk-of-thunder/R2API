using EntityStates;
using R2API.ContentManagement;
using RoR2;
using RoR2.EntitlementManagement;
using RoR2.ExpansionManagement;
using RoR2.Projectile;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;

namespace R2API {
    /// <summary>
    /// Class for adding Content Assets to your Mod's ContentPack.
    /// </summary>
    public static class ContentAddition {

        #region Add Prefab Methods
        /// <summary>
        /// Adds a BodyPrefab to your Mod's ContentPack
        /// <para>BodyPrefab requires a CharacterBody component.</para>
        /// </summary>
        /// <param name="bodyPrefab">The BodyPrefab to add.</param>
        /// <returns>true if valid and added, false if one of the requirements is not met</returns>
        public static bool AddBody(GameObject bodyPrefab) {
            var asm = Assembly.GetCallingAssembly();
            if (CatalogBlockers.GetAvailability<CharacterBody>()) {
                if (!HasComponent<CharacterBody>(bodyPrefab)) {
                    RejectContent(bodyPrefab, asm, "BodyPrefab", $"but it has no {nameof(CharacterBody)} component!");
                    return false;
                }
                R2APIContentManager.HandleContentAddition(asm, bodyPrefab);
                return true;
            }
            RejectContent(bodyPrefab, asm, "BodyPrefab", $"but the BodyCatalog has already initialized!");
            return false;
        }
        /// <summary>
        /// Adds a MasterPrefab to your Mod's ContentPack
        /// <para>MasterPrefab requires a CharacterMaster component.</para>
        /// </summary>
        /// <param name="masterPrefab">The MasterPrefab to add.</param>
        /// <returns>true if valid and added, false if one of the requirements is not met</returns>
        public static bool AddMaster(GameObject masterPrefab) {
            var asm = Assembly.GetCallingAssembly();
            if (CatalogBlockers.GetAvailability<CharacterMaster>()) {
                if (!HasComponent<CharacterMaster>(masterPrefab)) {
                    RejectContent(masterPrefab, asm, "MasterPrefab", $"but it has no {nameof(CharacterMaster)} component!");
                    return false;
                }
                R2APIContentManager.HandleContentAddition(asm, masterPrefab);
                return true;
            }
            RejectContent(masterPrefab, asm, "MasterPrefab", $"but the MasterCatalog has already initialized!");
            return false;
        }
        /// <summary>
        /// Adds a ProjectilePrefab to your Mod's ContentPack
        /// <para>ProjectilePrefab requires a ProjectileController component.</para>
        /// <para>Throws a warning if it has no assigned ghost prefab.</para>
        /// </summary>
        /// <param name="projectilePrefab">The ProjectilePrefab to add.</param>
        /// <returns>true if valid and added, false if one of the requirements is not met</returns>
        public static bool AddProjectile(GameObject projectilePrefab) {
            var asm = Assembly.GetCallingAssembly();
            if (CatalogBlockers.GetAvailability<ProjectileController>()) {
                if (!HasComponent<ProjectileController>(projectilePrefab)) {
                    RejectContent(projectilePrefab, asm, "ProjectilePrefab", $"but it has no {nameof(ProjectileController)} component!");
                    return false;
                }
                var pc = projectilePrefab.GetComponent<ProjectileController>();
                if (!pc.ghostPrefab) {
                    R2API.Logger.LogWarning($"Projectile {projectilePrefab} has no ghost prefab assigned! is this intentional?");
                }
                R2APIContentManager.HandleContentAddition(asm, projectilePrefab);
                return true;
            }
            RejectContent(projectilePrefab, asm, "ProjectilePrefab", "but the ProjectileCatalog has already initialized!");
            return false;
        }
        /// <summary>
        /// Adds a GameModePrefab to your Mod's ContentPack
        /// <para>GameModePrefab requires a Run component.</para>
        /// </summary>
        /// <param name="gameModePrefab">The GameModePrefab to add.</param>
        /// <returns>true if valid and added, false if one of the requirements is not met</returns>
        public static bool AddGameMode(GameObject gameModePrefab) {
            var asm = Assembly.GetCallingAssembly();
            if (CatalogBlockers.GetAvailability<Run>()) {
                if (!HasComponent<Run>(gameModePrefab)) {
                    RejectContent(gameModePrefab, asm, "GameMode", $"but it has no {nameof(Run)} component!");
                    return false;
                }
                R2APIContentManager.HandleContentAddition(asm, gameModePrefab);
                return true;
            }
            RejectContent(gameModePrefab, asm, "GameMode", "but the GameModeCatalog has already initialized!");
            return false;
        }
        /// <summary>
        /// Adds a NetworkedObject prefab to your Mod's ContentPack
        /// <para>NetworkedObject requires a NetworkIdentity component.</para>
        /// <para>NetworkedObject isnt in PrefabAPI's Objects to Network.</para>
        /// </summary>
        /// <param name="networkedObject">The NetworkedObjectPrefab to add.</param>
        /// <returns>true if valid and added, false if one of the requirements is not met</returns>
        public static bool AddNetworkedObject(GameObject networkedObject) {
            var asm = Assembly.GetCallingAssembly();
            if (CatalogBlockers.GetAvailability<NetworkIdentity>()) {
                if (!HasComponent<NetworkIdentity>(networkedObject)) {
                    RejectContent(networkedObject, asm, "NetworkedObject", $"but it has no {nameof(NetworkIdentity)} component!");
                    return false;
                }
                if (PrefabAPI.IsPrefabHashed(networkedObject)) {
                    RejectContent(networkedObject, asm, "NetworkedObject", $"but its already being networked by {nameof(PrefabAPI)}!");
                    return false;
                }
                R2APIContentManager.HandleContentAddition(asm, networkedObject);
                return true;
            }
            RejectContent(networkedObject, asm, "NetworkedObject", "but the GameNetworkManager has already networked all the prefabs!");
            return false;
        }
        /// <summary>
        /// Adds an EffectPrefab to your Mod's ContentPack
        /// EffectPrefab requires an EffectComponent.
        /// <para>Throws a warning if it has no VFXAttributes component.</para>
        /// </summary>
        /// <param name="effectPrefab">The EffectPrefab to add.</param>
        /// <returns>true if valid and added, false if one of the requirements is not met.</returns>
        public static bool AddEffect(GameObject effectPrefab) {
            var asm = Assembly.GetCallingAssembly();
            if (CatalogBlockers.GetAvailability<EffectComponent>()) {
                if (!HasComponent<EffectComponent>(effectPrefab)) {
                    RejectContent(effectPrefab, asm, "EffectPrefab", $"but it has no {nameof(EffectComponent)} component!");
                    return false;
                }
                if (!HasComponent<VFXAttributes>(effectPrefab)) {
                    R2API.Logger.LogWarning($"Effect {effectPrefab} has no {nameof(VFXAttributes)} component! is this intentional?");
                }
                R2APIContentManager.HandleContentAddition(asm, effectPrefab);
                return true;
            }
            RejectContent(effectPrefab, asm, "EffectPrefab", "but the EffectCatalog has already initialized!");
            return false;
        }
        #endregion

        #region Add Scriptable Methods
        /// <summary>
        /// Adds a SkillDef to your Mod's ContentPack
        /// <para>SkillDef Requires a valid activationState</para>
        /// <para>SkillDef's activationStateMachine cannot be Null, Empty or Whitespace</para>
        /// </summary>
        /// <param name="skillDef">the SkillDef to Add.</param>
        /// <returns>true if valid and added, false if one of the requirements is not met</returns>
        public static bool AddSkillDef(SkillDef skillDef) {
            var asm = Assembly.GetCallingAssembly();
            if (CatalogBlockers.GetAvailability<SkillDef>()) {
                if (skillDef.activationState.stateType == null) {
                    RejectContent(skillDef, asm, "SkillDef", $"but it's activation state type is null!");
                    return false;
                }
                if (string.IsNullOrEmpty(skillDef.activationStateMachineName) || string.IsNullOrWhiteSpace(skillDef.activationStateMachineName)) {
                    RejectContent(skillDef, asm, "SkillDef", $"but it's activation state machine name is Null, Whitespace or Empty!");
                    return false;
                }
                R2APIContentManager.HandleContentAddition(asm, skillDef);
                return true;
            }
            RejectContent(skillDef, asm, "SkillDef", "but the SkillCatalog has already initialized!");
            return false;
        }
        /// <summary>
        /// Adds a SkillFamily to your Mod's ContentPack
        /// <para>SkillFamily's Variant's SkillDef cannot be null</para>
        /// </summary>
        /// <param name="skillFamily">The SkillFamily to Add.</param>
        /// <returns>true if valid and added, false if one of the requirements is not met</returns>
        public static bool AddSkillFamily(SkillFamily skillFamily) {
            var asm = Assembly.GetCallingAssembly();
            if (CatalogBlockers.GetAvailability<SkillFamily>()) {
                if (skillFamily.variants.Any(v => v.skillDef == null)) {
                    RejectContent(skillFamily, asm, "SkillFamily", $"but one of it's variant's skillDefs is null!");
                    return false;
                }
                R2APIContentManager.HandleContentAddition(asm, skillFamily);
                return true;
            }
            RejectContent(skillFamily, asm, "SkillFamily", "but the SkillCatalog has already initialized!");
            return false;
        }
        /// <summary>
        /// Adds a SceneDef to your Mod's ContentPack
        /// <para>If you want he scene to be weaved with vanilla stages, use RainOfStages</para>
        /// </summary>
        /// <param name="sceneDef">The SceneDef to Add.</param>
        /// <returns>true if valid and added, false if one of the requirements is not met</returns>
        public static bool AddSceneDef(SceneDef sceneDef) {
            //Add stuff here, i dont know what qualifies as a "valid" sceneDef, then again, people should just use ROS for handling sceneDefs, r2api just lets you add them this way for the sake of completion
            var asm = Assembly.GetCallingAssembly();
            if (CatalogBlockers.GetAvailability<SceneDef>()) {
                R2API.Logger.LogInfo($"Assembly {asm.GetName().Name} is trying to add a SceneDef, R2API does not support weaving of Scenes, Use RainOfStages instead for weaving SceneDefs.");
                R2APIContentManager.HandleContentAddition(asm, sceneDef);
                return true;
            }
            RejectContent(sceneDef, asm, "SceneDef", "but the SceneCatalog has already initialized!");
            return false;
        }
        //ItemDefs should be added by ItemAPI, but this method is here purely for completion sake.
        /// <summary>
        /// Adds an ItemDef to your Mod's ContentPack
        /// <para>ItemDefs should be added by ItemAPI's Add methods.</para>
        /// </summary>
        /// <param name="itemDef">The ItemDef to Add.</param>
        /// <returns>true if valid and added, false if one of the requirements is not met</returns>
        public static bool AddItemDef(ItemDef itemDef) {
            var asm = Assembly.GetCallingAssembly();
            if (CatalogBlockers.GetAvailability<ItemDef>()) {
                R2API.Logger.LogInfo($"Assembly {asm.GetName().Name} is adding an {itemDef} via {nameof(ContentAddition)}.{nameof(AddItemDef)}()" +
                    $"The assembly should ideally add them via {nameof(ItemAPI)} so that they can use ItemAPI's IDRS systems, adding anyways.");
                ItemAPI.AddInternal(new CustomItem(itemDef, Array.Empty<ItemDisplayRule>()), asm);
                return true;
            }
            RejectContent(itemDef, asm, "ItemDef", "but the ItemCatalog has already initialized!");
            return false;
        }

        /// <summary>
        /// Adds an ItemTierDef to your Mod's ContentPack
        /// </summary>
        /// <param name="itemTierDef">The ItemTierDef to add</param>
        /// <returns>True if valid and added, false if one of the requirements is not met</returns>
        public static bool AddItemTierDef(ItemTierDef itemTierDef) {
            //Todo: finds what makes an itemTierDef invalid
            var asm = Assembly.GetCallingAssembly();
            if (CatalogBlockers.GetAvailability<ItemTierDef>()) {
                R2APIContentManager.HandleContentAddition(asm, itemTierDef);
                return true;
            }
            RejectContent(itemTierDef, asm, "ItemTierDef", "but the ItemTierCatalog has already initialized!");
            return false;
        }

        /// <summary>
        /// Adds an ItemRelationshipProvider to your Mod's ContentPack
        /// </summary>
        /// <param name="itemRelationshipProvider">The ItemRelationshipProvider to add</param>
        /// <returns>True if valid and added, false if one of the requirements is not met</returns>
        public static bool AddItemRelationshipProvider(ItemRelationshipProvider itemRelationshipProvider) {
            //Todo: Find what makes an ItemRelationshipProvider invalid
            var asm = Assembly.GetCallingAssembly();
            if (CatalogBlockers.GetAvailability<ItemRelationshipProvider>()) {
                R2APIContentManager.HandleContentAddition(asm, itemRelationshipProvider);
                return true;
            }
            RejectContent(itemRelationshipProvider, asm, "ItemRelationshipProvider", "but the ItemCatalog has already initialized!");
            return false;
        }

        /// <summary>
        /// Adds an ItemRelationshipType to your Mod's ContentPack
        /// </summary>
        /// <param name="itemRelationshipType">The ItemRelationshipType to add</param>
        /// <returns>True if valid and added, false if one of the requirements is not met</returns>
        public static bool AddItemRelationshipType(ItemRelationshipType itemRelationshipType) {
            //Todo: Find what makes an ItemRelationshipType invalid
            var asm = Assembly.GetCallingAssembly();
            if (CatalogBlockers.GetAvailability<ItemRelationshipType>()) {
                R2APIContentManager.HandleContentAddition(asm, itemRelationshipType);
                return true;
            }
            RejectContent(itemRelationshipType, asm, "ItemRelationshipType", "but the ItemCatalog has already initialized!");
            return false;
        }

        //EquipmentDefs should be added by ItemAPI, but this method is here purely for completion sake.
        /// <summary>
        /// Adds an EquipmentDef to your Mod's ContentPack
        /// <para>EquipmentDef should be added by ItemAPI's Add methods.</para>
        /// </summary>
        /// <param name="equipmentDef">The EquipmentDef to Add.</param>
        /// <returns>true if valid and added, false if one of the requirements is not met</returns>
        public static bool AddEquipmentDef(EquipmentDef equipmentDef) {
            var asm = Assembly.GetCallingAssembly();
            if (CatalogBlockers.GetAvailability<EquipmentDef>()) {
                R2API.Logger.LogInfo($"Assembly {asm.GetName().Name} is adding an {equipmentDef} via {nameof(ContentAddition)}.{nameof(AddEquipmentDef)}()" +
                    $"The assembly should ideally add them via {nameof(ItemAPI)} so that they can use ItemAPI's IDRS systems, adding anyways.");
                ItemAPI.AddInternal(new CustomEquipment(equipmentDef, Array.Empty<ItemDisplayRule>()), asm);
                return true;
            }
            RejectContent(equipmentDef, asm, "EquipmentDef", "but the EquipmnetCatalog has already initialized!");
            return false;
        }
        /// <summary>
        /// Adds a BuffDef to your Mod's ContentPack
        /// <para>Throws a warning if the buffDef's EliteDef's EquipmentDef's passive buffDef is not the buffDef you pass through</para>
        /// <para>Throws a warning if the buffDef has a startSFX, but the startSFX's eventName is Null, Empty or White space.</para>
        /// </summary>
        /// <param name="buffDef">The BuffDef to Add</param>
        /// <returns>true if valid and added, false if one of the requirements is not met</returns>
        public static bool AddBuffDef(BuffDef buffDef) {
            var asm = Assembly.GetCallingAssembly();
            if (CatalogBlockers.GetAvailability<BuffDef>()) {
                if (buffDef.eliteDef && buffDef.eliteDef.eliteEquipmentDef && buffDef.eliteDef.eliteEquipmentDef.passiveBuffDef != buffDef) {
                    R2API.Logger.LogWarning($"Assembly {asm.GetName().Name} is adding an {buffDef} which has an eliteDef assigned, but said eliteDef's equipmentDef's passiveBuffDef is not {buffDef}! is this intentional?");
                }
                if (buffDef.startSfx && (string.IsNullOrEmpty(buffDef.startSfx.eventName) || string.IsNullOrWhiteSpace(buffDef.startSfx.eventName))) {
                    R2API.Logger.LogWarning($"Assembly {asm.GetName().Name} is adding an {buffDef} that has a startSFX, but the startSFX's NetworkedSoundEventDef's eventName is Null, Empty or Whitespace! is this intentional?");
                }
                R2APIContentManager.HandleContentAddition(asm, buffDef);
                return true;
            }
            RejectContent(buffDef, asm, "BuffDef", "but the BuffCatalog has already initialized!");
            return false;
        }
        //EliteDefs should be added by EliteAPI, but this method is here purely for completion sake.
        /// <summary>
        /// Adds an EliteDef to your Mod's ContentPack
        /// <para>EliteDef should be added by EliteAPI's Add methods.</para>
        /// </summary>
        /// <param name="eliteDef">The EliteDef to Add</param>
        /// <returns>true if valid and added, false if one of the requirements is not met</returns>
        public static bool AddEliteDef(EliteDef eliteDef) {
            var asm = Assembly.GetCallingAssembly();
            if (CatalogBlockers.GetAvailability<EliteDef>()) {
                R2API.Logger.LogInfo($"Assembly {asm.GetName().Name} is adding an {eliteDef} via {nameof(ContentAddition)}.{nameof(AddEliteDef)}()" +
                    $"The assembly should ideally add them via {nameof(EliteAPI)} so that they can use EliteAPI's elite tier systems, adding the elite anyways as Tier1 elite.");
                EliteAPI.AddInternal(new CustomElite(eliteDef, new List<CombatDirector.EliteTierDef> { EliteAPI.VanillaEliteTiers[1], EliteAPI.VanillaEliteTiers[2] }), asm);
                return true;
            }
            RejectContent(eliteDef, asm, "EliteDef", "but the EliteCatalog has already initialized!");
            return false;
        }
        //UnlockableDefs should be added by UnlockableAPI, despite this, players could make new unlockableDefs for usage with DirectorCards (such as forbidden unlockables) or for Log entries, which also require unlockableDefs.
        /// <summary>
        /// Adds an UnlockableDef to your Mod's ContentPack
        /// <para>If you want the unlockable to be tied to an achievement, use UnlockableAPI instead.</para>
        /// </summary>
        /// <param name="unlockableDef">The UnlockableDef to Add</param>
        /// <returns>true if valid and added, false if one of the requirements is not met</returns>
        public static bool AddUnlockableDef(UnlockableDef unlockableDef) {
            var asm = Assembly.GetCallingAssembly();
            if (CatalogBlockers.GetAvailability<UnlockableDef>()) {
                R2APIContentManager.HandleContentAddition(asm, unlockableDef);
                return true;
            }
            RejectContent(unlockableDef, asm, "UnlockableDef", "but the UnlockableCatalog has already initialized");
            return false;
        }
        /// <summary>
        /// Adds a SurvivorDef to your Mod's ContentPack
        /// <para>Requires the bodyPrefab to be assigned</para>
        /// <para>BodyPrefab requires a CharacterBody component</para>
        /// <para>Throws a warning if no displayPrefab is assigned</para>
        /// </summary>
        /// <param name="survivorDef">The SurvivorDef to Add</param>
        /// <returns>true if valid and added, false if one of the requirements is not met</returns>
        public static bool AddSurvivorDef(SurvivorDef survivorDef) {
            var asm = Assembly.GetCallingAssembly();
            if (CatalogBlockers.GetAvailability<SurvivorDef>()) {
                if (!survivorDef.bodyPrefab) {
                    RejectContent(survivorDef, asm, "SurvivorDef", $"but it's bodyPrefab is not assigned!");
                    return false;
                }
                if (!survivorDef.bodyPrefab.GetComponent<CharacterBody>()) {
                    RejectContent(survivorDef, asm, "SurvivorDef", $"but it's bodyPrefab does not have a {nameof(CharacterBody)} component!");
                    return false;
                }
                if (!survivorDef.displayPrefab) {
                    R2API.Logger.LogWarning($"Assembly {asm.GetName().Name} is adding an {survivorDef} that does not have a displayPrefab! is this intentional?");
                }
                R2APIContentManager.HandleContentAddition(asm, survivorDef);
                return true;
            }
            RejectContent(survivorDef, asm, "SurvivorDef", "but the SurvivorCatalog has already initialized!");
            return false;
        }
        /// <summary>
        /// Adds an ArtifactDef to your Mod's ContentPack
        /// <para>Requires the ArtifactDef's icon sprites to not be null.</para>
        /// </summary>
        /// <param name="artifactDef">The ArtifactDef to Add</param>
        /// <returns>true if valid and added, false if one of the requirements is not met</returns>
        public static bool AddArtifactDef(ArtifactDef artifactDef) {
            var asm = Assembly.GetCallingAssembly();
            if (CatalogBlockers.GetAvailability<ArtifactDef>()) {
                if (artifactDef.smallIconDeselectedSprite == null || artifactDef.smallIconSelectedSprite == null) {
                    RejectContent(artifactDef, asm, "ArtifactDef", $"but one of it's icons are null! this is not allowed!");
                    return false;
                }
                R2APIContentManager.HandleContentAddition(asm, artifactDef);
                return true;
            }
            RejectContent(artifactDef, asm, "ArtifactDef", "but the ArtifactCatalog has already initialized!");
            return false;
        }
        /// <summary>
        /// Adds a SurfaceDef to your Mod's ContentPack
        /// <para>Requires the surfaceDef's impactEffect or footstepEffect prefabs to not be null</para>
        /// </summary>
        /// <param name="surfaceDef">The SurfaceDef to Add</param>
        /// <returns>true if valid and added, false if one of the requirements is not met</returns>
        public static bool AddSurfaceDef(SurfaceDef surfaceDef) {
            var asm = Assembly.GetCallingAssembly();
            if (CatalogBlockers.GetAvailability<SurfaceDef>()) {
                if (surfaceDef.impactEffectPrefab == null || surfaceDef.footstepEffectPrefab == null) {
                    RejectContent(surfaceDef, asm, "SurfaceDef", $"but one of it's effect prefabs are null! this is not allowed!");
                    return false;
                }
                R2APIContentManager.HandleContentAddition(asm, surfaceDef);
                return true;
            }
            RejectContent(surfaceDef, asm, "SurfaceDef", "but the SurfaceDefCatalog has already initialized!");
            return false;
        }
        /// <summary>
        /// Adds a NetworkSoundEventDef to your Mod's ContentPack
        /// <para>Requires that the event's name is not null, empty or whitespace</para>
        /// </summary>
        /// <param name="networkSoundEventDef">The NetworkSoundEventDef to Add</param>
        /// <returns>true if valid and added, false if one of the requirements is not met</returns>
        public static bool AddNetworkSoundEventDef(NetworkSoundEventDef networkSoundEventDef) {
            var asm = Assembly.GetCallingAssembly();
            if (CatalogBlockers.GetAvailability<NetworkSoundEventDef>()) {
                if (string.IsNullOrEmpty(networkSoundEventDef.eventName) || string.IsNullOrWhiteSpace(networkSoundEventDef.eventName)) {
                    RejectContent(networkSoundEventDef, asm, "NetworkSoundEventDef", $"but it's eventName is Null, Empty or Whitespace!");
                    return false;
                }
                R2APIContentManager.HandleContentAddition(asm, networkSoundEventDef);
                return true;
            }
            RejectContent(networkSoundEventDef, asm, "NetworkSoundEventDef", "but the NetworkSoundEventCatalog has already initialized!");
            return false;
        }
        /// <summary>
        /// Adds a MusicTrackDef to your Mod's ContentPack
        /// <para>MusicTrackDefs should only be created in the editor due to WWise's unity integration. If you want to add new songs, use SoundAPI's MusicAPI</para>
        /// </summary>
        /// <param name="musicTrackDef">The MusicTrackDef to Add.</param>
        /// <returns>true if valid and added, false if one of the requirements is not met</returns>
        public static bool AddMusicTrackDef(MusicTrackDef musicTrackDef) {
            var asm = Assembly.GetCallingAssembly();
            if (CatalogBlockers.GetAvailability<MusicTrackDef>()) {
                R2APIContentManager.HandleContentAddition(asm, musicTrackDef);
                return true;
            }
            RejectContent(musicTrackDef, asm, "MusicTrackDef", "but the MusicTrackCatalog has already initialized!");
            return false;
        }
        /// <summary>
        /// Adds a GameEndingDef to your Mod's ContentPack
        /// </summary>
        /// <param name="gameEndingDef">The GameEndingDef to Add</param>
        /// <returns>true if valid and added, false if one of the requirements is not met</returns>
        public static bool AddGameEndingDef(GameEndingDef gameEndingDef) {
            var asm = Assembly.GetCallingAssembly();
            if (CatalogBlockers.GetAvailability<GameEndingDef>()) {
                R2APIContentManager.HandleContentAddition(asm, gameEndingDef);
                return true;
            }
            RejectContent(gameEndingDef, asm, "GameEndingDef", "but the GameEndingCatalog has already initalized!");
            return false;
        }
        /// <summary>
        /// Adds an EntityStateConfiguration to your Mod's ContentPack
        /// <para>ESC's Target Type must inherit from EntityState</para>
        /// <para>ESC's Target Type cannot be Abstract</para>
        /// </summary>
        /// <param name="entityStateConfiguration">The EntityStateConfiguration to Add</param>
        /// <returns>true if valid and added, false if one of the requirements is not met</returns>
        public static bool AddEntityStateConfiguration(EntityStateConfiguration entityStateConfiguration) {
            var asm = Assembly.GetCallingAssembly();
            if (CatalogBlockers.GetAvailability<EntityStateConfiguration>()) {
                Type type = Type.GetType(entityStateConfiguration.targetType.assemblyQualifiedName);
                if (!type.IsSubclassOf(typeof(EntityState))) {
                    RejectContent(entityStateConfiguration, asm, "EntityStateConfiguration", $"but it's targetType ({type.Name}) is not a type that derives from EntityState!");
                    return false;
                }
                if (type.IsAbstract) {
                    RejectContent(entityStateConfiguration, asm, "EntityStateConfiguration", $"but it's targetType ({type.Name}) is abstract!");
                    return false;
                }
                R2APIContentManager.HandleContentAddition(asm, entityStateConfiguration);
                return true;
            }
            RejectContent(entityStateConfiguration, asm, "EntityStateConfiguration", "but the EntityStateCatalog has already initialized!");
            return false;

        }

        /// <summary>
        /// Adds an ExpansionDef to your Mod's ContentPack
        /// </summary>
        /// <param name="expansionDef">The ExpansionDef to Add</param>
        /// <returns>true if valid and added, false if one of the requirements is not met</returns>
        public static bool AddExpansionDef(ExpansionDef expansionDef) {
            //Todo: Find what makes an ExpansionDef invalid
            var asm = Assembly.GetCallingAssembly();
            if (CatalogBlockers.GetAvailability<ExpansionDef>()) {
                R2APIContentManager.HandleContentAddition(asm, expansionDef);
                return true;
            }
            RejectContent(expansionDef, asm, "ExpansionDef", "But the ExpansionCatalog has already initialized!");
            return false;
        }

        /// <summary>
        /// Adds an EntitlementDef to your Mod's ContentPack
        /// </summary>
        /// <param name="entitlementDef">The EntitlementDef to Add</param>
        /// <returns>true if valid and added, false if one of the requirements is not met</returns>
        public static bool AddEntitlementDef(EntitlementDef entitlementDef) {
            //Todo: Find what makes an EntitlementDef invalid
            var asm = Assembly.GetCallingAssembly();
            if (CatalogBlockers.GetAvailability<EntitlementDef>()) {
                R2APIContentManager.HandleContentAddition(asm, entitlementDef);
                return true;
            }
            RejectContent(entitlementDef, asm, "EntitlementDef", "But the EntitlementCatalog has already initialized!");
            return false;
        }

        /// <summary>
        /// Adds a MiscPickupDef to your Mod's ContentPack
        /// </summary>
        /// <param name="miscPickupDef">The MiscPickupDef to Add</param>
        /// <returns>true if valid and added, false if one of the requirements is not met</returns>
        public static bool AddMiscPickupDef(MiscPickupDef miscPickupDef) {
            var asm = Assembly.GetCallingAssembly();
            if (CatalogBlockers.GetAvailability<MiscPickupDef>()) {
                R2APIContentManager.HandleContentAddition(asm, miscPickupDef);
                return true;
            }
            RejectContent(miscPickupDef, asm, "MiscPickupDef", "But the MiscPickupCatalog has already initailized!");
            return false;
        }

        /// <summary>
        /// Adds an EntitySateType to your Mod's ContentPack
        /// <para>State Type cannot be abstract</para>
        /// </summary>
        /// <typeparam name="T">The State's Type</typeparam>
        /// <param name="wasAdded">Wether or not the state Type was succesfully added or not</param>
        /// <returns>A SerializableEntityStateType, the StateType will be null if "wasAdded" is false.</returns>
        public static SerializableEntityStateType AddEntityState<T>(out bool wasAdded) where T : EntityState {
            var asm = Assembly.GetCallingAssembly();
            Type t = typeof(T);
            if (CatalogBlockers.GetAvailability<EntityState>()) {
                if (t.IsAbstract) {
                    RejectContent(t, asm, "EntityStateType", "but the entity state type is markeed as abstract!");
                    wasAdded = false;
                    return new SerializableEntityStateType();
                }
                wasAdded = true;
                R2APIContentManager.HandleEntityState(asm, t);
                return new SerializableEntityStateType(t);
            }
            RejectContent(t, asm, "EntityStateType", "but the EntityStateCatalog has already initialzed!");
            wasAdded = false;
            return new SerializableEntityStateType();
        }
        #endregion

        #region Util Methods
        private static void RejectContent(object content, Assembly assembly, string contentType, string problem) {
            try {
                throw new InvalidOperationException($"Assembly {assembly.GetName().Name} is trying to add a {content} as a {contentType}, {problem}");
            }
            catch (Exception e) { R2API.Logger.LogError(e); }
        }
        private static bool HasComponent<T>(GameObject obj) where T : Component => obj.GetComponent<T>();
        #endregion
    }
}
