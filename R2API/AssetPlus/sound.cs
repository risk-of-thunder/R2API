using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using RoR2;
using UnityEngine;

namespace R2API.AssetPlus {
    internal static class SoundPlus
    {
        internal static void SoundAwake()
        {
            // Disable SoundPlus if RoR2 is running with its graphics and sound engine disabled (Dedicated Servers) to avoid any bad side effects.
            if (Application.isBatchMode)
                return;

            var files = AssetPlus.GetFiles("*.sound");

            foreach (var file in files)
            {
                SoundBanks.Add(file);
            }

            //for bank loading
            On.RoR2.RoR2Application.OnLoad += RoR2Application_OnLoad;
        }

        private static void RoR2Application_OnLoad(On.RoR2.RoR2Application.orig_OnLoad orig, RoR2Application self)
        {
            orig(self);
            LoadBanks();
        }

        /// <summary>
        /// Loads all the banks, can only be called once and after RoR2.RoR2Application.OnLoad because of the initialization of the init bank
        /// </summary>
        private static void LoadBanks()
        {
            foreach (var bank in SoundBanks.soundBanks)
            {
                bank.Load();
            }

            SoundBanks.Loaded = true;
        }
    }

    /// <summary>
    /// class for SoundBanks to load
    /// </summary>
    public static class SoundBanks
    {
        /// <summary>
        /// Makes sure to correctly load banks added before or after RoR2.RoR2Application.OnLoad()
        /// </summary>
        internal static bool Loaded = false;

        /// <summary>
        /// Adds a soundbank to load, returns the ID used for unloading
        /// </summary>
        /// <param name="bank">byte array of the entire .bnk file</param>
        public static uint Add(byte[] bank)
        {
            var bankToAdd= new Bank(bank);
            soundBanks.Add(bankToAdd);
            if (Loaded)
            {
                bankToAdd.Load();
            }
            return bankToAdd.PublicID;
        }

        /// <summary>
        /// Adds an external soundbank to load, returns the ID used for unloading (.sound files are loaded automatically)
        /// </summary>
        /// <param name="path">the absolute path to the file</param>
        public static uint Add(string path)
        {
            byte[] bank = File.ReadAllBytes(path);

            return Add(bank);
        }

        /// <summary>
        /// Unloads an bank using the ID (ID is returned at the Add() of the bank)
        /// </summary>
        /// <param name="ID">BankID</param>
        /// <returns></returns>
        public static AKRESULT Remove(uint ID)
        {
            var bankToUnload = soundBanks.Find(bank => bank.PublicID == ID);
            return bankToUnload.UnLoad();
        }

        /// <summary>
        /// Class containing all the information of a bank
        /// </summary>
        internal class Bank
        {
            internal Bank(byte[] bankData)
            {
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
            internal void Load()
            {
                //Creates IntPtr of sufficient size.
                Memory = Marshal.AllocHGlobal(BankData.Length);

                //copies the byte array to the IntPtr
                Marshal.Copy(BankData, 0, Memory, BankData.Length);

                //Loads the entire IntPtr as a bank
                var result = AkSoundEngine.LoadBank(Memory, (uint)BankData.Length, out BankID);
                if (result != AKRESULT.AK_Success)
                {
                    Debug.LogError("WwiseUnity: AkMemBankLoader: bank loading failed with result " + result);
                    soundBanks.Remove(this);
                }

                //BankData is now copied to Memory so is unnecassary
                BankData = null;
            }

            /// <summary>
            /// Unloads the bank from the wwise engine
            /// </summary>
            /// <returns>The AKRESULT of unloading itself</returns>
            internal AKRESULT UnLoad()
            {
                var result = AkSoundEngine.UnloadBank(BankID, Memory);
                if (result != AKRESULT.AK_Success)
                {
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
}
