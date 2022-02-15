using BepInEx;
using EntityStates;
using RoR2;
using RoR2.ContentManagement;
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

namespace R2API {
    /// <summary>
    /// A class that's used for managing ContentPacks created by R2API
    /// </summary>
    public static class R2APIContentManager {

        /// <summary>
        /// Returns a read only collection of all the ContentPacks created by R2API
        /// </summary>
        public static ReadOnlyCollection<ContentPack> ManagedContentPacks {
            get {
                if (!contentPacksCreated) {
                    R2API.Logger.LogError($"Cannot return ContentPacks when they havent been created!");
                    return null;
                }
                return _managedContentPacks;
            }
        }
        private static ReadOnlyCollection<ContentPack> _managedContentPacks = null;
        internal static List<R2APIGenericContentPack> genericContentPacks = new List<R2APIGenericContentPack>();

        /// <summary>
        /// When R2API finishes creating the ContentPacks that it manages, this Action is ran.
        /// </summary>
        public static Action OnContentPacksCreated;
        private static bool contentPacksCreated = false;

        //This is an easy way of storing the new content packs that are being created. not to mention that the ContentPack's identifier will be the plugin's GUID
        private static Dictionary<string, SerializableContentPack> BepInModNameToSerialziableContentPack = new Dictionary<string, SerializableContentPack>();
        //Cache-ing the Assembly's main plugin in a dictionary for ease of access.
        private static Dictionary<Assembly, string> AssemblyToBepInModName = new Dictionary<Assembly, string>();
        //Due to the fact that all contents should have unique names to avoid issues with the catalogs, we need to make sure there are no duplicate names whatsoever.
        //This dictionary which gets populated in Init() can be used to get all the currently registered names of a content depending on it's type.
        //We use this to do a While() loop later to ensure no duplicate names between the RoR2ContentPacks, and our own.
        //there might be a better way of doing this by ILHooking each catalog's init and adding a check to handle duplicate names, but i'm not smart enough to do this.
        private static Dictionary<Type, Func<string[]>> TypeToAllCurrentlyRegisteredNames = new Dictionary<Type, Func<string[]>>();

        internal static void Init() {
            string[] BodyPrefabs() {
                return LoadRoR2ContentEarly.ReadOnlyRoR2ContentPack.bodyPrefabs
                    .Union(BepInModNameToSerialziableContentPack.Values
                        .SelectMany(scp => scp.bodyPrefabs))
                    .Select(go => go.name)
                    .ToArray();
            }
            TypeToAllCurrentlyRegisteredNames.Add(typeof(CharacterBody), BodyPrefabs);

            string[] MasterPrefabs() {
                return LoadRoR2ContentEarly.ReadOnlyRoR2ContentPack.masterPrefabs
                    .Union(BepInModNameToSerialziableContentPack.Values
                        .SelectMany(scp => scp.masterPrefabs))
                    .Select(go => go.name)
                    .ToArray();
            }
            TypeToAllCurrentlyRegisteredNames.Add(typeof(CharacterMaster), MasterPrefabs);

            string[] ProjectilePrefabs() {
                return LoadRoR2ContentEarly.ReadOnlyRoR2ContentPack.projectilePrefabs
                    .Union(BepInModNameToSerialziableContentPack.Values
                        .SelectMany(scp => scp.projectilePrefabs))
                    .Select(go => go.name)
                    .ToArray();
            }
            TypeToAllCurrentlyRegisteredNames.Add(typeof(ProjectileController), ProjectilePrefabs);

            string[] RunPrefabs() {
                return LoadRoR2ContentEarly.ReadOnlyRoR2ContentPack.gameModePrefabs
                    .Union(BepInModNameToSerialziableContentPack.Values
                        .SelectMany(scp => scp.gameModePrefabs))
                    .Select(go => go.name)
                    .ToArray();
            }
            TypeToAllCurrentlyRegisteredNames.Add(typeof(Run), RunPrefabs);

            string[] NetworkedPrefabs() {
                return LoadRoR2ContentEarly.ReadOnlyRoR2ContentPack.networkedObjectPrefabs
                    .Union(BepInModNameToSerialziableContentPack.Values
                        .SelectMany(scp => scp.networkedObjectPrefabs))
                    .Select(go => go.name)
                    .ToArray();
            }
            TypeToAllCurrentlyRegisteredNames.Add(typeof(NetworkIdentity), NetworkedPrefabs);

            string[] EffectPrefabs() {
                return LoadRoR2ContentEarly.ReadOnlyRoR2ContentPack.effectDefs
                    .Select(ed => ed.prefab)
                    .Union(BepInModNameToSerialziableContentPack.Values
                        .SelectMany(scp => scp.effectDefs
                            .Select(ed => ed.prefab)))
                    .Select(go => go.name)
                    .ToArray();
            }
            TypeToAllCurrentlyRegisteredNames.Add(typeof(EffectDef), EffectPrefabs);

            string[] SkillDefs() {
                return LoadRoR2ContentEarly.ReadOnlyRoR2ContentPack.skillDefs
                    .Union(BepInModNameToSerialziableContentPack.Values
                        .SelectMany(scp => scp.skillDefs))
                    .Select(sd => sd as ScriptableObject)
                    .Select(so => so.name)
                    .ToArray();
            }
            TypeToAllCurrentlyRegisteredNames.Add(typeof(SkillDef), SkillDefs);

            string[] SkillFamilies() {
                return LoadRoR2ContentEarly.ReadOnlyRoR2ContentPack.skillFamilies
                    .Union(BepInModNameToSerialziableContentPack.Values
                        .SelectMany(scp => scp.skillFamilies))
                    .Select(sf => sf as ScriptableObject)
                    .Select(so => so.name)
                    .ToArray();
            }
            TypeToAllCurrentlyRegisteredNames.Add(typeof(SkillFamily), SkillFamilies);

            string[] SceneDefs() {
                return LoadRoR2ContentEarly.ReadOnlyRoR2ContentPack.sceneDefs
                    .Union(BepInModNameToSerialziableContentPack.Values
                        .SelectMany(scp => scp.sceneDefs))
                    .Select(sd => sd.cachedName)
                    .ToArray();
            }
            TypeToAllCurrentlyRegisteredNames.Add(typeof(SceneDef), SceneDefs);

            string[] ItemDefs() {
                return LoadRoR2ContentEarly.ReadOnlyRoR2ContentPack.itemDefs
                    .Union(BepInModNameToSerialziableContentPack.Values
                        .SelectMany(scp => scp.itemDefs))
                    .Select(id => id.name)
                    .ToArray();
            }
            TypeToAllCurrentlyRegisteredNames.Add(typeof(ItemDef), ItemDefs);

            string[] EquipmentDefs() {
                return LoadRoR2ContentEarly.ReadOnlyRoR2ContentPack.equipmentDefs
                    .Union(BepInModNameToSerialziableContentPack.Values
                        .SelectMany(scp => scp.equipmentDefs))
                    .Select(ed => ed.name)
                    .ToArray();
            }
            TypeToAllCurrentlyRegisteredNames.Add(typeof(EquipmentDef), EquipmentDefs);

            string[] BuffDefs() {
                return LoadRoR2ContentEarly.ReadOnlyRoR2ContentPack.buffDefs
                    .Union(BepInModNameToSerialziableContentPack.Values
                        .SelectMany(scp => scp.buffDefs))
                    .Select(bd => bd.name)
                    .ToArray();
            }
            TypeToAllCurrentlyRegisteredNames.Add(typeof(BuffDef), BuffDefs);

            string[] EliteDefs() {
                return LoadRoR2ContentEarly.ReadOnlyRoR2ContentPack.eliteDefs
                    .Union(BepInModNameToSerialziableContentPack.Values
                        .SelectMany(scp => scp.eliteDefs))
                    .Select(ed => ed.name)
                    .ToArray();
            }
            TypeToAllCurrentlyRegisteredNames.Add(typeof(EliteDef), EliteDefs);

            string[] UnlockableDefs() {
                return LoadRoR2ContentEarly.ReadOnlyRoR2ContentPack.unlockableDefs
                    .Union(BepInModNameToSerialziableContentPack.Values
                        .SelectMany(scp => scp.unlockableDefs))
                    .Select(ud => ud.cachedName)
                    .ToArray();
            }
            TypeToAllCurrentlyRegisteredNames.Add(typeof(UnlockableDef), UnlockableDefs);

            string[] SurvivorDefs() {
                return LoadRoR2ContentEarly.ReadOnlyRoR2ContentPack.survivorDefs
                    .Union(BepInModNameToSerialziableContentPack.Values
                        .SelectMany(scp => scp.survivorDefs))
                    .Select(sd => sd.cachedName)
                    .ToArray();
            }
            TypeToAllCurrentlyRegisteredNames.Add(typeof(SurvivorDef), SurvivorDefs);

            string[] ArtifactDefs() {
                return LoadRoR2ContentEarly.ReadOnlyRoR2ContentPack.artifactDefs
                    .Union(BepInModNameToSerialziableContentPack.Values
                        .SelectMany(scp => scp.artifactDefs))
                    .Select(ad => ad.cachedName)
                    .ToArray();
            }
            TypeToAllCurrentlyRegisteredNames.Add(typeof(ArtifactDef), ArtifactDefs);

            string[] SurfaceDefs() {
                return LoadRoR2ContentEarly.ReadOnlyRoR2ContentPack.surfaceDefs
                        .Union(BepInModNameToSerialziableContentPack.Values
                            .SelectMany(scp => scp.surfaceDefs))
                        .Select(sd => sd.name)
                        .ToArray();
            }
            TypeToAllCurrentlyRegisteredNames.Add(typeof(SurfaceDef), SurfaceDefs);

            string[] NetworkSoundEventDefs() {
                return LoadRoR2ContentEarly.ReadOnlyRoR2ContentPack.networkSoundEventDefs
                    .Union(BepInModNameToSerialziableContentPack.Values
                        .SelectMany(scp => scp.networkSoundEventDefs))
                    .Select(nsed => nsed.name)
                    .ToArray();
            }
            TypeToAllCurrentlyRegisteredNames.Add(typeof(NetworkSoundEventDef), NetworkSoundEventDefs);

            string[] MusicTrackDefs() {
                return LoadRoR2ContentEarly.ReadOnlyRoR2ContentPack.musicTrackDefs
                    .Union(BepInModNameToSerialziableContentPack.Values
                        .SelectMany(scp => scp.musicTrackDefs))
                    .Select(mtd => mtd.cachedName)
                    .ToArray();
            }
            TypeToAllCurrentlyRegisteredNames.Add(typeof(MusicTrackDef), MusicTrackDefs);

            string[] GameEndingDefs() {
                return LoadRoR2ContentEarly.ReadOnlyRoR2ContentPack.gameEndingDefs
                    .Union(BepInModNameToSerialziableContentPack.Values
                        .SelectMany(scp => scp.gameEndingDefs))
                    .Select(ged => ged.cachedName)
                    .ToArray();
            }
            TypeToAllCurrentlyRegisteredNames.Add(typeof(GameEndingDef), GameEndingDefs);

            string[] EntityStateConfigurations() {
                return LoadRoR2ContentEarly.ReadOnlyRoR2ContentPack.entityStateConfigurations
                    .Union(BepInModNameToSerialziableContentPack.Values
                        .SelectMany(scp => scp.entityStateConfigurations))
                    .Select(esc => esc.name)
                    .ToArray();
            }
            TypeToAllCurrentlyRegisteredNames.Add(typeof(EntityStateConfiguration), EntityStateConfigurations);
        }
        internal static void HandleContentAddition(Assembly assembly, Object content) {
            SerializableContentPack scp = GetOrCreateSerializableContentPack(assembly);
            content = EnsureSafeContentName(content, scp.name);
            if (scp) {
                switch (content) {
                    case GameObject go: HandleGameObject(go, scp); break;
                    case SkillDef skd: AddSafe(ref scp.skillDefs, skd, scp.name); break;
                    case SkillFamily sf: AddSafe(ref scp.skillFamilies, sf, scp.name); break;
                    case SceneDef scd: AddSafe(ref scp.sceneDefs, scd, scp.name); break;
                    case ItemDef id: AddSafe(ref scp.itemDefs, id, scp.name); break;
                    case EquipmentDef eqd: AddSafe(ref scp.equipmentDefs, eqd, scp.name); break;
                    case BuffDef bd: AddSafe(ref scp.buffDefs, bd, scp.name); break;
                    case EliteDef ed: AddSafe(ref scp.eliteDefs, ed, scp.name); break;
                    case UnlockableDef ud: AddSafe(ref scp.unlockableDefs, ud, scp.name); break;
                    case SurvivorDef sd: AddSafe(ref scp.survivorDefs, sd, scp.name); break;
                    case ArtifactDef ad: AddSafe(ref scp.artifactDefs, ad, scp.name); break;
                    case SurfaceDef surd: AddSafe(ref scp.surfaceDefs, surd, scp.name); break;
                    case NetworkSoundEventDef nsed: AddSafe(ref scp.networkSoundEventDefs, nsed, scp.name); break;
                    case MusicTrackDef mtd: AddSafe(ref scp.musicTrackDefs, mtd, scp.name); break;
                    case GameEndingDef ged: AddSafe(ref scp.gameEndingDefs, ged, scp.name); break;
                    case EntityStateConfiguration esc: AddSafe(ref scp.entityStateConfigurations, esc, scp.name); break;
                }
            }
        }

        internal static void HandleEntityState(Assembly assembly, Type type) {
            SerializableContentPack scp = GetOrCreateSerializableContentPack(assembly);
            if (scp) {
                AddSafeType(ref scp.entityStateTypes, new SerializableEntityStateType(type), scp.name);
            }
        }

        private static void HandleGameObject(GameObject go, SerializableContentPack scp) {
            bool alreadyNetworked = false;
            if (go.GetComponent<CharacterBody>()) {
                AddSafe(ref scp.bodyPrefabs, go, scp.name);
                alreadyNetworked = true;
            }
            if (go.GetComponent<CharacterMaster>()) {
                AddSafe(ref scp.masterPrefabs, go, scp.name);
                alreadyNetworked = true;
            }
            if (go.GetComponent<ProjectileController>()) {
                AddSafe(ref scp.projectilePrefabs, go, scp.name);
                alreadyNetworked = true;
            }
            if (go.GetComponent<Run>()) {
                AddSafe(ref scp.gameModePrefabs, go, scp.name);
                alreadyNetworked = true;
            }
            //ror2 automatically networks prefabs that are in the arrays above this one. (since all of them already have network identities)
            if (!alreadyNetworked && !PrefabAPI.IsPrefabHashed(go) && go.GetComponent<NetworkIdentity>()) {
                AddSafe(ref scp.networkedObjectPrefabs, go, scp.name);
            }
            //Modify this once dlc 1 comes out, as EffectDefs will be a game object array instead of an EffectDef array.
            if (go.GetComponent<EffectComponent>()) {
                AddSafeType(ref scp.effectDefs, new EffectDef(go), scp.name);
            }
        }

        internal static void CreateContentPacks() {
            if (!contentPacksCreated) {
                R2API.Logger.LogInfo($"Generating a total of {BepInModNameToSerialziableContentPack.Values.Count} ContentPacks...");
                List<ContentPack> contentPacks = new List<ContentPack>();
                foreach (KeyValuePair<string, SerializableContentPack> kvp in BepInModNameToSerialziableContentPack) {
                    ContentPack cp = kvp.Value.CreateContentPack();
                    cp.identifier = kvp.Key;
                    contentPacks.Add(cp);
                    genericContentPacks.Add(new R2APIGenericContentPack(cp));
                    R2API.Logger.LogDebug($"Content pack for {kvp.Key} created.");
                }
                _managedContentPacks = new ReadOnlyCollection<ContentPack>(contentPacks);
                contentPacksCreated = true;
                OnContentPacksCreated?.Invoke();
            }
            else {
                throw new InvalidOperationException($"The Content Pack collection has already been created!");
            }
        }

        #region Util
        private static SerializableContentPack GetOrCreateSerializableContentPack(Assembly assembly) {
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
                    SerializableContentPack serializableContentPack;
                    //If this assembly does not have a content pack assigned to it, create a new one and add it to the dictionary
                    if (!BepInModNameToSerialziableContentPack.ContainsKey(modName)) {
                        serializableContentPack = ScriptableObject.CreateInstance<SerializableContentPack>();
                        serializableContentPack.name = modName;
                        BepInModNameToSerialziableContentPack.Add(modName, serializableContentPack);
                        R2API.Logger.LogInfo($"Created a SerializableContentPack for mod {modName}");
                    }
                    return BepInModNameToSerialziableContentPack[modName];
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
                R2API.Logger.LogWarning($"Cannot add {asset} to content pack {identifier} because the asset has already been added to it's correspoinding array!");
            }
        }

        private static void AddSafeType<T>(ref T[] assetArray, T asset, string identifier) {
            if (!assetArray.Contains(asset)) {
                if (asset is EffectDef ed) {
                    HG.ArrayUtils.ArrayAppend(ref assetArray, asset);
                }
                else {
                    HG.ArrayUtils.ArrayAppend(ref assetArray, asset);
                }
            }
            else {
                R2API.Logger.LogWarning($"Cannot add {asset} to content pack {identifier} because the asset has already been added to it's correspoinding array!");
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
            }
            return obj;
        }
        private static Object EnsureSafeGameObjectName(GameObject go, string identifier) {
            if (go.GetComponent<CharacterBody>()) {
                string[] allBodies = TypeToAllCurrentlyRegisteredNames[typeof(CharacterBody)]();
                if ((string.IsNullOrWhiteSpace(go.name) || string.IsNullOrEmpty(go.name)) && allBodies.Contains(go.name)) {
                    R2API.Logger.LogInfo($"An object with name {go.name} already exists in the registered bodies! creating new name!");
                    go.name = GetNewName(go, identifier, allBodies);
                }
            }
            if (go.GetComponent<CharacterMaster>()) {
                string[] allMasters = TypeToAllCurrentlyRegisteredNames[typeof(CharacterMaster)]();
                if ((string.IsNullOrWhiteSpace(go.name) || string.IsNullOrEmpty(go.name)) && allMasters.Contains(go.name)) {
                    R2API.Logger.LogInfo($"An object with name {go.name} already exists in the registered masters! creating new name!");
                    go.name = GetNewName(go, identifier, allMasters);
                }
            }
            if (go.GetComponent<ProjectileController>()) {
                string[] allProjectiles = TypeToAllCurrentlyRegisteredNames[typeof(ProjectileController)]();
                if ((string.IsNullOrWhiteSpace(go.name) || string.IsNullOrEmpty(go.name)) && allProjectiles.Contains(go.name)) {
                    R2API.Logger.LogInfo($"An object with name {go.name} already exists in the registered projectiles! creating new name!");
                    go.name = GetNewName(go, identifier, allProjectiles);
                }
            }
            if (go.GetComponent<Run>()) {
                string[] allRuns = TypeToAllCurrentlyRegisteredNames[typeof(Run)]();
                if ((string.IsNullOrWhiteSpace(go.name) || string.IsNullOrEmpty(go.name)) && allRuns.Contains(go.name)) {
                    R2API.Logger.LogInfo($"An object with name {go.name} already exists in the registered Runs! creating new name!");
                    go.name = GetNewName(go, identifier, allRuns);
                }
            }
            //ror2 automatically networks prefabs that are in the arrays above this one. (since all of them already have network identities)
            if (!PrefabAPI.IsPrefabHashed(go) && go.GetComponent<NetworkIdentity>()) {
                string[] allNetworkedPrefabs = TypeToAllCurrentlyRegisteredNames[typeof(NetworkIdentity)]();
                if ((string.IsNullOrWhiteSpace(go.name) || string.IsNullOrEmpty(go.name)) && allNetworkedPrefabs.Contains(go.name)) {
                    R2API.Logger.LogInfo($"An object with name {go.name} already exists in the registered networked prefabs! creating new name!");
                    go.name = GetNewName(go, identifier, allNetworkedPrefabs);
                }
            }
            //Modify this once dlc 1 comes out, as EffectDefs will be a game object array instead of an EffectDef array.
            if (go.GetComponent<EffectComponent>()) {
                string[] allEffects = TypeToAllCurrentlyRegisteredNames[typeof(EffectDef)]();
                if ((string.IsNullOrWhiteSpace(go.name) || string.IsNullOrEmpty(go.name)) && allEffects.Contains(go.name)) {
                    R2API.Logger.LogInfo($"An object with name {go.name} already exists in the registered Effects! creating new name!");
                    go.name = GetNewName(go, identifier, allEffects);
                }
            }
            return go;
        }

        private static ScriptableObject EnsureSafeScriptableObjectName<T>(ScriptableObject obj, string identifier) where T : ScriptableObject {
            if (TypeToAllCurrentlyRegisteredNames.TryGetValue(typeof(T), out var func)) {
                string[] allScriptablesOfTypeT = func();
                if ((string.IsNullOrWhiteSpace(obj.name) || string.IsNullOrEmpty(obj.name)) && allScriptablesOfTypeT.Contains(obj.name)) {
                    R2API.Logger.LogInfo($"An object with name {obj.name} already exists in the registered {typeof(T).Name}! creating new Name!");
                    obj.name = GetNewName(obj, identifier, allScriptablesOfTypeT);
                }
            }
            return obj;
        }

        //Creates a new, generic name for a unity asset.
        private static string GetNewName(Object obj, string identifier, string[] allAssets) {
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
