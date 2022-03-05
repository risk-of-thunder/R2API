using BepInEx;
using EntityStates;
using HG;
using R2API.MiscHelpers;
using R2API.ScriptableObjects;
using R2API.Utils;
using RoR2;
using RoR2.ContentManagement;
using RoR2.EntitlementManagement;
using RoR2.ExpansionManagement;
using RoR2.Projectile;
using RoR2.Skills;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;
using UnityObject = UnityEngine.Object;

namespace R2API.ContentManagement {
    /// <summary>
    /// Represents a SerializableContentPack that's managed by R2API in some way, shape or form
    /// </summary>
    public struct ManagedSerializableContentPack {
        /// <summary>
        /// The SerializableContentPack
        /// </summary>
        public R2APISerializableContentPack serializableContentPack;
        /// <summary>
        /// Wether or not R2API will create an R2APIGenericContentPack for the finalized ContentPack.
        /// </summary>
        public bool AutoCreateIContentPackProvider { get; }

        public Assembly AssemblyThatCreatedContentPack { get; }

        internal ManagedSerializableContentPack(R2APISerializableContentPack serializableContentPack, bool autoCreateIContentPackProvider, Assembly assemblyThatCreatedContentPack) {
            this.serializableContentPack = serializableContentPack;
            AutoCreateIContentPackProvider = autoCreateIContentPackProvider;
            AssemblyThatCreatedContentPack = assemblyThatCreatedContentPack;
        }
    }

    /// <summary>
    /// Represents a ContentPack that's managed by R2API in some way, shape or form
    /// </summary>
    public struct ManagedReadOnlyContentPack {

        /// <summary>
        /// The ReadOnlyContentPack
        /// </summary>
        public ReadOnlyContentPack ContentPack { get; }

        /// <summary>
        /// The Identifier of the ReadOnlyContentPack
        /// </summary>
        public string Identifier => _contentPack.identifier;

        public Assembly TiedAssembly { get; }

        internal ContentPack _contentPack;

        /// <summary>
        /// Wether or not R2API created an R2APIGenericContentPack for this ContentPack
        /// </summary>
        public bool HasAutoCreatedIContentPackProvider { get; }
        internal R2APIGenericContentPack contentPackProvider;

        internal ManagedReadOnlyContentPack(R2APISerializableContentPack scp, bool autoCreateIContentPackProvider, Assembly assemblyThatCreatedContentPack) {
            _contentPack = scp.GetOrCreateContentPack();
            _contentPack.identifier = scp.name;
            ContentPack = new ReadOnlyContentPack(_contentPack);

            TiedAssembly = assemblyThatCreatedContentPack;

            if (autoCreateIContentPackProvider) {
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
    [R2APISubmodule]
    public static class R2APIContentManager {
        /// <summary>
        /// Return true if the submodule is loaded.
        /// </summary>
        public static bool Loaded {
            get => _loaded;
            internal set => _loaded = value;
        }

        private static bool _loaded;

        /// <summary>
        /// Returns a read only collection of all the ContentPacks created by R2API
        /// </summary>
        public static ReadOnlyArray<ManagedReadOnlyContentPack> ManagedContentPacks {
            get {
                if (!_contentPacksCreated) {
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
        private static bool _contentPacksCreated = false;

        //This is an easy way of storing the new content packs that are being created. not to mention that the ContentPack's identifier will be the plugin's GUID
        private static readonly Dictionary<string, ManagedSerializableContentPack> BepInModNameToSerializableContentPack = new Dictionary<string, ManagedSerializableContentPack>();
        //Cache-ing the Assembly's main plugin in a dictionary for ease of access.
        private static readonly Dictionary<Assembly, string> AssemblyToBepInModName = new Dictionary<Assembly, string>();
        //Populated on CreateContentPacks
        private static readonly Dictionary<ContentPack, Assembly> ContentPackToAssembly = new Dictionary<ContentPack, Assembly>();


        #region Public Methods
        /// <summary>
        /// Adds a Pre-Existing SurvivorsOfTheVoidSerializableContentPack as your mod's content pack.
        /// <para>Example usage would be a Thunderkit mod adding their items via ItemAPI to get the advantage of using ItemAPI's IDRS Systems</para>
        /// </summary>
        /// <param name="sotvSCP">The SOTVSerializable Content Pack that will be tied to your mod.</param>
        /// <param name="createIContentPackProvider">If this is set to true, R2API will create a ContentPackProvider for your ContentPack and handle the loading for you.</param>
        public static void AddPreExistingSerializableContentPack(R2APISerializableContentPack sotvSCP, bool createIContentPackProvider = true) {
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
                        BepInModNameToSerializableContentPack.Add(modName, new ManagedSerializableContentPack(sotvSCP, createIContentPackProvider, assembly));
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
        public static R2APISerializableContentPack ReserveSerializableContentPack() => GetOrCreateSerializableContentPack(Assembly.GetCallingAssembly());

        public static Assembly GetAssemblyFromContentPack(ContentPack contentPack) {
            if (ContentPackToAssembly.TryGetValue(contentPack, out Assembly ass)) {
                return ass;
            }
            return null;
        }
        #endregion

        #region Main Methods

        [R2APISubmoduleInit(Stage = InitStage.SetHooks)]
        internal static void SetHooks() {
            On.RoR2.ContentManagement.ContentManager.SetContentPacks += EnsureUniqueNames;

            Loaded = true;
        }

        [R2APISubmoduleInit(Stage = InitStage.UnsetHooks)]
        internal static void UnsetHooks() {
            On.RoR2.ContentManagement.ContentManager.SetContentPacks -= EnsureUniqueNames;

            Loaded = false;
        }

        [R2APISubmoduleInit(Stage = InitStage.LoadCheck)]
        internal static void ShouldLoad(out bool shouldload) {
            shouldload = true;
        }

        private static void EnsureUniqueNames(On.RoR2.ContentManagement.ContentManager.orig_SetContentPacks orig, List<ReadOnlyContentPack> newContentPacks) {
            if (newContentPacks.Count > 1) {
                IEnumerable<PropertyInfo> allReadOnlyNamedAssetCollectionProperty = GetAllAssetCollectionPropertiesOfAReadOnlyContentPack();

                // Compare each content pack with each others for any potential duplicate asset names
                for (int i = 0; i < newContentPacks.Count - 1; i++) {
                    for (int j = i + 1; j < newContentPacks.Count; j++) {

                        var firstContentPack = newContentPacks[i];
                        var secondContentPack = newContentPacks[j];

                        var isFirstContentPackVanilla = firstContentPack.IsVanillaContentPack();
                        var isSecondContentPackVanilla = secondContentPack.IsVanillaContentPack();

                        foreach (var assetCollectionProperty in allReadOnlyNamedAssetCollectionProperty) {
                            var firstAssetCollection = ((IEnumerable)assetCollectionProperty.GetValue(firstContentPack)).Cast<UnityObject>();
                            var secondAssetCollection = ((IEnumerable)assetCollectionProperty.GetValue(secondContentPack)).Cast<UnityObject>();

                            var firstAssetIndex = 0;
                            foreach (var firstAsset in firstAssetCollection) {
                                var secondAssetIndex = 0;
                                foreach (var secondAsset in secondAssetCollection) {
                                    var differentReferences = firstAsset != secondAsset;
                                    if (differentReferences) {
                                        ChangeAssetNameIfNeeded(firstContentPack, firstAsset, ref firstAssetIndex,
                                            secondContentPack, secondAsset, ref secondAssetIndex,
                                            isFirstContentPackVanilla, isSecondContentPackVanilla);
                                    }
                                    else {
                                        R2API.Logger.LogError($"The exact same asset {firstAsset} is being added by two different content packs : {firstContentPack.identifier} and {secondContentPack.identifier}");
                                        // Todo, try removing it from the non-vanilla contentPack, lot of annoying code to write that I cant bother writing right now
                                    }
                                }
                            }
                        }
                    }
                }
            }

            orig(newContentPacks);
        }

        private static void ChangeAssetNameIfNeeded(ReadOnlyContentPack firstContentPack, UnityObject firstAsset, ref int firstAssetIndex,
            ReadOnlyContentPack secondContentPack, UnityObject secondAsset, ref int secondAssetIndex,
            bool isFirstContentPackVanilla, bool isSecondContentPackVanilla) {
            if (firstAsset.name.Equals(secondAsset.name, StringComparison.InvariantCulture)) {
                if (isFirstContentPackVanilla) {
                    ChangeAssetName(secondContentPack, ref secondAssetIndex, secondAsset, firstContentPack);
                }
                else if (isSecondContentPackVanilla) {
                    ChangeAssetName(firstContentPack, ref firstAssetIndex, firstAsset, secondContentPack);
                }
            }
        }

        private static bool IsVanillaContentPack(this ReadOnlyContentPack contentPack) {
            return contentPack.identifier.StartsWith("RoR2.");
        }

        private static void ChangeAssetName(ReadOnlyContentPack changingContentPack, ref int assetIndex, UnityObject changingAsset, ReadOnlyContentPack notChangingContentPack) {
            var newName = $"{changingContentPack.identifier}_{changingAsset.name}_{assetIndex++}";

            R2API.Logger.LogWarning($"Asset name from {changingContentPack.identifier} is conflicting with {notChangingContentPack.identifier}. " +
                $"Old name : {changingAsset.name}, new name : {newName}");
            changingAsset.name = newName;
        }

        private static IEnumerable<PropertyInfo> GetAllAssetCollectionPropertiesOfAReadOnlyContentPack() {
            const BindingFlags allFlags = (BindingFlags)(-1);

            var allReadOnlyNamedAssetCollectionProperty =
                typeof(ReadOnlyContentPack).GetProperties(allFlags).
                Where(
                    p => p.PropertyType.GenericTypeArguments.Length > 0 &&
                    (p.PropertyType.GenericTypeArguments[0].IsSubclassOf(typeof(UnityObject)) ||
                    typeof(UnityObject) == p.PropertyType.GenericTypeArguments[0]));

            return allReadOnlyNamedAssetCollectionProperty;
        }

        internal static void HandleContentAddition(Assembly assembly, UnityObject content) {
            R2APISerializableContentPack scp = GetOrCreateSerializableContentPack(assembly);
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
            R2APISerializableContentPack scp = GetOrCreateSerializableContentPack(assembly);
            if (scp) {
                AddSafeType(ref scp.entityStateTypes, new SerializableEntityStateType(type), scp.name);
            }
        }

        private static void HandleGameObject(GameObject go, R2APISerializableContentPack scp) {
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
            if (!_contentPacksCreated) {
                R2API.Logger.LogInfo($"Generating a total of {BepInModNameToSerializableContentPack.Values.Count} ContentPacks...");
                List<ManagedReadOnlyContentPack> managedReadOnlyContentPacks = new List<ManagedReadOnlyContentPack>();
                foreach (var (modName, managedSCP) in BepInModNameToSerializableContentPack) {
                    managedReadOnlyContentPacks.Add(new ManagedReadOnlyContentPack(managedSCP.serializableContentPack, managedSCP.AutoCreateIContentPackProvider, managedSCP.AssemblyThatCreatedContentPack));
                    ContentPackToAssembly.Add(managedSCP.serializableContentPack.GetOrCreateContentPack(), managedSCP.AssemblyThatCreatedContentPack);
                }
                _contentPacksCreated = true;
                _managedContentPacks = new ReadOnlyArray<ManagedReadOnlyContentPack>(managedReadOnlyContentPacks.ToArray());
                OnContentPacksCreated?.Invoke();
            }
            else {
                throw new InvalidOperationException($"The Content Pack collection has already been created!");
            }
        }
        #endregion

        #region Util
        internal static R2APISerializableContentPack GetOrCreateSerializableContentPack(Assembly assembly) {
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
                    R2APISerializableContentPack serializableContentPack;
                    //If this assembly does not have a content pack assigned to it, create a new one and add it to the dictionary
                    if (!BepInModNameToSerializableContentPack.ContainsKey(modName)) {
                        serializableContentPack = ScriptableObject.CreateInstance<R2APISerializableContentPack>();
                        serializableContentPack.name = modName;
                        BepInModNameToSerializableContentPack.Add(modName, new ManagedSerializableContentPack(serializableContentPack, true, assembly));
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

        private static void AddSafe<T>(ref T[] assetArray, T asset, string identifier) where T : UnityObject {
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
    }
}
