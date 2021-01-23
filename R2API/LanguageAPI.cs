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

        private static void throwIfNotLoaded() {
            if (!Loaded) {
                throw new InvalidOperationException($"{nameof(LanguageAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(LanguageAPI)})]");
            }
        }

        private static Dictionary<string, Dictionary<string, string>> customLanguage = new Dictionary<string, Dictionary<string, string>>();
        private static Dictionary<string, Dictionary<string, string>> overlayLanuage = new Dictionary<string, Dictionary<string, string>>();
        private static List<LanguageOverlay> temporaryOverlays = new List<LanguageOverlay>();
        private const string genericLanguage = "generic";

        [R2APISubmoduleInit(Stage = InitStage.SetHooks)]
#pragma warning disable IDE0051 // Remove unused private members
        internal static void LanguageAwake() {
#pragma warning restore IDE0051 // Remove unused private members
            if (Loaded) {
                return;
            }

            Loaded = true;

            var languagePaths = Directory.GetFiles(Paths.PluginPath, "*.language", SearchOption.AllDirectories);
            foreach (var path in languagePaths) {
                AddPath(path);
            }

            On.RoR2.Language.GetLocalizedStringByToken += Language_GetLocalizedStringByToken;
        }

        private static string Language_GetLocalizedStringByToken(On.RoR2.Language.orig_GetLocalizedStringByToken orig, Language self, string token) {
            var languagename = self.name;
            if (overlayLanuage.ContainsKey(languagename)) {
                if (overlayLanuage[languagename].ContainsKey(token)) {
                    return overlayLanuage[languagename][token];
                }
            }
            if (overlayLanuage.ContainsKey(genericLanguage)) {
                if (overlayLanuage[genericLanguage].ContainsKey(token)) {
                    return overlayLanuage[genericLanguage][token];
                }
            }
            if (customLanguage.ContainsKey(languagename)) {
                if (customLanguage[languagename].ContainsKey(token)) {
                    return customLanguage[languagename][token];
                }
            }
            if (customLanguage.ContainsKey(genericLanguage)) {
                if (customLanguage[genericLanguage].ContainsKey(token)) {
                    return customLanguage[genericLanguage][token];
                }
            }
            return orig(self, token);
        }

        [R2APISubmoduleInit(Stage = InitStage.LoadCheck)]
#pragma warning disable IDE0051 // Remove unused private members
        private static void ShouldLoad(out bool shouldload) {
#pragma warning restore IDE0051 // Remove unused private members
            shouldload = Directory.GetFiles(Paths.PluginPath, "*.language", SearchOption.AllDirectories).Length > 0;
        }

        private static Dictionary<string, Dictionary<string, string>>? LoadFile(string fileContent) {
            Dictionary<string, Dictionary<string, string>> dict = new Dictionary<string, Dictionary<string, string>>();
            try {
                JSONNode jsonNode = JSON.Parse(fileContent);
                if (jsonNode == null) {
                    return null;
                }

                var languages = jsonNode.Keys;
                foreach (var language in languages) {
                    JSONNode languageTokens = jsonNode[language];
                    if (languageTokens == null) {
                        continue;
                    }

                    var languagename = language;
                    if (languagename == "strings") {
                        languagename = genericLanguage;
                    }

                    if (!dict.ContainsKey(languagename)) {
                        dict.Add(languagename, new Dictionary<string, string>());
                    }
                    var languagedict = dict[languagename];

                    foreach (var key in languageTokens.Keys) {
                        languagedict.Add(key, languageTokens[key].Value);
                    }
                }
            }
            catch (Exception ex) {
                Debug.LogFormat("Parsing error in language file , Error: {0}", ex);
                return null;
            }
            if (dict.Count == 0) {
                return null;
            }
            return dict;
        }

        /// <summary>
        /// Adds a single languagetoken and its associated value to all languages
        /// </summary>
        /// <param name="key">Token the game asks</param>
        /// <param name="value">Value it gives back</param>
        public static void Add(string? key, string? value) {
            throwIfNotLoaded();
            if (key == null) {
                throw new NullReferenceException($"param {nameof(key)} is null");
            }
            if (value == null) {
                throw new NullReferenceException($"param {nameof(value)} is null");
            }

            Add(key, value, genericLanguage);
        }

        /// <summary>
        /// Adds a single languagetoken and value to a specific language
        /// </summary>
        /// <param name="key">Token the game asks</param>
        /// <param name="value">Value it gives back</param>
        /// <param name="language">Language you want to add this to</param>
        public static void Add(string? key, string? value, string? language) {
            throwIfNotLoaded();
            if (key == null) {
                throw new NullReferenceException($"param {nameof(key)} is null");
            }
            if (value == null) {
                throw new NullReferenceException($"param {nameof(value)} is null");
            }
            if (language == null) {
                throw new NullReferenceException($"param {nameof(language)} is null");
            }

            if (!customLanguage.ContainsKey(language)) {
                customLanguage.Add(language, new Dictionary<string, string>());
            }
            var languagedict = customLanguage[language];
            if (!languagedict.ContainsKey(key)) {
                languagedict.Add(key, value);
            }
        }

        /// <summary>
        /// adding an file via path (.language is added automatically)
        /// </summary>
        /// <param name="path">absolute path to file</param>
        public static void AddPath(string? path) {
            throwIfNotLoaded();
            if (path == null) {
                throw new NullReferenceException($"param {nameof(path)} is null");
            }

            var fileText = File.ReadAllText(path);
            Add(fileText);
        }

        /// <summary>
        /// Adding an file which is read into an string
        /// </summary>
        /// <param name="file">entire file as string</param>
        public static void Add(string? file) {
            throwIfNotLoaded();
            if (file == null) {
                throw new NullReferenceException($"param {nameof(file)} is null");
            }

            var dict = LoadFile(file);
            if (dict == null) {
                return;
            }

            Add(dict!);
        }

        /// <summary>
        /// Adds multiple languagetokens and value
        /// </summary>
        /// <param name="tokenDictionary">dictionaries of key-value (eg ["mytoken"]="mystring")</param>
        public static void Add(Dictionary<string, string?>? tokenDictionary) {
            throwIfNotLoaded();
            Add(tokenDictionary, genericLanguage);
        }

        /// <summary>
        /// Adds multiple languagetokens and value to a specific language
        /// </summary>
        /// <param name="tokenDictionary">dictionaries of key-value (eg ["mytoken"]="mystring")</param>
        /// <param name="language">Language you want to add this to</param>
        public static void Add(Dictionary<string, string?>? tokenDictionary, string? language) {
            throwIfNotLoaded();
            if (tokenDictionary == null) {
                throw new NullReferenceException($"param {nameof(tokenDictionary)} is null");
            }

            foreach (var item in tokenDictionary) {
                if (item.Value == null) {
                    continue;
                }
                Add(item.Key, item.Value, language);
            }
        }

        /// <summary>
        /// Adds multiple languagetokens and value to languages
        /// </summary>
        /// <param name="languageDictionary">dictionary of languages containing dictionaries of key-value (eg ["en"]["mytoken"]="mystring")</param>
        public static void Add(Dictionary<string, Dictionary<string, string?>?>? languageDictionary) {
            throwIfNotLoaded();
            if (languageDictionary == null) {
                throw new NullReferenceException($"param {nameof(languageDictionary)} is null");
            }

            foreach (var language in languageDictionary) {
                Add(language.Value, language.Key);
            }
        }

        /// <summary>
        /// Manages temporary language token changes.
        /// </summary>
        public class LanguageOverlay {
            internal LanguageOverlay(List<OverlayTokenData> data) {
                overlayTokenDatas = data;
                readOnlyOverlays = overlayTokenDatas.AsReadOnly();
                temporaryOverlays.Add(this);
                this.Add();
            }
            /// <summary>Contains information about the language token changes this LanguageOverlay makes.</summary>
            public readonly ReadOnlyCollection<OverlayTokenData> readOnlyOverlays;
            private List<OverlayTokenData> overlayTokenDatas;

            private void Add() {
                foreach (var item in readOnlyOverlays) {
                    if (!overlayLanuage.ContainsKey(item.lang)) {
                        overlayLanuage.Add(item.lang, new Dictionary<string, string>());
                    }
                    var langdict = overlayLanuage[item.lang];
                    langdict[item.key] = item.value;
                }
            }

            /// <summary>Undoes this LanguageOverlay's language token changes; you may safely dispose it afterwards. Requires a language reload to take effect.</summary>
            public void Remove() {
                temporaryOverlays.Remove(this);
                overlayLanuage.Clear();
                foreach (var item in temporaryOverlays) {
                    item.Add();
                }
            }
        }

        /// <summary>
        /// Adds a single temporary language token, and its associated value, to all languages. Please add multiple instead (dictionary- or file-based signatures) where possible. Language-specific tokens, as well as overlays added later in time, will take precedence. Call LanguageOverlay.Remove() on the result to undo your change to this language token.
        /// </summary>
        /// <param name="key">Token the game asks</param>
        /// <param name="value">Value it gives back</param>
        /// <returns>A LanguageOverlay representing your language addition/override; call .Remove() on it to undo the change. May be safely disposed after calling .Remove().</returns>
        public static LanguageOverlay AddOverlay(string? key, string? value) {
            throwIfNotLoaded();
            if (key == null) {
                throw new NullReferenceException($"param {nameof(key)} is null");
            }
            if (value == null) {
                throw new NullReferenceException($"param {nameof(value)} is null");
            }

            return AddOverlay(key, value, genericLanguage);
        }
        
        /// <summary>
        /// Adds a single temporary language token, and its associated value, to a specific language. Please add multiple instead (dictionary- or file-based signatures) where possible. Overlays added later in time will take precedence. Call LanguageOverlay.Remove() on the result to undo your change to this language token.
        /// </summary>
        /// <param name="key">Token the game asks</param>
        /// <param name="value">Value it gives back</param>
        /// <param name="lang">Language you want to add this to</param>
        /// <returns>A LanguageOverlay representing your language addition/override; call .Remove() on it to undo the change. May be safely disposed after calling .Remove().</returns>
        public static LanguageOverlay AddOverlay(string? key, string? value, string? lang) {
            throwIfNotLoaded();
            if (key == null) {
                throw new NullReferenceException($"param {nameof(key)} is null");
            }
            if (value == null) {
                throw new NullReferenceException($"param {nameof(value)} is null");
            }
            if (lang == null) {
                throw new NullReferenceException($"param {nameof(lang)} is null");
            }

            var list = new List<OverlayTokenData>(1);
            list.Add(new OverlayTokenData(key, value, lang));

            return new LanguageOverlay(list);
        }
        
        /// <summary>
        /// Add temporary language tokens from a file via path (.language is added automatically). Call LanguageOverlay.Remove() on the result to undo all contained changes. May return null.
        /// </summary>
        /// <param name="path">absolute path to file</param>
        /// <returns>A LanguageOverlay representing your language addition/override; call .Remove() on it to undo the change. Returns null if the target file is missing or cannot be parsed, or if no changes would otherwise be made.</returns>
        public static LanguageOverlay? AddOverlayPath(string? path) {
            throwIfNotLoaded();
            if (path == null) {
                throw new NullReferenceException($"param {nameof(path)} is null");
            }

            var text = File.ReadAllText(path);
            if (text == null) {
                return null;
            }
            return AddOverlay(text);
        }

        /// <summary>
        /// Add temporary language tokens from a file via string. Call LanguageOverlay.Remove() on the result to undo all contained changes. May return null.
        /// </summary>
        /// <param name="file">entire file as string</param>
        /// <returns>A LanguageOverlay representing your language addition/override; call .Remove() on it to undo the change. Returns null if no changes would be made.</returns>
        public static LanguageOverlay? AddOverlay(string? file) {
            throwIfNotLoaded();
            if (file == null) {
                throw new NullReferenceException($"param {nameof(file)} is null");
            }

            var dict = LoadFile(file);
            if (dict == null) {
                return null;
            }
            return AddOverlay(dict!);
        }
        
        /// <summary>
        /// Adds multiple temporary language tokens, and corresponding values, to all languages. Language-specific tokens, as well as overlays added later in time, will take precedence. Call LanguageOverlay.Remove() on the result to remove your changes to these language tokens.
        /// </summary>
        /// <param name="tokenDictionary">dictionaries of key-value (eg ["mytoken"]="mystring")</param>
        /// <returns>A LanguageOverlay representing your language addition/override; call .Remove() on it to undo the change.</returns>
        public static LanguageOverlay AddOverlay(Dictionary<string, string?>? tokenDictionary) {
            throwIfNotLoaded();
            if (tokenDictionary == null) {
                throw new NullReferenceException($"param {nameof(tokenDictionary)} is null");
            }

            return AddOverlay(tokenDictionary, genericLanguage);
        }

        /// <summary>
        /// Adds multiple temporary language tokens, and corresponding values, to a specific language. Overlays added later in time will take precedence. Call LanguageOverlay.Remove() on the result to remove your changes to these language tokens.
        /// </summary>
        /// <param name="tokenDictionary">dictionaries of key-value (eg ["mytoken"]="mystring")</param>
        /// <param name="language">Language you want to add this to</param>
        /// <returns>A LanguageOverlay representing your language addition/override; call .Remove() on it to undo the change.</returns>
        public static LanguageOverlay AddOverlay(Dictionary<string, string?>? tokenDictionary, string? language) {
            throwIfNotLoaded();
            if (tokenDictionary == null) {
                throw new NullReferenceException($"param {nameof(tokenDictionary)} is null");
            }

            if (language == null) {
                throw new NullReferenceException($"param {nameof(language)} is null");
            }

            var list = new List<OverlayTokenData>(tokenDictionary.Count);
            foreach (var item in tokenDictionary) {
                if (item.Value == null) {
                    continue;
                }
                list.Add(new OverlayTokenData(item.Key, item.Value, language));
            }
            return new LanguageOverlay(list);
        }
        
        /// <summary>
        /// Adds multiple temporary language tokens, and corresponding values, to mixed languages. Overlays added later in time will take precedence. Call LanguageOverlay.Remove() on the result to remove your changes to these language tokens.
        /// </summary>
        /// <param name="languageDictionary">dictionary of languages containing dictionaries of key-value (eg ["en"]["mytoken"]="mystring")</param>
        /// <returns>A LanguageOverlay representing your language addition/override; call .Remove() on it to undo the change.</returns>
        public static LanguageOverlay AddOverlay(Dictionary<string, Dictionary<string, string?>?>? languageDictionary) {
            throwIfNotLoaded();
            if (languageDictionary == null) {
                throw new NullReferenceException($"param {nameof(languageDictionary)} is null");
            }

            var list = new List<OverlayTokenData>();
            foreach (var language in languageDictionary) {
                if (language.Value == null) {
                    continue;
                }

                foreach (var tokenvalue in language.Value) {
                    if (tokenvalue.Value == null) {
                        continue;
                    }

                    list.Add(new OverlayTokenData(language.Key, tokenvalue.Key, tokenvalue.Value));
                }
            }

            return new LanguageOverlay(list);
        }

        /// <summary>
        /// Contains information about a single temporary language token change.
        /// </summary>
        public struct OverlayTokenData {
            /// <summary>The token identifier to add/replace the value of.</summary>
            public string key;
            /// <summary>The value to set the target token to.</summary>
            public string value;
            /// <summary>The language which the target token belongs to, if isGeneric = false.</summary>
            public string lang;
            /// <summary>Whether the target token is generic (applies to all languages which don't contain the token).</summary>
            public bool isGeneric;

            internal OverlayTokenData(string _key, string _value, string _lang) {
                key = _key;
                value = _value;
                if (_lang == genericLanguage) {
                    isGeneric = true;
                }
                else {
                    isGeneric = false;
                }
                lang = _lang;
            }
            internal OverlayTokenData(string _key, string _value) {
                key = _key;
                value = _value;
                lang = genericLanguage;
                isGeneric = true;
            }
        }
    }
}
