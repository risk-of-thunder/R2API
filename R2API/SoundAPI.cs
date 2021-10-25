using BepInEx;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using R2API.MiscHelpers;
using R2API.Utils;
using RoR2;
using RoR2.ContentManagement;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace R2API {

    /// <summary>
    /// API for adding sounds with Wwise
    /// </summary>
    [R2APISubmodule]
    public static class SoundAPI {

        /// <summary>
        /// Return true if the submodule is loaded.
        /// </summary>
        public static bool Loaded {
            get => _loaded;
            internal set => _loaded = value;
        }

        private static bool _loaded;

        private static Hook AddBanksAfterEngineInitHook;

        private static readonly List<NetworkSoundEventDef> NetworkSoundEventDefs = new List<NetworkSoundEventDef>();

        private static bool _NetworkSoundEventCatalogInitialized;

        #region Soundbank Setup

        [R2APISubmoduleInit(Stage = InitStage.SetHooks)]
        internal static void SetHooks() {
            // Disable SoundPlus if RoR2 is running with its graphics and sound engine disabled (Dedicated Servers) to avoid any bad side effects.
            if (Application.isBatchMode)
                return;

            if (Loaded) {
                return;
            }

            Loaded = true;

            var files = Directory.GetFiles(Paths.PluginPath, "*.sound", SearchOption.AllDirectories);

            foreach (var file in files) {
                SoundBanks.Add(file);
            }

            AddBanksAfterEngineInitHook = new Hook(
                typeof(AkWwiseInitializationSettings).GetMethodCached(nameof(AkWwiseInitializationSettings.InitializeSoundEngine)),
                typeof(SoundAPI).GetMethodCached(nameof(AddBanksAfterEngineInit)));

            Music.SetHooks();
        }

        [R2APISubmoduleInit(Stage = InitStage.UnsetHooks)]
        internal static void UnsetHooks() {
            AddBanksAfterEngineInitHook.Dispose();

            Music.UnsetHooks();
        }

        private static bool AddBanksAfterEngineInit(Func<bool> orig) {
            var res = orig();

            LoadBanks();

            return res;
        }

        [R2APISubmoduleInit(Stage = InitStage.LoadCheck)]
        private static void ShouldLoad(out bool shouldload) {
            shouldload = Directory.GetFiles(Paths.PluginPath, "*.sound", SearchOption.AllDirectories).Length > 0;
        }

        /// <summary>
        /// Loads all the banks, called after wwise engine init
        /// </summary>
        private static void LoadBanks() {
            var failedBanks = new List<SoundBanks.Bank>();
            var loadedABank = false;
            foreach (var bank in SoundBanks.soundBanks) {
                if (!bank.Load()) {
                    failedBanks.Add(bank);
                }
                else {
                    loadedABank = true;
                }
            }

            foreach (var bank in failedBanks) {
                SoundBanks.soundBanks.Remove(bank);
            }

            SoundBanks.Loaded = true;

            if (loadedABank) {
                R2API.Logger.LogInfo("Custom sound banks loaded.");
            }
        }

        /// <summary>
        /// class for SoundBanks to load
        /// </summary>
        public static class SoundBanks {

            /// <summary>
            /// Makes sure to correctly load banks added before or after RoR2.RoR2Application.OnLoad()
            /// </summary>
            internal static bool Loaded = false;

            /// <summary>
            /// Adds a soundbank to load, returns the ID used for unloading
            /// </summary>
            /// <param name="bank">byte array of the entire .bnk file</param>
            public static uint Add(byte[]? bank) {
                var bankToAdd = new Bank(bank);
                if (Loaded) {
                    if (bankToAdd.Load()) {
                        soundBanks.Add(bankToAdd);
                    }
                }
                else {
                    soundBanks.Add(bankToAdd);
                }
                return bankToAdd.PublicID;
            }

            /// <summary>
            /// Adds an external soundbank to load, returns the ID used for unloading (.sound files are loaded automatically)
            /// </summary>
            /// <param name="path">the absolute path to the file</param>
            public static uint Add(string? path) {
                byte[] bank = File.ReadAllBytes(path);

                return Add(bank);
            }

            /// <summary>
            /// Unloads an bank using the ID (ID is returned at the Add() of the bank)
            /// </summary>
            /// <param name="ID">BankID</param>
            /// <returns></returns>
            public static AKRESULT Remove(uint ID) {
                var bankToUnload = soundBanks.Find(bank => bank.PublicID == ID);
                return bankToUnload.UnLoad();
            }

            /// <summary>
            /// Class containing all the information of a bank
            /// </summary>
            internal class Bank {

                internal Bank(byte[] bankData) {
                    BankData = bankData;
                    PublicID = _bankIteration++;
                }

                /// <summary>
                /// Number keeping track of PublicID to give
                /// </summary>
                private static uint _bankIteration = 0;

                /// <summary>
                /// BankData supplied by the user
                /// </summary>
                internal byte[] BankData;

                /// <summary>
                /// Identifier for the User
                /// </summary>
                internal uint PublicID;

                /// <summary>
                /// Pointer for the wwise engine
                /// </summary>
                internal IntPtr Memory;

                /// <summary>
                /// Identifier for the engine
                /// </summary>
                internal uint BankID;

                /// <summary>
                /// Loads the bank into the wwise engine
                /// </summary>
                /// <returns>True if the bank successfully loaded, false otherwise</returns>
                internal bool Load() {
                    //Creates IntPtr of sufficient size.
                    Memory = Marshal.AllocHGlobal(BankData.Length);

                    //copies the byte array to the IntPtr
                    Marshal.Copy(BankData, 0, Memory, BankData.Length);

                    //Loads the entire IntPtr as a bank
                    var result = AkSoundEngine.LoadBank(Memory, (uint)BankData.Length, out BankID);
                    if (result != AKRESULT.AK_Success) {
                        Debug.LogError("WwiseUnity: AkMemBankLoader: bank loading failed with result " + result);
                        return false;
                    }

                    //BankData is now copied to Memory so is unnecassary
                    BankData = null;
                    return true;
                }

                /// <summary>
                /// Unloads the bank from the wwise engine
                /// </summary>
                /// <returns>The AKRESULT of unloading itself</returns>
                internal AKRESULT UnLoad() {
                    var result = AkSoundEngine.UnloadBank(BankID, Memory);
                    if (result != AKRESULT.AK_Success) {
                        Debug.LogError("Failed to unload bank " + PublicID.ToString() + ": " + result.ToString());
                        return result;
                    }
                    Marshal.FreeHGlobal(Memory);
                    soundBanks.Remove(this);
                    return result;
                }
            }

            /// <summary>
            /// List of all the Banks
            /// </summary>
            internal static List<Bank> soundBanks = new List<Bank>();
        }

        #endregion Soundbank Setup

        #region NetworkSoundEventCatalog Setup

        [R2APISubmoduleInit(Stage = InitStage.SetHooks)]
        internal static void NetworkSetHooks() {
            R2APIContentPackProvider.WhenContentPackReady += AddNetworkSoundEventDefsToGame;
        }

        [R2APISubmoduleInit(Stage = InitStage.UnsetHooks)]
        internal static void NetworkUnsetHooks() {
            R2APIContentPackProvider.WhenContentPackReady -= AddNetworkSoundEventDefsToGame;
        }

        private static void AddNetworkSoundEventDefsToGame(ContentPack r2apiContentPack) {
            foreach (var networkSoundEventDef in NetworkSoundEventDefs) {
                R2API.Logger.LogInfo($"Custom Network Sound Event: {networkSoundEventDef.eventName} added");
            }

            r2apiContentPack.networkSoundEventDefs.Add(NetworkSoundEventDefs.ToArray());
            _NetworkSoundEventCatalogInitialized = true;
        }

        /// <summary>
        /// Add a custom network sound event to the list of available network sound events.
        /// If this is called after the NetworkSoundEventCatalog inits then this will return false and ignore the custom network sound event.
        /// </summary>
        /// <param name="networkSoundEventDef">The network sound event def to add.</param>
        /// <returns>true if added, false otherwise</returns>
        public static bool AddNetworkedSoundEvent(NetworkSoundEventDef? networkSoundEventDef) {
            if (!Loaded) {
                throw new InvalidOperationException($"{nameof(SoundAPI)} is not loaded. " +
                    $"Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(SoundAPI)})]");
            }

            if (_NetworkSoundEventCatalogInitialized) {
                R2API.Logger.LogError(
                    "Too late ! " +
                    "Tried to add network sound event: " +
                    $"{networkSoundEventDef.eventName} " +
                    "after the network sound event def list was created");
                return false;
            }

            if (NetworkSoundEventDefs.Contains(networkSoundEventDef) ||
                NetworkSoundEventDefs.Any(n => n.eventName == networkSoundEventDef.eventName)) {
                R2API.Logger.LogError(
                    "NetworkSoundEventDef or NetworkSoundEventDef " +
                    $"with EventName: {networkSoundEventDef.eventName} " +
                    $"already exists in the catalog! " +
                    $"Consider changing your event name to avoid the collision. Aborting!");
                return false;
            }

            NetworkSoundEventDefs.Add(networkSoundEventDef);
            return true;
        }

        /// <summary>
        /// Add a custom network sound event to the list of available network sound events.
        /// If this is called after the NetworkSoundEventCatalog inits then this will return false and ignore the custom network sound event.
        /// </summary>
        /// <param name="eventName">The name of the AKWwise Sound Event to add.</param>
        /// <returns>true if added, false otherwise</returns>
        public static bool AddNetworkedSoundEvent(string eventName) {
            if (!Loaded) {
                throw new InvalidOperationException($"{nameof(SoundAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(SoundAPI)})]");
            }

            if (_NetworkSoundEventCatalogInitialized) {
                R2API.Logger.LogError(
                    $"Too late! Tried to add network sound event: {eventName} " +
                    "after the network sound event def list was created");
                return false;
            }

            var networkSoundEventDef = ScriptableObject.CreateInstance<NetworkSoundEventDef>();
            networkSoundEventDef.eventName = eventName;

            if (NetworkSoundEventDefs.Any(n => n.eventName == eventName)) {
                R2API.Logger.LogError(
                    $"NetworkSoundEventDef with Event Name: {eventName} " +
                    "already exists in the catalog! " +
                    "Consider changing your event name to avoid the collision. Aborting!");
                return false;
            }

            NetworkSoundEventDefs.Add(networkSoundEventDef);
            return true;
        }

        #endregion NetworkSoundEventCatalog Setup

        /// <summary>
        /// Class for adding music to the game music system.
        /// <see href="https://github.com/risk-of-thunder/R2Wiki/wiki/Custom-Music---WWise">Tutorial available here</see>
        /// </summary>
        public static class Music {

            /// <summary>
            /// Class that contains all the needed information
            /// for the api to process your custom tracks into
            /// the game music system.
            /// </summary>
            public class CustomMusicData {

                /// <summary>
                /// Used for logging purposes.
                /// </summary>
                public BepInPlugin BepInPlugin;

                /// <summary>
                /// The event name that is used for firing the whole custom music system.
                /// It must be different than the game one which is <see cref="GameEventNamePlayMusicSystem"></see>.
                /// </summary>
                public string PlayMusicSystemEventName;

                /// <summary>
                /// The absolute path of the folder that contains your init bank and your sound bank.
                /// </summary>
                public string BanksFolderPath;

                /// <summary>
                /// Should be different than the game one which is called <see cref="GameInitBankName"></see>.
                /// </summary>
                public string InitBankName;

                /// <summary>
                /// The name of the sound bank that contains your music tracks.
                /// </summary>
                public string SoundBankName;

                /// <summary>
                /// Dictionary for replacing the main and boss tracks of scene defs.
                /// Can be null.
                /// </summary>
                public Dictionary<SceneDef, IEnumerable<MainAndBossTracks>> SceneDefToTracks;

                internal uint _loadedInitBankId;

                /// <summary>
                /// Will be filled after using <see cref="Add(CustomMusicData)"></see> and after the Wwise sound engine get initialized.
                /// </summary>
                public uint LoadedInitBankId {
                    get => _loadedInitBankId;
                    private set => _loadedInitBankId = value;
                }

                internal uint _loadedSoundBankId;

                /// <inheritdoc cref="LoadedInitBankId"/>
                public uint LoadedSoundBankId {
                    get => _loadedSoundBankId;
                    private set => _loadedSoundBankId = value;
                }
            }

            /// <summary>
            /// Contains the two MusicTrackDef that are fired by the game music system depending on the game current state.
            /// <see cref="MusicTrackDef.states"></see> usually have only two states.
            /// One <see cref="AK.Wwise.State"></see> should serve a similar purpose as the game one called gameplaySongChoice,
            /// which is normally used for telling Wwise which track to play.
            /// That gameplaySongChoice <see cref="AK.Wwise.State"></see> should have a custom group id associated with it which
            /// should be different than the game one called gameplaySongChoice.
            /// The other <see cref="AK.Wwise.State"></see> should have the same group id as the game one which
            /// is currently called Music_system and is used for telling the game if
            /// its either a Gameplay, a Bossfight, or a Menu track.
            /// </summary>
            public struct MainAndBossTracks {

                /// <inheritdoc cref="MainAndBossTracks"/>
                public MusicTrackDef MainTrack;

                /// <inheritdoc cref="MainAndBossTracks"/>
                public MusicTrackDef BossTrack;

                /// <inheritdoc cref="MainAndBossTracks"/>
                public MainAndBossTracks(MusicTrackDef mainTrack, MusicTrackDef bossTrack) {
                    MainTrack = mainTrack;
                    BossTrack = bossTrack;
                }
            }

            /// <summary>
            /// Extended <see cref="MusicTrackDef"/> for a more code based project.
            /// This can also be used to an extent by unity editor users as a baseline for whats needed.
            /// </summary>
            public class CustomMusicTrackDef : MusicTrackDef {

                /// <summary>
                /// Struct for storing the wwise state data
                /// that will be posted to the wwise engine
                /// through <see cref="AkSoundEngine.SetState(uint, uint)"/>
                /// </summary>
                public struct CustomState {

                    /// <summary>
                    /// First arg of <see cref="AkSoundEngine.SetState(uint, uint)"/>
                    /// </summary>
                    public uint GroupId;

                    /// <summary>
                    /// Second arg of <see cref="AkSoundEngine.SetState(uint, uint)"/>
                    /// </summary>
                    public uint StateId;
                }

                /// <summary>
                /// Iterated in the <see cref="Play"/> and <see cref="Stop"/> methods.
                /// </summary>
                public List<CustomState> CustomStates;

                /// <summary>
                /// Used in the <see cref="Preload"/> (which should be called by <see cref="Play"/>).
                /// Can NOT be null or whitespace.
                /// </summary>
                public string SoundBankName;

                /// <summary>
                /// Preload should try to load your music bank with <see cref="SoundBankName"/>
                /// </summary>
                public override void Preload() {
                    if (!string.IsNullOrWhiteSpace(SoundBankName)) {
                        AkSoundEngine.LoadBank(SoundBankName, AkSoundEngine.AK_DEFAULT_POOL_ID, out _);
                    }
                }

                /// <summary>
                /// Call Preload and set the states to the Wwise sound engine.
                /// </summary>
                public override void Play() {
                    Preload();

                    foreach (var customState in CustomStates) {
                        AkSoundEngine.SetState(customState.GroupId, customState.StateId);
                    }
                }

                /// <summary>
                /// Set the states to 0 to the Wwise sound engine
                /// </summary>
                public override void Stop() {
                    foreach (var customState in CustomStates) {
                        AkSoundEngine.SetState(customState.GroupId, 0U);
                    }
                }
            }

            private const string GameMusicBankName = "Music";
            private const string GameInitBankName = "Init";
            private const string GameEventNamePlayMusicSystem = "Play_Music_System";

            private static readonly List<CustomMusicData> CustomMusicDatas = new();

            private static readonly Dictionary<string, BepInPlugin> EventNameToBepinPlugin = new();
            private static readonly HashSet<string> PlayMusicSystemEventNames = new();
            private static readonly Dictionary<SceneDef, MainAndBossTracks> SceneDefToOriginalTracks = new();
            private static readonly Dictionary<SceneDef, List<MainAndBossTracks>> SceneDefToTracks = new();

            private static bool GameMusicBankInUse;

            private static SceneDef LastSceneDef;
            private static MusicController MusicControllerInstance;

            private static bool IsVanillaMusicTrack(MusicTrackDef self) =>
                self && self.soundBank != null && self.soundBank.Name == GameMusicBankName;

            private static Hook AddCustomMusicDatasHook;

            internal static void SetHooks() {
                AddCustomMusicDatasHook = new Hook(
                typeof(AkWwiseInitializationSettings).GetMethodCached(nameof(AkWwiseInitializationSettings.InitializeSoundEngine)),
                typeof(Music).GetMethodCached(nameof(AddCustomMusicDatas)));

                On.RoR2.MusicController.Start += EnableCustomMusicSystems;
                SceneCatalog.onMostRecentSceneDefChanged += OnSceneChangeReplaceMusic;

                IL.RoR2.MusicController.LateUpdate += PauseMusicIfGameMusicBankNotInUse;

                IL.RoR2.MusicTrackOverride.PickMusicTrack += CheckIfTrackOverrideIsVanilla;
            }

            internal static void UnsetHooks() {
                AddCustomMusicDatasHook.Dispose();

                On.RoR2.MusicController.Start -= EnableCustomMusicSystems;
                SceneCatalog.onMostRecentSceneDefChanged -= OnSceneChangeReplaceMusic;

                IL.RoR2.MusicController.LateUpdate -= PauseMusicIfGameMusicBankNotInUse;

                IL.RoR2.MusicTrackOverride.PickMusicTrack -= CheckIfTrackOverrideIsVanilla;
            }

            private static bool AddCustomMusicDatas(Func<bool> orig) {
                var res = orig();

                foreach (var data in CustomMusicDatas) {
                    var akResult = AkSoundEngine.AddBasePath(data.BanksFolderPath);
                    if (akResult != AKRESULT.AK_Success) {
                        R2API.Logger.LogError(
                            $"Error adding base path : {data.BanksFolderPath}. " +
                            $"Error code : {akResult}");
                        continue;
                    }

                    akResult = AkSoundEngine.LoadBank(data.InitBankName, AkSoundEngine.AK_DEFAULT_POOL_ID, out data._loadedInitBankId);
                    if (akResult != AKRESULT.AK_Success) {
                        R2API.Logger.LogError(
                            $"Error loading init bank : {data.InitBankName}. " +
                            $"Error code : {akResult}");
                        continue;
                    }

                    akResult = AkSoundEngine.LoadBank(data.SoundBankName, AkSoundEngine.AK_DEFAULT_POOL_ID, out data._loadedSoundBankId);
                    if (akResult != AKRESULT.AK_Success) {
                        R2API.Logger.LogError(
                            $"Error loading sound bank : {data.SoundBankName}. " +
                            $"Error code : {akResult}");
                        continue;
                    }

                    AddCustomTracksToDictionary(data);
                }

                return res;
            }

            private static void AddCustomTracksToDictionary(CustomMusicData data) {
                if (data.SceneDefToTracks != null) {
                    foreach (var (sceneDef, customTracks) in data.SceneDefToTracks) {
                        if (SceneDefToTracks.TryGetValue(sceneDef, out var existingCustomTracks)) {
                            existingCustomTracks.AddRange(customTracks);
                        }
                        else {
                            var allCustomTracksForThatScene = new List<MainAndBossTracks>();
                            allCustomTracksForThatScene.AddRange(customTracks);
                            SceneDefToTracks.Add(sceneDef, allCustomTracksForThatScene);
                        }
                    }
                }
            }

            private static void EnableCustomMusicSystems(On.RoR2.MusicController.orig_Start orig, MusicController self) {
                orig(self);

                foreach (var playMusicSystemEventName in PlayMusicSystemEventNames) {
                    AkSoundEngine.PostEvent(playMusicSystemEventName, self.gameObject);
                }

                MusicControllerInstance = self;
                GameMusicBankInUse = true;
            }

            private static void OnSceneChangeReplaceMusic(SceneDef sceneDef) {
                if (!sceneDef) {
                    return;
                }

                ReplaceSceneMusicWithCustomTracks(sceneDef);

                UpdateIsGameMusicBankInUse(sceneDef);

                LastSceneDef = sceneDef;
            }

            private static void ReplaceSceneMusicWithCustomTracks(SceneDef sceneDef) {
                if (SceneDefToTracks.TryGetValue(sceneDef, out var customTracks)) {
                    if (!SceneDefToOriginalTracks.ContainsKey(sceneDef)) {
                        var originalTracks = new MainAndBossTracks(sceneDef.mainTrack, sceneDef.bossTrack);
                        SceneDefToOriginalTracks.Add(sceneDef, originalTracks);
                    }

                    if (customTracks.Count > 0) {
                        var selectedTracks = customTracks[RoR2Application.rng.RangeInt(0, customTracks.Count)];

                        if (selectedTracks.MainTrack) {
                            if (IsVanillaMusicTrack(sceneDef.mainTrack) ||
                                LastSceneDef && IsVanillaMusicTrack(LastSceneDef.mainTrack)) {
                                GameMusicBankInUse = false;
                            }

                            sceneDef.mainTrack = selectedTracks.MainTrack;
                        }
                        if (selectedTracks.BossTrack) {
                            sceneDef.bossTrack = selectedTracks.BossTrack;
                        }
                    }
                }
            }

            private static void UpdateIsGameMusicBankInUse(SceneDef sceneDef) {
                if (IsVanillaMusicTrack(sceneDef.mainTrack) ||
                    IsVanillaMusicTrack(sceneDef.bossTrack)) {
                    GameMusicBankInUse = true;
                }
            }

            private static void PauseMusicIfGameMusicBankNotInUse(ILContext il) {
                var cursor = new ILCursor(il);

                static bool PauseMusicIfGameMusicBankNotInUse(bool b) {
                    if (b)
                        return true;

                    return !GameMusicBankInUse;
                }

                cursor.GotoNext(i => i.MatchStloc(out _));
                cursor.EmitDelegate<Func<bool, bool>>(PauseMusicIfGameMusicBankNotInUse);
            }

            private static void CheckIfTrackOverrideIsVanilla(ILContext il) {
                var cursor = new ILCursor(il);

                static MusicTrackDef CheckIfTrackOverrideIsVanilla(MusicTrackDef overrideTrack) {
                    if (overrideTrack) {
                        GameMusicBankInUse = IsVanillaMusicTrack(overrideTrack);
                    }

                    return overrideTrack;
                }

                if (cursor.TryGotoNext(
                    i => i.MatchLdfld<MusicTrackOverride>(nameof(MusicTrackOverride.track)),
                    i => i.MatchStindRef())) {
                    cursor.Index++;
                    cursor.EmitDelegate<Func<MusicTrackDef, MusicTrackDef>>(CheckIfTrackOverrideIsVanilla);
                }
                else {
                    R2API.Logger.LogError("Failed finding IL Instructions. " +
                        $"Aborting {nameof(MusicTrackOverride.PickMusicTrack)} IL Hook");
                }
            }

            /// <summary>
            /// Please refer to the <see cref="CustomMusicData"/> fields documentation
            /// for indication on how to fill the fields properly.
            /// </summary>
            /// <returns>True if the preliminary checks succeed</returns>
            public static bool Add(CustomMusicData data) {
                if (data.BepInPlugin == null) {
                    throw new ArgumentNullException(nameof(CustomMusicData) + "." + nameof(CustomMusicData.BepInPlugin));
                }

                if (data.InitBankName == GameInitBankName) {
                    R2API.Logger.LogError(
                        "Error loading custom init bank. " +
                        "Called the same as the game Init Bank. " +
                        "The name must be different.");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(data.InitBankName)) {
                    R2API.Logger.LogError(
                        "Error loading custom init bank. " +
                        "Should not be empty.");
                    return false;
                }

                if (data.PlayMusicSystemEventName == GameEventNamePlayMusicSystem) {
                    R2API.Logger.LogError(
                        "Error adding the play music system event name. " +
                        "Called the same as the game play music system event name. " +
                        "The name must be different.");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(data.PlayMusicSystemEventName)) {
                    R2API.Logger.LogError(
                        "Error adding the play music system event name. " +
                        "Should not be empty.");
                    return false;
                }

                if (!PlayMusicSystemEventNames.Add(data.PlayMusicSystemEventName)) {
                    R2API.Logger.LogError(
                        $"Error adding playMusicSystemEventName : {data.PlayMusicSystemEventName}. " +
                        $"Already in use by {EventNameToBepinPlugin[data.PlayMusicSystemEventName].GUID}.");
                    return false;
                }
                EventNameToBepinPlugin.Add(data.PlayMusicSystemEventName, data.BepInPlugin);

                CustomMusicDatas.Add(data);

                return true;
            }

            /// <summary>
            /// Remove everything related to your custom music data.
            /// Both the sound banks get unloaded.
            /// The scene tracks that were override by your dictionary are restored.
            /// </summary>
            /// <param name="data"></param>
            /// <returns></returns>
            public static bool Remove(CustomMusicData data) {
                var akResult = AkSoundEngine.UnloadBank(data.LoadedSoundBankId, IntPtr.Zero);
                if (akResult != AKRESULT.AK_Success) {
                    R2API.Logger.LogError(
                        $"Error unloading sound bank : {data.SoundBankName}. " +
                        $"Error code : {akResult}");
                    return false;
                }

                akResult = AkSoundEngine.UnloadBank(data.LoadedInitBankId, IntPtr.Zero);
                if (akResult != AKRESULT.AK_Success) {
                    R2API.Logger.LogError(
                        $"Error unloading init bank : {data.InitBankName}. " +
                        $"Error code : {akResult}");
                    return false;
                }

                PlayMusicSystemEventNames.Remove(data.PlayMusicSystemEventName);
                EventNameToBepinPlugin.Remove(data.PlayMusicSystemEventName);

                RemoveTracksFromThatBankFromTheScenes(data);

                return CustomMusicDatas.Remove(data);
            }

            private static void RemoveTracksFromThatBankFromTheScenes(CustomMusicData data) {
                if (data.SceneDefToTracks != null) {
                    foreach (var (sceneDef, customTracksList) in data.SceneDefToTracks) {
                        var allCustomTracksForThatScene = SceneDefToTracks[sceneDef];
                        foreach (var customTracks in customTracksList) {
                            allCustomTracksForThatScene.Remove(customTracks);

                            RestoreOriginalTracksIfNeeded(customTracks);
                        }
                    }
                }
            }

            private static void RestoreOriginalTracksIfNeeded(MainAndBossTracks customTracks) {
                foreach (var scene in SceneCatalog.allSceneDefs) {
                    if (scene.mainTrack == customTracks.MainTrack) {
                        scene.mainTrack = SceneDefToOriginalTracks[scene].MainTrack;

                        GameMusicBankInUse = true;
                    }
                    if (scene.bossTrack == customTracks.BossTrack) {
                        scene.bossTrack = SceneDefToOriginalTracks[scene].BossTrack;

                        GameMusicBankInUse = true;
                    }
                }
            }
        }
    }
}
