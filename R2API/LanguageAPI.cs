using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using BepInEx;
using MonoMod.Utils;
using R2API.Utils;
using RoR2;
using SimpleJSON;
using UnityEngine;

namespace R2API {
    /// <summary>
    /// class for language files to load
    /// </summary>
    [R2APISubmodule]
    public static class LanguageAPI {
        public static bool Loaded {
            get; private set;
        }

        [R2APISubmoduleInit(Stage = InitStage.SetHooks)]
#pragma warning disable IDE0051 // Remove unused private members
        private static void LanguageAwake() {
#pragma warning restore IDE0051 // Remove unused private members
            if (Loaded) {
                return;
            }

            Loaded = true;

            var languagePaths = Directory.GetFiles(Paths.PluginPath, "*.language", SearchOption.AllDirectories);
            foreach (var path in languagePaths) {
                AddPath(path);
            }
        }

        [R2APISubmoduleInit(Stage = InitStage.LoadCheck)]
#pragma warning disable IDE0051 // Remove unused private members
        private static void ShouldLoad(out bool shouldload) {
#pragma warning restore IDE0051 // Remove unused private members
            shouldload = Directory.GetFiles(Paths.PluginPath, "*.language", SearchOption.AllDirectories).Length > 0;
        }

        /// <summary>
        /// Adds a single languagetoken and its associated value to all languages
        /// </summary>
        /// <param name="key">Token the game asks</param>
        /// <param name="value">Value it gives back</param>
        public static void Add(string? key, string? value) {
        }

        /// <summary>
        /// Adds a single languagetoken and value to a specific language
        /// </summary>
        /// <param name="key">Token the game asks</param>
        /// <param name="value">Value it gives back</param>
        /// <param name="language">Language you want to add this to</param>
        public static void Add(string? key, string? value, string? language) { 
        }

        /// <summary>
        /// adding an file via path (.language is added automatically)
        /// </summary>
        /// <param name="path">absolute path to file</param>
        public static void AddPath(string? path) {
        }

        /// <summary>
        /// Adding an file which is read into an string
        /// </summary>
        /// <param name="file">entire file as string</param>
        public static void Add(string? file) {
        }

        /// <summary>
        /// Adds multiple languagetokens and value
        /// </summary>
        /// <param name="tokenDictionary">dictionaries of key-value (eg ["mytoken"]="mystring")</param>
        public static void Add(Dictionary<string, string?>? tokenDictionary) {
        }

        /// <summary>
        /// Adds multiple languagetokens and value to a specific language
        /// </summary>
        /// <param name="tokenDictionary">dictionaries of key-value (eg ["mytoken"]="mystring")</param>
        /// <param name="language">Language you want to add this to</param>
        public static void Add(Dictionary<string, string?>? tokenDictionary, string? language) {
        }

        /// <summary>
        /// Adds multiple languagetokens and value to languages
        /// </summary>
        /// <param name="languageDictionary">dictionary of languages containing dictionaries of key-value (eg ["en"]["mytoken"]="mystring")</param>
        public static void Add(Dictionary<string, Dictionary<string, string?>?>? languageDictionary) {
        }

        /// <summary>
        /// Manages temporary language token changes.
        /// </summary>
        public class LanguageOverlay {
            /// <summary>Contains information about the language token changes this LanguageOverlay makes.</summary>
            public readonly ReadOnlyCollection<OverlayTokenData> readOnlyOverlays;

            internal void Add() {
            }

            /// <summary>Undoes this LanguageOverlay's language token changes; you may safely dispose it afterwards. Requires a language reload to take effect.</summary>
            public void Remove() {
            }
        }

        /// <summary>
        /// Adds a single temporary language token, and its associated value, to all languages. Please add multiple instead (dictionary- or file-based signatures) where possible. Language-specific tokens, as well as overlays added later in time, will take precedence. Call LanguageOverlay.Remove() on the result to undo your change to this language token.
        /// </summary>
        /// <param name="key">Token the game asks</param>
        /// <param name="value">Value it gives back</param>
        /// <returns>A LanguageOverlay representing your language addition/override; call .Remove() on it to undo the change. May be safely disposed after calling .Remove().</returns>
        public static LanguageOverlay AddOverlay(string? key, string? value) {
        }
        
        /// <summary>
        /// Adds a single temporary language token, and its associated value, to a specific language. Please add multiple instead (dictionary- or file-based signatures) where possible. Overlays added later in time will take precedence. Call LanguageOverlay.Remove() on the result to undo your change to this language token.
        /// </summary>
        /// <param name="key">Token the game asks</param>
        /// <param name="value">Value it gives back</param>
        /// <param name="lang">Language you want to add this to</param>
        /// <returns>A LanguageOverlay representing your language addition/override; call .Remove() on it to undo the change. May be safely disposed after calling .Remove().</returns>
        public static LanguageOverlay AddOverlay(string? key, string? value, string? lang) {
        }
        
        /// <summary>
        /// Add temporary language tokens from a file via path (.language is added automatically). Call LanguageOverlay.Remove() on the result to undo all contained changes. May return null.
        /// </summary>
        /// <param name="path">absolute path to file</param>
        /// <returns>A LanguageOverlay representing your language addition/override; call .Remove() on it to undo the change. Returns null if the target file is missing or cannot be parsed, or if no changes would otherwise be made.</returns>
        public static LanguageOverlay AddOverlayPath(string? path) {
        }

        /// <summary>
        /// Add temporary language tokens from a file via string. Call LanguageOverlay.Remove() on the result to undo all contained changes. May return null.
        /// </summary>
        /// <param name="file">entire file as string</param>
        /// <returns>A LanguageOverlay representing your language addition/override; call .Remove() on it to undo the change. Returns null if no changes would be made.</returns>
        public static LanguageOverlay AddOverlay(string? file) {
        }
        
        /// <summary>
        /// Adds multiple temporary language tokens, and corresponding values, to all languages. Language-specific tokens, as well as overlays added later in time, will take precedence. Call LanguageOverlay.Remove() on the result to remove your changes to these language tokens.
        /// </summary>
        /// <param name="tokenDictionary">dictionaries of key-value (eg ["mytoken"]="mystring")</param>
        /// <returns>A LanguageOverlay representing your language addition/override; call .Remove() on it to undo the change.</returns>
        public static LanguageOverlay AddOverlay(Dictionary<string, string?>? tokenDictionary) {
        }

        /// <summary>
        /// Adds multiple temporary language tokens, and corresponding values, to a specific language. Overlays added later in time will take precedence. Call LanguageOverlay.Remove() on the result to remove your changes to these language tokens.
        /// </summary>
        /// <param name="tokenDictionary">dictionaries of key-value (eg ["mytoken"]="mystring")</param>
        /// <param name="language">Language you want to add this to</param>
        /// <returns>A LanguageOverlay representing your language addition/override; call .Remove() on it to undo the change.</returns>
        public static LanguageOverlay AddOverlay(Dictionary<string, string?>? tokenDictionary, string? language) {
        }
        
        /// <summary>
        /// Adds multiple temporary language tokens, and corresponding values, to mixed languages. Overlays added later in time will take precedence. Call LanguageOverlay.Remove() on the result to remove your changes to these language tokens.
        /// </summary>
        /// <param name="languageDictionary">dictionary of languages containing dictionaries of key-value (eg ["en"]["mytoken"]="mystring")</param>
        /// <returns>A LanguageOverlay representing your language addition/override; call .Remove() on it to undo the change.</returns>
        public static LanguageOverlay AddOverlay(Dictionary<string, Dictionary<string, string?>?>? languageDictionary) {
        }

        /// <summary>
        /// Contains information about a single temporary language token change.
        /// </summary>
        public struct OverlayTokenData {
            /// <summary>The token identifier to add/replace the value of.</summary>
            public string? key;
            /// <summary>The value to set the target token to.</summary>
            public string? value;
            /// <summary>The language which the target token belongs to, if isGeneric = false.</summary>
            public string? lang;
            /// <summary>Whether the target token is generic (applies to all languages which don't contain the token).</summary>
            public bool isGeneric;
        }
    }
}