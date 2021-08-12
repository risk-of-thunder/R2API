using BepInEx;
using MonoMod.RuntimeDetour;
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
    /// API for adding sounds and music with Wwise
    /// </summary>
    [R2APISubmodule]
    public static class SoundAPI {

        private static readonly List<NetworkSoundEventDef> NetworkSoundEventDefs = new List<NetworkSoundEventDef>();

        private static bool _NetworkSoundEventCatalogInitialized;

        public static bool Loaded {
            get; private set;
        }

        #region Soundbank Setup

        [R2APISubmoduleInit(Stage = InitStage.SetHooks)]
        internal static void SoundAwake() {
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

            _ = new Hook(
                typeof(AkWwiseInitializationSettings).GetMethodCached(nameof(AkWwiseInitializationSettings.InitializeSoundEngine)),
                typeof(SoundAPI).GetMethodCached(nameof(AddBanksAfterEngineInit)));
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
        internal static void SetHooks() {
            R2APIContentPackProvider.WhenContentPackReady += AddNetworkSoundEventDefsToGame;
        }

        [R2APISubmoduleInit(Stage = InitStage.UnsetHooks)]
        internal static void UnsetHooks() {
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
                throw new InvalidOperationException($"{nameof(SoundAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(SoundAPI)})]");
            }

            if (_NetworkSoundEventCatalogInitialized) {
                R2API.Logger.LogError(
                    $"Too late ! Tried to add network sound event: {networkSoundEventDef.eventName} after the network sound event def list was created");
                return false;
            }

            if (NetworkSoundEventDefs.Contains(networkSoundEventDef) || NetworkSoundEventDefs.Any(networkSoundEvent => networkSoundEvent.eventName == networkSoundEventDef.eventName)) {
                R2API.Logger.LogError($"NetworkSoundEventDef or NetworkSoundEventDef with EventName: {networkSoundEventDef.eventName} already exists in the catalog! Consider changing your event name to avoid the collision. Aborting!");
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
                    $"Too late! Tried to add network sound event: {eventName} after the network sound event def list was created");
                return false;
            }

            var networkSoundEventDef = ScriptableObject.CreateInstance<NetworkSoundEventDef>();
            networkSoundEventDef.eventName = eventName;

            if (NetworkSoundEventDefs.Any(networkSoundEvent => networkSoundEvent.eventName == eventName)) {
                R2API.Logger.LogError($"NetworkSoundEventDef with Event Name: {eventName} already exists in the catalog! Consider changing your event name to avoid the collision. Aborting!");
                return false;
            }

            NetworkSoundEventDefs.Add(networkSoundEventDef);
            return true;
        }

        #endregion NetworkSoundEventCatalog Setup

    }
}
