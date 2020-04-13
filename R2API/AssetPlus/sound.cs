using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using RoR2;
using UnityEngine;

namespace R2API.AssetPlus {
    /// <summary>
    /// class for SoundBanks to load
    /// </summary>
    [Obsolete("Moved to R2API/SoundAPI/")]
    public static class SoundBanks
    {
        /// <summary>
        /// Adds a soundbank to load, returns the ID used for unloading
        /// </summary>
        /// <param name="bank">byte array of the entire .bnk file</param>
        [Obsolete("Moved to R2API/SoundAPI/SoundBanks")]
        public static uint Add(byte[] bank)
        {
            if (!SoundAPI.Loaded) {
                SoundAPI.SoundAwake();
            }
            return SoundAPI.SoundBanks.Add(bank);
        }

        /// <summary>
        /// Adds an external soundbank to load, returns the ID used for unloading (.sound files are loaded automatically)
        /// </summary>
        /// <param name="path">the absolute path to the file</param>
        [Obsolete("Moved to R2API/SoundAPI/SoundBanks")]
        public static uint Add(string path)
        {
            if (!SoundAPI.Loaded) {
                SoundAPI.SoundAwake();
            }
            return SoundAPI.SoundBanks.Add(path);
        }

        /// <summary>
        /// Unloads an bank using the ID (ID is returned at the Add() of the bank)
        /// </summary>
        /// <param name="ID">BankID</param>
        /// <returns></returns>
        [Obsolete("Moved to R2API/SoundAPI/SoundBanks")]
        public static AKRESULT Remove(uint ID)
        {
            if (!SoundAPI.Loaded) {
                SoundAPI.SoundAwake();
            }
            return SoundAPI.SoundBanks.Remove(ID);
        }
    }
}
