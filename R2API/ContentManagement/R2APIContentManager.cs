using BepInEx;
using EntityStates;
using HG;
using R2API.MiscHelpers;
using R2API.ScriptableObjects;
using RoR2;
using RoR2.ContentManagement;
using RoR2.EntitlementManagement;
using RoR2.ExpansionManagement;
using RoR2.Projectile;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

namespace R2API.ContentManagement {
    /// <summary>
    /// Represents a SerializableContentPack that's managed by R2API in some way, shape or form
    /// </summary>
    public struct ManagedSerializableContentPack {
        /// <summary>
        /// The SerializableContentPack
        /// </summary>
        public SOTVSerializableContentPack serializableContentPack;
        /// <summary>
        /// Wether or not R2API will create an R2APIGenericContentPack for the finalized ContentPack.
        /// </summary>
        public bool AutoCreateIContentPackProvider { get; }

        internal ManagedSerializableContentPack(SOTVSerializableContentPack serializableContentPack, bool autoCreateIContentPackProvider) {
            this.serializableContentPack = serializableContentPack;
            AutoCreateIContentPackProvider = autoCreateIContentPackProvider;
        }
    }

    /// <summary>
    /// Represents a ContentPack that's managed by R2API in some way, shape or form
    /// </summary>
    public struct ManagedReadOnlyContentPack {

        /// <summary>
        /// The ReadOnlyContentPack
        /// </summary>
        public ReadOnlyContentPack contentPack;

        /// <summary>
        /// The Identifier of the ReadOnlyContentPack
        /// </summary>
        public string Identifier => contentPack.identifier;

        internal ContentPack _contentPack;

        /// <summary>
        /// Wether or not R2API created an R2APIGenericContentPack for this ContentPack
        /// </summary>
        public bool HasAutoCreatedIContentPackProvider { get; }
        internal R2APIGenericContentPack contentPackProvider;

        internal ManagedReadOnlyContentPack(SOTVSerializableContentPack scp, bool autoCreateIContentPackProvider) {
            _contentPack = scp.GetOrCreateContentPack();
            _contentPack.identifier = scp.name;
            contentPack = new ReadOnlyContentPack(_contentPack);

            if(autoCreateIContentPackProvider) {
                HasAutoCreatedIContentPackProvider = true;
                contentPackProvider = new R2APIGenericContentPack(_contentPack);
            }
            else {
                HasAutoCreatedIContentPackProvider = false;
                contentPackProvider = null;
            }
        }
    }

    /// <summary>
    /// A class that's used for managing ContentPacks created by R2API
    /// </summary>
    public static class R2APIContentManager {
        /// <summary>
        /// Returns a read only collection of all the ContentPacks created by R2API
        /// </summary>
        public static ReadOnlyArray<ManagedReadOnlyContentPack> ManagedContentPacks {
            get {
                if (!contentPacksCreated) {
                    R2API.Logger.LogError($"Cannot return ContentPacks when they havent been created!");
                    return null;
                }
                return _managedContentPacks;
            }
        }
        private static ReadOnlyArray<ManagedReadOnlyContentPack> _managedContentPacks = null;

        /// <summary>
        /// When R2API finishes creating the ContentPacks that it manages, this Action is ran.
        /// </summary>
        public static Action OnContentPacksCreated;
        private static bool contentPacksCreated = false;

        //This is an easy way of storing the new content packs that are being created. not to mention that the ContentPack's identifier will be the plugin's GUID
        private static Dictionary<string, ManagedSerializableContentPack> BepInModNameToSerializableContentPack = new Dictionary<string, ManagedSerializableContentPack>();
        //Cache-ing the Assembly's main plugin in a dictionary for ease of access.
        private static Dictionary<Assembly, string> AssemblyToBepInModName = new Dictionary<Assembly, string>();
        //Due to the fact that all contents should have unique names to avoid issues with the catalogs, we need to make sure there are no duplicate names whatsoever.
        //This dictionary which gets populated in Init() can be used to get all the currently registered names of a content depending on it's type.
        //We use this to do a While() loop later to ensure no duplicate names between the RoR2ContentPacks, and our own.
        //there might be a better way of doing this by ILHooking each catalog's init and adding a check to handle duplicate names, but i'm not smart enough to do this.
        private static Dictionary<Type, Func<string[]>> TypeToAllCurrentlyRegisteredNames = new Dictionary<Type, Func<string[]>>();

        #region Public Methods
        /// <summary>
        /// Adds a Pre-Existing SurvivorsOfTheVoidSerializableContentPack as your mod's content pack.
        /// <para>Example usage would be a Thunderkit mod adding their items via ItemAPI to get the advantage of using ItemAPI's IDRS Systems</para>
        /// </summary>
        /// <param name="sotvSCP">The SOTVSerializable Content Pack that will be tied to your mod.</param>
        /// <param name="createIContentPackProvider">If this is set to true, R2API will create a ContentPackProvider for your ContentPack and handle the loading for you.</param>
        public static void AddPreExistingSerializableContentPack(SOTVSerializableContentPack sotvSCP, bool createIContentPackProvider = true) {
            try {
                Assembly assembly = Assembly.GetCallingAssembly();
                if (!AssemblyToBepInModName.ContainsKey(assembly)) {
                    Type mainClass = assembly.GetTypes()
                        .Where(t => t.GetCustomAttribute<BepInPlugin>() != null)
                        .FirstOrDefault();

                    if (mainClass != null) {
                        BepInPlugin attribute = mainClass.GetCustomAttribute<BepInPlugin>();
                        if (attribute != null) {
                            AssemblyToBepInModName.Add(assembly, attribute.Name);
                        }
                    }
                }
                if (AssemblyToBepInModName.TryGetValue(assembly, out string modName)) {
                    sotvSCP.name = modName;
                    if (!BepInModNameToSerializableContentPack.ContainsKey(modName)) {
                        BepInModNameToSerializableContentPack.Add(modName, new ManagedSerializableContentPack(sotvSCP, createIContentPackProvider));
                        R2API.Logger.LogInfo($"Added Pre-Existing SerializableContentPack from mod {modName}");
                        return;
                    }
                    throw new InvalidOperationException($"The Mod {modName} already has a Serializable Content Pack assigned to it!");
                }
                throw new NullReferenceException($"The assembly {assembly} does not have a class that has a BepInPlugin attribute! Cannot assign Serializable Content Pack for {modName}!");
            }
            catch (Exception e) {
                R2API.Logger.LogError(e);
            }
        }

        /// <summary>
        /// Reserves a SerializableContentPack for your mod and returns it
        /// <para>If the SerializableContentPack already exists, it returns it.</para>
        /// </summary>
        /// <returns>The reserved SerializableContentPack</returns>
        public static SOTVSerializableContentPack ReserveSerializableContentPack() => GetOrCreateSerializableContentPack(Assembly.GetCallingAssembly());
        #endregion

        #region Main Methods
        internal static void Init() {
            string[] BodyPrefabs() {
                return LoadRoR2ContentEarly.ReadOnlyRoR2ContentPack.bodyPrefabs
                    .Union(BepInModNameToSerializableContentPack.Values
                        .SelectMany(scp => scp.serializableContentPack.bodyPrefabs))
                    .Select(go => go.name)
                    .ToArray();
            }
            TypeToAllCurrentlyRegisteredNames.Add(typeof(CharacterBody), BodyPrefabs);

            string[] MasterPrefabs() {
                return LoadRoR2ContentEarly.ReadOnlyRoR2ContentPack.masterPrefabs
                    .Union(BepInModNameToSerializableContentPack.Values
                        .SelectMany(scp => scp.serializableContentPack.masterPrefabs))
                    .Select(go => go.name)
                    .ToArray();
            }
            TypeToAllCurrentlyRegisteredNames.Add(typeof(CharacterMaster), MasterPrefabs);

            string[] ProjectilePrefabs() {
                return LoadRoR2ContentEarly.ReadOnlyRoR2ContentPack.projectilePrefabs
                    .Union(BepInModNameToSerializableContentPack.Values
                        .SelectMany(scp => scp.serializableContentPack.projectilePrefabs))
                    .Select(go => go.name)
                    .ToArray();
            }
            TypeToAllCurrentlyRegisteredNames.Add(typeof(ProjectileController), ProjectilePrefabs);

            string[] RunPrefabs() {
                return LoadRoR2ContentEarly.ReadOnlyRoR2ContentPack.gameModePrefabs
                    .Union(BepInModNameToSerializableContentPack.Values
                        .SelectMany(scp => scp.serializableContentPack.gameModePrefabs))
                    .Select(go => go.name)
                    .ToArray();
            }
            TypeToAllCurrentlyRegisteredNames.Add(typeof(Run), RunPrefabs);

            string[] NetworkedPrefabs() {
                return LoadRoR2ContentEarly.ReadOnlyRoR2ContentPack.networkedObjectPrefabs
                    .Union(BepInModNameToSerializableContentPack.Values
                        .SelectMany(scp => scp.serializableContentPack.networkedObjectPrefabs))
                    .Select(go => go.name)
                    .ToArray();
            }
            TypeToAllCurrentlyRegisteredNames.Add(typeof(NetworkIdentity), NetworkedPrefabs);

            string[] EffectPrefabs() {
                return LoadRoR2ContentEarly.ReadOnlyRoR2ContentPack.effectDefs
                    .Select(ed => ed.prefab)
                    .Union(BepInModNameToSerializableContentPack.Values
                        .SelectMany(scp => scp.serializableContentPack.effectPrefabs))
                    .Select(go => go.name)
                    .ToArray();
            }
            TypeToAllCurrentlyRegisteredNames.Add(typeof(EffectDef), EffectPrefabs);

            string[] SkillDefs() {
                return LoadRoR2ContentEarly.ReadOnlyRoR2ContentPack.skillDefs
                    .Union(BepInModNameToSerializableContentPack.Values
                        .SelectMany(scp => scp.serializableContentPack.skillDefs))
                    .Select(sd => sd as ScriptableObject)
                    .Select(so => so.name)
                    .ToArray();
            }
            TypeToAllCurrentlyRegisteredNames.Add(typeof(SkillDef), SkillDefs);

            string[] SkillFamilies() {
                return LoadRoR2ContentEarly.ReadOnlyRoR2ContentPack.skillFamilies
                    .Union(BepInModNameToSerializableContentPack.Values
                        .SelectMany(scp => scp.serializableContentPack.skillFamilies))
                    .Select(sf => sf as ScriptableObject)
                    .Select(so => so.name)
                    .ToArray();
            }
            TypeToAllCurrentlyRegisteredNames.Add(typeof(SkillFamily), SkillFamilies);

            string[] SceneDefs() {
                return LoadRoR2ContentEarly.ReadOnlyRoR2ContentPack.sceneDefs
                    .Union(BepInModNameToSerializableContentPack.Values
                        .SelectMany(scp => scp.serializableContentPack.sceneDefs))
                    .Select(sd => sd.cachedName)
                    .ToArray();
            }
            TypeToAllCurrentlyRegisteredNames.Add(typeof(SceneDef), SceneDefs);

            string[] ItemDefs() {
                return LoadRoR2ContentEarly.ReadOnlyRoR2ContentPack.itemDefs
                    .Union(BepInModNameToSerializableContentPack.Values
                        .SelectMany(scp => scp.serializableContentPack.itemDefs))
                    .Select(id => id.name)
                    .ToArray();
            }
            TypeToAllCurrentlyRegisteredNames.Add(typeof(ItemDef), ItemDefs);

            string[] ItemTierDefs() {
                return LoadRoR2ContentEarly.ReadOnlyRoR2ContentPack.itemTierDefs
                    .Union(BepInModNameToSerializableContentPack.Values
                        .SelectMany(scp => scp.serializableContentPack.itemTierDefs))
                    .Select(itd => itd.name)
                    .ToArray();
            }
            TypeToAllCurrentlyRegisteredNames.Add(typeof(ItemTierDef), ItemTierDefs);

            string[] ItemRelationshipProviders() {
                return LoadRoR2ContentEarly.ReadOnlyRoR2ContentPack.itemRelationshipProviders
                    .Union(BepInModNameToSerializableContentPack.Values
                        .SelectMany(scp => scp.serializableContentPack.itemRelationshipProviders))
                    .Select(irp => irp.name)
                    .ToArray();
            }
            TypeToAllCurrentlyRegisteredNames.Add(typeof(ItemRelationshipProvider), ItemRelationshipProviders);

            string[] ItemRelationshipTypes() {
                return LoadRoR2ContentEarly.ReadOnlyRoR2ContentPack.itemRelationshipTypes
                    .Union(BepInModNameToSerializableContentPack.Values
                        .SelectMany(scp => scp.serializableContentPack.itemRelationshipTypes))
                    .Select(irt => irt.name)
                    .ToArray();
            }
            TypeToAllCurrentlyRegisteredNames.Add(typeof(ItemRelationshipType), ItemRelationshipTypes);

            string[] EquipmentDefs() {
                return LoadRoR2ContentEarly.ReadOnlyRoR2ContentPack.equipmentDefs
                    .Union(BepInModNameToSerializableContentPack.Values
                        .SelectMany(scp => scp.serializableContentPack.equipmentDefs))
                    .Select(ed => ed.name)
                    .ToArray();
            }
            TypeToAllCurrentlyRegisteredNames.Add(typeof(EquipmentDef), EquipmentDefs);

            string[] BuffDefs() {
                return LoadRoR2ContentEarly.ReadOnlyRoR2ContentPack.buffDefs
                    .Union(BepInModNameToSerializableContentPack.Values
                        .SelectMany(scp => scp.serializableContentPack.buffDefs))
                    .Select(bd => bd.name)
                    .ToArray();
            }
            TypeToAllCurrentlyRegisteredNames.Add(typeof(BuffDef), BuffDefs);

            string[] EliteDefs() {
                return LoadRoR2ContentEarly.ReadOnlyRoR2ContentPack.eliteDefs
                    .Union(BepInModNameToSerializableContentPack.Values
                        .SelectMany(scp => scp.serializableContentPack.eliteDefs))
                    .Select(ed => ed.name)
                    .ToArray();
            }
            TypeToAllCurrentlyRegisteredNames.Add(typeof(EliteDef), EliteDefs);

            string[] UnlockableDefs() {
                return LoadRoR2ContentEarly.ReadOnlyRoR2ContentPack.unlockableDefs
                    .Union(BepInModNameToSerializableContentPack.Values
                        .SelectMany(scp => scp.serializableContentPack.unlockableDefs))
                    .Select(ud => ud.cachedName)
                    .ToArray();
            }
            TypeToAllCurrentlyRegisteredNames.Add(typeof(UnlockableDef), UnlockableDefs);

            string[] SurvivorDefs() {
                return LoadRoR2ContentEarly.ReadOnlyRoR2ContentPack.survivorDefs
                    .Union(BepInModNameToSerializableContentPack.Values
                        .SelectMany(scp => scp.serializableContentPack.survivorDefs))
                    .Select(sd => sd.cachedName)
                    .ToArray();
            }
            TypeToAllCurrentlyRegisteredNames.Add(typeof(SurvivorDef), SurvivorDefs);

            string[] ArtifactDefs() {
                return LoadRoR2ContentEarly.ReadOnlyRoR2ContentPack.artifactDefs
                    .Union(BepInModNameToSerializableContentPack.Values
                        .SelectMany(scp => scp.serializableContentPack.artifactDefs))
                    .Select(ad => ad.cachedName)
                    .ToArray();
            }
            TypeToAllCurrentlyRegisteredNames.Add(typeof(ArtifactDef), ArtifactDefs);

            string[] SurfaceDefs() {
                return LoadRoR2ContentEarly.ReadOnlyRoR2ContentPack.surfaceDefs
                        .Union(BepInModNameToSerializableContentPack.Values
                            .SelectMany(scp => scp.serializableContentPack.surfaceDefs))
                        .Select(sd => sd.name)
                        .ToArray();
            }
            TypeToAllCurrentlyRegisteredNames.Add(typeof(SurfaceDef), SurfaceDefs);

            string[] NetworkSoundEventDefs() {
                return LoadRoR2ContentEarly.ReadOnlyRoR2ContentPack.networkSoundEventDefs
                    .Union(BepInModNameToSerializableContentPack.Values
                        .SelectMany(scp => scp.serializableContentPack.networkSoundEventDefs))
                    .Select(nsed => nsed.name)
                    .ToArray();
            }
            TypeToAllCurrentlyRegisteredNames.Add(typeof(NetworkSoundEventDef), NetworkSoundEventDefs);

            string[] MusicTrackDefs() {
                return LoadRoR2ContentEarly.ReadOnlyRoR2ContentPack.musicTrackDefs
                    .Union(BepInModNameToSerializableContentPack.Values
                        .SelectMany(scp => scp.serializableContentPack.musicTrackDefs))
                    .Select(mtd => mtd.cachedName)
                    .ToArray();
            }
            TypeToAllCurrentlyRegisteredNames.Add(typeof(MusicTrackDef), MusicTrackDefs);

            string[] GameEndingDefs() {
                return LoadRoR2ContentEarly.ReadOnlyRoR2ContentPack.gameEndingDefs
                    .Union(BepInModNameToSerializableContentPack.Values
                        .SelectMany(scp => scp.serializableContentPack.gameEndingDefs))
                    .Select(ged => ged.cachedName)
                    .ToArray();
            }
            TypeToAllCurrentlyRegisteredNames.Add(typeof(GameEndingDef), GameEndingDefs);

            string[] EntityStateConfigurations() {
                return LoadRoR2ContentEarly.ReadOnlyRoR2ContentPack.entityStateConfigurations
                    .Union(BepInModNameToSerializableContentPack.Values
                        .SelectMany(scp => scp.serializableContentPack.entityStateConfigurations))
                    .Select(esc => esc.name)
                    .ToArray();
            }
            TypeToAllCurrentlyRegisteredNames.Add(typeof(EntityStateConfiguration), EntityStateConfigurations);

            string[] ExpansionDefs() {
                return LoadRoR2ContentEarly.ReadOnlyRoR2ContentPack.expansionDefs
                    .Union(BepInModNameToSerializableContentPack.Values
                        .SelectMany(scp => scp.serializableContentPack.expansionDefs))
                    .Select(ed => ed.name)
                    .ToArray();
            }
            TypeToAllCurrentlyRegisteredNames.Add(typeof(ExpansionDef), ExpansionDefs);

            string[] EntitlementDefs() {
                return LoadRoR2ContentEarly.ReadOnlyRoR2ContentPack.entitlementDefs
                    .Union(BepInModNameToSerializableContentPack.Values
                        .SelectMany(scp => scp.serializableContentPack.entitlementDefs))
                    .Select(ed => ed.name)
                    .ToArray();
            }
            TypeToAllCurrentlyRegisteredNames.Add(typeof(EntitlementDef), EntitlementDefs);

            string[] MiscPickupDefs() {
                return LoadRoR2ContentEarly.ReadOnlyRoR2ContentPack.miscPickupDefs
                    .Union(BepInModNameToSerializableContentPack.Values
                        .SelectMany(scp => scp.serializableContentPack.miscPickupDefs))
                    .Select(mpd => mpd.name)
                    .ToArray();
            }
            TypeToAllCurrentlyRegisteredNames.Add(typeof(MiscPickupDef), MiscPickupDefs);
        }

        internal static void HandleContentAddition(Assembly assembly, Object content) {
            SOTVSerializableContentPack scp = GetOrCreateSerializableContentPack(assembly);
            content = EnsureSafeContentName(content, scp.name);
            if (scp) {
                try {
                    bool added = false;
                    switch (content) {
                        case GameObject go: HandleGameObject(go, scp); added = true; break;
                        case SkillDef skd: AddSafe(ref scp.skillDefs, skd, scp.name); added = true; break;
                        case SkillFamily sf: AddSafe(ref scp.skillFamilies, sf, scp.name); added = true; break;
                        case SceneDef scd: AddSafe(ref scp.sceneDefs, scd, scp.name); added = true; break;
                        case ItemDef id: AddSafe(ref scp.itemDefs, id, scp.name); added = true; break;
                        case ItemTierDef itd: AddSafe(ref scp.itemTierDefs, itd, scp.name); added = true; break;
                        case ItemRelationshipProvider irp: AddSafe(ref scp.itemRelationshipProviders, irp, scp.name); added = true; break;
                        case ItemRelationshipType irt: AddSafe(ref scp.itemRelationshipTypes, irt, scp.name); added = true; break;
                        case EquipmentDef eqd: AddSafe(ref scp.equipmentDefs, eqd, scp.name); added = true; break;
                        case BuffDef bd: AddSafe(ref scp.buffDefs, bd, scp.name); added = true; break;
                        case EliteDef ed: AddSafe(ref scp.eliteDefs, ed, scp.name); added = true; break;
                        case UnlockableDef ud: AddSafe(ref scp.unlockableDefs, ud, scp.name); added = true; break;
                        case SurvivorDef sd: AddSafe(ref scp.survivorDefs, sd, scp.name); added = true; break;
                        case ArtifactDef ad: AddSafe(ref scp.artifactDefs, ad, scp.name); added = true; break;
                        case SurfaceDef surd: AddSafe(ref scp.surfaceDefs, surd, scp.name); added = true; break;
                        case NetworkSoundEventDef nsed: AddSafe(ref scp.networkSoundEventDefs, nsed, scp.name); added = true; break;
                        case MusicTrackDef mtd: AddSafe(ref scp.musicTrackDefs, mtd, scp.name); added = true; break;
                        case GameEndingDef ged: AddSafe(ref scp.gameEndingDefs, ged, scp.name); added = true; break;
                        case EntityStateConfiguration esc: AddSafe(ref scp.entityStateConfigurations, esc, scp.name); added = true; break;
                        case ExpansionDef exd: AddSafe(ref scp.expansionDefs, exd, scp.name); added = true; break;
                        case EntitlementDef end: AddSafe(ref scp.entitlementDefs, end, scp.name); added = true; break;
                        case MiscPickupDef mpd: AddSafe(ref scp.miscPickupDefs, mpd, scp.name); added = true; break;
                    }
                    if (!added) {
                        throw new ArgumentException($"The content {content.name} ({content.GetType()}) is not supported by the ContentManager! \n" +
                            $"If you think this is an Error and it should be supported, please file a bug report.");
                    }
                }
                catch (Exception e) { R2API.Logger.LogError(e); }
            }
        }

        internal static void HandleEntityState(Assembly assembly, Type type) {
            SOTVSerializableContentPack scp = GetOrCreateSerializableContentPack(assembly);
            if (scp) {
                AddSafeType(ref scp.entityStateTypes, new SerializableEntityStateType(type), scp.name);
            }
        }

        private static void HandleGameObject(GameObject go, SOTVSerializableContentPack scp) {
            try {
                bool alreadyNetworked = false;
                bool addedToAnyCatalogs = false;
                if (go.GetComponent<CharacterBody>()) {
                    AddSafe(ref scp.bodyPrefabs, go, scp.name);
                    alreadyNetworked = true;
                    addedToAnyCatalogs = true;
                }
                if (go.GetComponent<CharacterMaster>()) {
                    AddSafe(ref scp.masterPrefabs, go, scp.name);
                    alreadyNetworked = true;
                    addedToAnyCatalogs = true;
                }
                if (go.GetComponent<ProjectileController>()) {
                    AddSafe(ref scp.projectilePrefabs, go, scp.name);
                    alreadyNetworked = true;
                    addedToAnyCatalogs = true;
                }
                if (go.GetComponent<Run>()) {
                    AddSafe(ref scp.gameModePrefabs, go, scp.name);
                    alreadyNetworked = true;
                    addedToAnyCatalogs = true;
                }
                //ror2 automatically networks prefabs that are in the arrays above this one. (since all of them already have network identities)
                if (!alreadyNetworked && !PrefabAPI.IsPrefabHashed(go) && go.GetComponent<NetworkIdentity>()) {
                    AddSafe(ref scp.networkedObjectPrefabs, go, scp.name);
                    addedToAnyCatalogs = true;
                }
                if (go.GetComponent<EffectComponent>()) {
                    AddSafe(ref scp.effectPrefabs, go, scp.name);
                    addedToAnyCatalogs = true;
                }
                if (!addedToAnyCatalogs) {
                    throw new ArgumentException($"The GameObject {go.name} ({go.GetType()}) does not have any components that are supported by the ContentManager! \n" +
                        $"If you think this is an Error and it should be supported, please file a bug report.");
                }
            }
            catch (Exception e) { R2API.Logger.LogError(e); }
        }

        internal static void CreateContentPacks() {
            if (!contentPacksCreated) {
                R2API.Logger.LogInfo($"Generating a total of {BepInModNameToSerializableContentPack.Values.Count} ContentPacks...");
                List<ManagedReadOnlyContentPack> managedReadOnlyContentPacks = new List<ManagedReadOnlyContentPack>();
                foreach(var (modName, managedSCP) in BepInModNameToSerializableContentPack) {
                    managedReadOnlyContentPacks.Add(new ManagedReadOnlyContentPack(managedSCP.serializableContentPack, managedSCP.AutoCreateIContentPackProvider));
                }
                contentPacksCreated = true;
                _managedContentPacks = new ReadOnlyArray<ManagedReadOnlyContentPack>(managedReadOnlyContentPacks.ToArray());
                OnContentPacksCreated?.Invoke();
            }
            else {
                throw new InvalidOperationException($"The Content Pack collection has already been created!");
            }
        }
        #endregion

        #region Util
        internal static SOTVSerializableContentPack GetOrCreateSerializableContentPack(Assembly assembly) {
            try {
                //If the assembly that's adding the item has not been cached, find the GUID of the assembly and cache it.
                if (!AssemblyToBepInModName.ContainsKey(assembly)) {
                    Type mainClass = assembly.GetTypes()
                     .Where(t => t.GetCustomAttribute<BepInPlugin>() != null)
                     .FirstOrDefault();

                    if (mainClass != null) {
                        BepInPlugin attribute = mainClass.GetCustomAttribute<BepInPlugin>();
                        if (attribute != null) {
                            AssemblyToBepInModName.Add(assembly, attribute.Name);
                        }
                    }
                }

                if (AssemblyToBepInModName.TryGetValue(assembly, out string modName)) {
                    SOTVSerializableContentPack serializableContentPack;
                    //If this assembly does not have a content pack assigned to it, create a new one and add it to the dictionary
                    if (!BepInModNameToSerializableContentPack.ContainsKey(modName)) {
                        serializableContentPack = ScriptableObject.CreateInstance<SOTVSerializableContentPack>();
                        serializableContentPack.name = modName;
                        BepInModNameToSerializableContentPack.Add(modName, new ManagedSerializableContentPack(serializableContentPack, true));
                        R2API.Logger.LogInfo($"Created a SerializableContentPack for mod {modName}");
                    }
                    return BepInModNameToSerializableContentPack[modName].serializableContentPack;
                }
                throw new NullReferenceException($"The assembly {assembly} does not have a class that has a BepInPlugin attribute! Cannot create ContentPack for {modName}!");
            }
            catch (Exception e) {
                R2API.Logger.LogError(e);
                return null;
            }
        }

        private static void AddSafe<T>(ref T[] assetArray, T asset, string identifier) where T : Object {
            if (!assetArray.Contains(asset)) {
                HG.ArrayUtils.ArrayAppend(ref assetArray, asset);
            }
            else {
                R2API.Logger.LogWarning($"Cannot add {asset} to content pack {identifier} because the asset has already been added to it's corresponding array!");
            }
        }

        private static void AddSafeType<T>(ref T[] assetArray, T asset, string identifier) {
            if (!assetArray.Contains(asset)) {
                HG.ArrayUtils.ArrayAppend(ref assetArray, asset);
            }
            else {
                R2API.Logger.LogWarning($"Cannot add {asset} to content pack {identifier} because the asset has already been added to it's corresponding array!");
            }
        }
        #endregion

        #region Duplicate Naming Avoidance Methods
        private static Object EnsureSafeContentName(Object obj, string identifier) {
            switch (obj) {
                case GameObject go: return EnsureSafeGameObjectName(go, identifier);
                case SkillDef skd: return EnsureSafeScriptableObjectName<SkillDef>(skd, identifier);
                case SkillFamily sf: return EnsureSafeScriptableObjectName<SkillFamily>(sf, identifier);
                case SceneDef scd: return EnsureSafeScriptableObjectName<SceneDef>(scd, identifier);
                case ItemDef id: return EnsureSafeScriptableObjectName<ItemDef>(id, identifier);
                case ItemTierDef itd: return EnsureSafeScriptableObjectName<ItemTierDef>(itd, identifier);
                case ItemRelationshipProvider irp: return EnsureSafeScriptableObjectName<ItemRelationshipProvider>(irp, identifier);
                case ItemRelationshipType irt: return EnsureSafeScriptableObjectName<ItemRelationshipType>(irt, identifier);
                case EquipmentDef eqd: return EnsureSafeScriptableObjectName<EquipmentDef>(eqd, identifier);
                case BuffDef bd: return EnsureSafeScriptableObjectName<BuffDef>(bd, identifier);
                case EliteDef ed: return EnsureSafeScriptableObjectName<EliteDef>(ed, identifier);
                case UnlockableDef ud: return EnsureSafeScriptableObjectName<UnlockableDef>(ud, identifier);
                case SurvivorDef sd: return EnsureSafeScriptableObjectName<SurvivorDef>(sd, identifier);
                case ArtifactDef ad: return EnsureSafeScriptableObjectName<ArtifactDef>(ad, identifier);
                case SurfaceDef surd: return EnsureSafeScriptableObjectName<SurfaceDef>(surd, identifier);
                case NetworkSoundEventDef nsed: return EnsureSafeScriptableObjectName<NetworkSoundEventDef>(nsed, identifier);
                case MusicTrackDef mtd: return EnsureSafeScriptableObjectName<MusicTrackDef>(mtd, identifier);
                case GameEndingDef ged: return EnsureSafeScriptableObjectName<GameEndingDef>(ged, identifier);
                case EntityStateConfiguration esc: return EnsureSafeScriptableObjectName<EntityStateConfiguration>(esc, identifier);
                case ExpansionDef exd: return EnsureSafeScriptableObjectName<ExpansionDef>(exd, identifier);
                case EntitlementDef end: return EnsureSafeScriptableObjectName<EntitlementDef>(end, identifier);
                case MiscPickupDef mpd: return EnsureSafeScriptableObjectName<MiscPickupDef>(mpd, identifier);
            }
            return obj;
        }
        private static Object EnsureSafeGameObjectName(GameObject go, string identifier) {
            if (go.GetComponent<CharacterBody>()) {
                string[] allBodies = TypeToAllCurrentlyRegisteredNames[typeof(CharacterBody)]();
                if ((string.IsNullOrWhiteSpace(go.name) || string.IsNullOrEmpty(go.name)) && allBodies.Contains(go.name)) {
                    R2API.Logger.LogInfo($"An object with name \"{go.name}\" already exists in the registered bodies! creating new name!");
                    go.name = GetNewName(go, identifier, allBodies);
                }
            }
            if (go.GetComponent<CharacterMaster>()) {
                string[] allMasters = TypeToAllCurrentlyRegisteredNames[typeof(CharacterMaster)]();
                if ((string.IsNullOrWhiteSpace(go.name) || string.IsNullOrEmpty(go.name)) && allMasters.Contains(go.name)) {
                    R2API.Logger.LogInfo($"An object with name \"{go.name}\" already exists in the registered masters! creating new name!");
                    go.name = GetNewName(go, identifier, allMasters);
                }
            }
            if (go.GetComponent<ProjectileController>()) {
                string[] allProjectiles = TypeToAllCurrentlyRegisteredNames[typeof(ProjectileController)]();
                if ((string.IsNullOrWhiteSpace(go.name) || string.IsNullOrEmpty(go.name)) && allProjectiles.Contains(go.name)) {
                    R2API.Logger.LogInfo($"An object with name \"{go.name}\" already exists in the registered projectiles! creating new name!");
                    go.name = GetNewName(go, identifier, allProjectiles);
                }
            }
            if (go.GetComponent<Run>()) {
                string[] allRuns = TypeToAllCurrentlyRegisteredNames[typeof(Run)]();
                if ((string.IsNullOrWhiteSpace(go.name) || string.IsNullOrEmpty(go.name)) && allRuns.Contains(go.name)) {
                    R2API.Logger.LogInfo($"An object with name \"{go.name}\" already exists in the registered Runs! creating new name!");
                    go.name = GetNewName(go, identifier, allRuns);
                }
            }
            if (!PrefabAPI.IsPrefabHashed(go) && go.GetComponent<NetworkIdentity>()) {
                string[] allNetworkedPrefabs = TypeToAllCurrentlyRegisteredNames[typeof(NetworkIdentity)]();
                if ((string.IsNullOrWhiteSpace(go.name) || string.IsNullOrEmpty(go.name)) && allNetworkedPrefabs.Contains(go.name)) {
                    R2API.Logger.LogInfo($"An object with name \"{go.name}\" already exists in the registered networked prefabs! creating new name!");
                    go.name = GetNewName(go, identifier, allNetworkedPrefabs);
                }
            }
            if (go.GetComponent<EffectComponent>()) {
                string[] allEffects = TypeToAllCurrentlyRegisteredNames[typeof(EffectDef)]();
                if ((string.IsNullOrWhiteSpace(go.name) || string.IsNullOrEmpty(go.name)) && allEffects.Contains(go.name)) {
                    R2API.Logger.LogInfo($"An object with name \"{go.name}\" already exists in the registered Effects! creating new name!");
                    go.name = GetNewName(go, identifier, allEffects);
                }
            }
            return go;
        }

        private static ScriptableObject EnsureSafeScriptableObjectName<T>(ScriptableObject obj, string identifier) where T : ScriptableObject {
            if (TypeToAllCurrentlyRegisteredNames.TryGetValue(typeof(T), out var func)) {
                string[] allScriptablesOfTypeT = func();
                if ((string.IsNullOrWhiteSpace(obj.name) || string.IsNullOrEmpty(obj.name)) && allScriptablesOfTypeT.Contains(obj.name)) {
                    R2API.Logger.LogInfo($"An object with name \"{obj.name}\" already exists in the registered {typeof(T).Name}! creating new Name!");
                    obj.name = GetNewName(obj, identifier, allScriptablesOfTypeT);
                }
            }
            return obj;
        }

        //Creates a new, generic name for a unity asset.
        internal static string GetNewName(Object obj, string identifier, string[] allAssets) {
            int i = 0;
            string newName = obj.name;
            while (allAssets.Contains(newName)) {
                newName = $"{identifier}{obj.GetType().Name}{i}";
                i++;
            }
            R2API.Logger.LogDebug($"The new name for {obj} is {newName}");
            return newName;
        }
        #endregion
    }
}
