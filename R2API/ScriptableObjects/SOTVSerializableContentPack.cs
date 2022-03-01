using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EntityStates;
using RoR2;
using RoR2.ContentManagement;
using RoR2.EntitlementManagement;
using RoR2.ExpansionManagement;
using RoR2.Skills;
using UnityEngine;

namespace R2API.ScriptableObjects {
    [CreateAssetMenu(fileName = "new SOTVSerializableContentPack", menuName = "R2API/SOTVSerializableContentPack", order = 0)]
    public class SOTVSerializableContentPack : ScriptableObject {

        #region Prefabs
        [Header("Prefabs")]
        [Tooltip("Prefabs with a CharacterBody component")]
        public GameObject[] bodyPrefabs = Array.Empty<GameObject>();

        [Tooltip("Prefabs with a CharacterMaster component")]
        public GameObject[] masterPrefabs = Array.Empty<GameObject>();

        [Tooltip("Prefabs with a ProjectileController component")]
        public GameObject[] projectilePrefabs = Array.Empty<GameObject>();

        [Tooltip("Prefabs with a component that inherits from \"Run\"")]
        public GameObject[] gameModePrefabs = Array.Empty<GameObject>();

        [Tooltip("Prefabs with an EffectComponent component")]
        public GameObject[] effectPrefabs = Array.Empty<GameObject>();

        [Tooltip("Prefabs with a NetworkIdentity component that dont apply to the arrays above")]
        public GameObject[] networkedObjectPrefabs = Array.Empty<GameObject>();
        #endregion

        #region Scriptable Objects
        [Space(5)]
        [Header("Scriptable Objects")]

        public SkillDef[] skillDefs = Array.Empty<SkillDef>();

        public SkillFamily[] skillFamilies = Array.Empty<SkillFamily>();

        public SceneDef[] sceneDefs = Array.Empty<SceneDef>();

        public ItemDef[] itemDefs = Array.Empty<ItemDef>();

        public ItemTierDef[] itemTierDefs = Array.Empty<ItemTierDef>();

        public ItemRelationshipProvider[] itemRelationshipProviders = Array.Empty<ItemRelationshipProvider>();

        public ItemRelationshipType[] itemRelationshipTypes = Array.Empty<ItemRelationshipType>();

        public EquipmentDef[] equipmentDefs = Array.Empty<EquipmentDef>();

        public BuffDef[] buffDefs = Array.Empty<BuffDef>();

        public EliteDef[] eliteDefs = Array.Empty<EliteDef>();

        public UnlockableDef[] unlockableDefs = Array.Empty<UnlockableDef>();

        public SurvivorDef[] survivorDefs = Array.Empty<SurvivorDef>();

        public ArtifactDef[] artifactDefs = Array.Empty<ArtifactDef>();

        public SurfaceDef[] surfaceDefs = Array.Empty<SurfaceDef>();

        public NetworkSoundEventDef[] networkSoundEventDefs = Array.Empty<NetworkSoundEventDef>();

        public MusicTrackDef[] musicTrackDefs = Array.Empty<MusicTrackDef>();

        public GameEndingDef[] gameEndingDefs = Array.Empty<GameEndingDef>();

        public MiscPickupDef[] miscPickupDefs = Array.Empty<MiscPickupDef>();
        #endregion

        #region Entity States
        [Space(5)]
        [Header("EntityState Related")]

        public EntityStateConfiguration[] entityStateConfigurations = Array.Empty<EntityStateConfiguration>();

        [Tooltip("Types inheriting from EntityState")]
        public SerializableEntityStateType[] entityStateTypes = Array.Empty<SerializableEntityStateType>();
        #endregion

        #region Expansion Related
        [Space(5)]
        [Header("Expansion Related")]

        public ExpansionDef[] expansionDefs = Array.Empty<ExpansionDef>();

        public EntitlementDef[] entitlementDefs = Array.Empty<EntitlementDef>();
        #endregion

        private ContentPack contentPack;
        #region Methods
        private ContentPack CreateContentPackPrivate() {
            ContentPack cp = new ContentPack();
            cp.bodyPrefabs.Add(bodyPrefabs);
            cp.masterPrefabs.Add(masterPrefabs);
            cp.projectilePrefabs.Add(projectilePrefabs);
            cp.gameModePrefabs.Add(gameModePrefabs);
            cp.effectDefs.Add(effectPrefabs.Select(go => new EffectDef(go)).ToArray());
            cp.networkedObjectPrefabs.Add(networkedObjectPrefabs);
            cp.skillDefs.Add(skillDefs);
            cp.skillFamilies.Add(skillFamilies);
            cp.sceneDefs.Add(sceneDefs);
            cp.itemDefs.Add(itemDefs);
            cp.itemTierDefs.Add(itemTierDefs);
            cp.itemRelationshipTypes.Add(itemRelationshipTypes);
            cp.equipmentDefs.Add(equipmentDefs);
            cp.buffDefs.Add(buffDefs);
            cp.eliteDefs.Add(eliteDefs);
            cp.unlockableDefs.Add(unlockableDefs);
            cp.survivorDefs.Add(survivorDefs);
            cp.artifactDefs.Add(artifactDefs);
            cp.surfaceDefs.Add(surfaceDefs);
            cp.networkSoundEventDefs.Add(networkSoundEventDefs);
            cp.musicTrackDefs.Add(musicTrackDefs);
            cp.gameEndingDefs.Add(gameEndingDefs);
            cp.miscPickupDefs.Add(miscPickupDefs);
            cp.entityStateConfigurations.Add(entityStateConfigurations);

            List<Type> list = new List<Type>();
            for (int i = 0; i < entityStateTypes.Length; i++) {
                Type stateType = entityStateTypes[i].stateType;
                if (stateType != null) {
                    list.Add(stateType);
                    continue;
                }
                Debug.LogWarning("SerializableContentPack \"" + base.name + "\" could not resolve type with name \"" + entityStateTypes[i].typeName + "\". The type will not be available in the content pack.");
            }
            cp.entityStateTypes.Add(list.ToArray());

            cp.expansionDefs.Add(expansionDefs);
            cp.entitlementDefs.Add(entitlementDefs);

            return cp;
        }

        public ContentPack GetOrCreateContentPack() {
            if (contentPack != null)
                return contentPack;
            else {
                contentPack = CreateContentPackPrivate();
                return contentPack;
            }
        }
    }
}
