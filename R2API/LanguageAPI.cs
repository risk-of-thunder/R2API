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
        internal static void LanguageAwake() {
            if (Loaded) {
                return;
            }

            Loaded = true;

            var languagePaths = Directory.GetFiles(Paths.PluginPath, "*.language", SearchOption.AllDirectories);
            foreach (var path in languagePaths) {
                AddPath(path);
            }

            Language.onCurrentLanguageChanged += OnCurrentLanguageChanged;
        }

        private static void OnCurrentLanguageChanged() {
            var currentLanguage = Language.currentLanguage;
            if (currentLanguage is null)
                return;

            _originalTokens.Clear();
            _originalTokens.AddRange(currentLanguage.stringsByToken);

            currentLanguage.stringsByToken = currentLanguage.stringsByToken.ReplaceAndAddRange(GenericTokens);
                
            if (LanguageSpecificTokens.TryGetValue(currentLanguage.name, out var languageSpecificDic)) {
                currentLanguage.stringsByToken = currentLanguage.stringsByToken.ReplaceAndAddRange(languageSpecificDic);
            }

            GenericOverlays.Clear();
            LanguageSpecificOverlays.Clear();
            onSetupLanguageOverlays?.Invoke();

            currentLanguage.stringsByToken = currentLanguage.stringsByToken.ReplaceAndAddRange(GenericOverlays);
                
            if (LanguageSpecificOverlays.TryGetValue(currentLanguage.name, out var languageSpecificOverlayDic)) {
                currentLanguage.stringsByToken = currentLanguage.stringsByToken.ReplaceAndAddRange(languageSpecificOverlayDic);
            }
        }

        private static Dictionary<string, string> ReplaceAndAddRange(this Dictionary<string, string> dict, Dictionary<string, string> other) {
            dict = dict.Where(kvp => !other.ContainsKey(kvp.Key))
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            dict.AddRange(other);

            return dict;
        }

        //based upon RoR2.language.LoadTokensFromFile but with specific language support
        private static void LoadCustomTokensFromFile(string file) {
            try {
                JSONNode jsonNode = JSON.Parse(file);
                if (jsonNode == null) {
                    return;
                }

                var genericsAdded = false;
                var languages = jsonNode.Keys;
                foreach (var language in languages) {
                    JSONNode languageTokens = jsonNode[language];
                    if (languageTokens == null) {
                        return;
                    }

                    if (!genericsAdded) {
                        foreach (string text in languageTokens.Keys) {
                            Add(text, languageTokens[text].Value);
                        }
                        genericsAdded = true;
                    }

                    foreach (string text in languageTokens.Keys) {
                        Add(text, languageTokens[text].Value, language);
                    }
                }
            }
            catch (Exception ex) {
                Debug.LogFormat("Parsing error in language file , Error: {0}", ex);
            }
        }

        /// <summary>
        /// Adds a single languagetoken and its associated value to all languages
        /// </summary>
        /// <param name="key">Token the game asks</param>
        /// <param name="value">Value it gives back</param>
        public static void Add(string? key, string? value) {
            if(!Loaded) {
                throw new InvalidOperationException($"{nameof(LanguageAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(LanguageAPI)})]");
            }
            if (GenericTokens.ContainsKey(key)) {
                GenericTokens[key] = value;
            }
            else {
                GenericTokens.Add(key, value);
            }
        }

        /// <summary>
        /// Adds a single languagetoken and value to a specific language
        /// </summary>
        /// <param name="key">Token the game asks</param>
        /// <param name="value">Value it gives back</param>
        /// <param name="language">Language you want to add this to</param>
        public static void Add(string? key, string? value, string? language) {
            if(!Loaded) {
                throw new InvalidOperationException($"{nameof(LanguageAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(LanguageAPI)})]");
            }
            if (!LanguageSpecificTokens.ContainsKey(language)) {
                LanguageSpecificTokens.Add(language, new Dictionary<string, string>());
            }

            if (LanguageSpecificTokens[language].ContainsKey(key)) {
                R2API.Logger.LogDebug($"Overriding token {key} in {language} dictionary");
                LanguageSpecificTokens[language][key] = value;
            }
            else {
                LanguageSpecificTokens[language].Add(key, value);
            }
        }

        /// <summary>
        /// adding an file via path (.language is added automatically)
        /// </summary>
        /// <param name="path">absolute path to file</param>
        public static void AddPath(string? path) {
            if(!Loaded) {
                throw new InvalidOperationException($"{nameof(LanguageAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(LanguageAPI)})]");
            }
            if (File.Exists(path)) {
                Add(File.ReadAllText(path));
            } else R2API.Logger.LogError($"LanguageAPI.AddPath: Couldn't find language file at path \"{path}\"");
        }

        /// <summary>
        /// Adding an file which is read into an string
        /// </summary>
        /// <param name="file">entire file as string</param>
        public static void Add(string? file) {
            if(!Loaded) {
                throw new InvalidOperationException($"{nameof(LanguageAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(LanguageAPI)})]");
            }
            LoadCustomTokensFromFile(file);
        }

        /// <summary>
        /// Adds multiple languagetokens and value
        /// </summary>
        /// <param name="tokenDictionary">dictionaries of key-value (eg ["mytoken"]="mystring")</param>
        public static void Add(Dictionary<string?, string?>? tokenDictionary) {
            if(!Loaded) {
                throw new InvalidOperationException($"{nameof(LanguageAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(LanguageAPI)})]");
            }
            foreach (var token in tokenDictionary.Keys) {
                Add(token, tokenDictionary[token]);
            }
        }

        /// <summary>
        /// Adds multiple languagetokens and value to a specific language
        /// </summary>
        /// <param name="tokenDictionary">dictionaries of key-value (eg ["mytoken"]="mystring")</param>
        /// <param name="language">Language you want to add this to</param>
        public static void Add(Dictionary<string?, string?>? tokenDictionary, string? language) {
            if(!Loaded) {
                throw new InvalidOperationException($"{nameof(LanguageAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(LanguageAPI)})]");
            }
            foreach (var token in tokenDictionary.Keys) {
                Add(token, tokenDictionary[token], language);
            }
        }

        /// <summary>
        /// Adds multiple languagetokens and value to languages
        /// </summary>
        /// <param name="languageDictionary">dictionary of languages containing dictionaries of key-value (eg ["en"]["mytoken"]="mystring")</param>
        public static void Add(Dictionary<string?, Dictionary<string?, string?>?>? languageDictionary) {
            if(!Loaded) {
                throw new InvalidOperationException($"{nameof(LanguageAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(LanguageAPI)})]");
            }
            foreach (var language in languageDictionary.Keys) {
                foreach (var token in languageDictionary[language].Keys) {
                    Add(languageDictionary[language][token], token, language);
                }
            }
        }

        internal static Dictionary<string, string> GenericTokens = new Dictionary<string, string>();

        internal static Dictionary<string, Dictionary<string, string>> LanguageSpecificTokens = new Dictionary<string, Dictionary<string, string>>();
        
        internal static Dictionary<string, string> _originalTokens = new Dictionary<string, string>();
        public static ReadOnlyDictionary<string, string> OriginalTokens = new ReadOnlyDictionary<string, string>(_originalTokens);

        internal static Dictionary<string, string> GenericOverlays = new Dictionary<string, string>();

        internal static Dictionary<string, Dictionary<string, string>> LanguageSpecificOverlays = new Dictionary<string, Dictionary<string, string>>();

        internal delegate void SetupLanguageOverlays();
        internal static event SetupLanguageOverlays onSetupLanguageOverlays;

        /// <summary>
        /// Manages temporary language token changes.
        /// </summary>
        public class LanguageOverlay {
            private readonly OverlayTokenData[]? overlays;
            /// <summary>Contains information about the language token changes this LanguageOverlay makes.</summary>
            public readonly ReadOnlyCollection<OverlayTokenData> readOnlyOverlays;

            internal LanguageOverlay(OverlayTokenData[]? _overlays) {
                overlays = _overlays;
                readOnlyOverlays = new ReadOnlyCollection<OverlayTokenData>(overlays);
            }

            internal LanguageOverlay(OverlayTokenData _singleOverlay) {
                overlays = new OverlayTokenData[]{_singleOverlay};
                readOnlyOverlays = new ReadOnlyCollection<OverlayTokenData>(overlays);
            }

            internal void Add() {
                onSetupLanguageOverlays += LanguageOverlay_onSetupLanguageOverlays;
            }

            /// <summary>Undoes this LanguageOverlay's language token changes; you may safely dispose it afterwards. Requires a language reload to take effect.</summary>
            public void Remove() {
                onSetupLanguageOverlays -= LanguageOverlay_onSetupLanguageOverlays;
            }

            private void LanguageOverlay_onSetupLanguageOverlays() {
                foreach(var overlay in overlays) {
                    Dictionary<string, string> targetDict;
                    if(overlay.isGeneric) {
                        targetDict = GenericOverlays;
                    } else {
                        if(!LanguageSpecificOverlays.ContainsKey(overlay.lang))
                            LanguageSpecificOverlays.Add(overlay.lang, new Dictionary<string, string>());
                        targetDict = LanguageSpecificOverlays[overlay.lang];
                    }
                    targetDict[overlay.key] = overlay.value;
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
            var overlay = new LanguageOverlay(new OverlayTokenData(key, value));
            overlay.Add();
            return overlay;
        }
        
        /// <summary>
        /// Adds a single temporary language token, and its associated value, to a specific language. Please add multiple instead (dictionary- or file-based signatures) where possible. Overlays added later in time will take precedence. Call LanguageOverlay.Remove() on the result to undo your change to this language token.
        /// </summary>
        /// <param name="key">Token the game asks</param>
        /// <param name="value">Value it gives back</param>
        /// <param name="lang">Language you want to add this to</param>
        /// <returns>A LanguageOverlay representing your language addition/override; call .Remove() on it to undo the change. May be safely disposed after calling .Remove().</returns>
        public static LanguageOverlay AddOverlay(string? key, string? value, string? lang) {
            var overlay = new LanguageOverlay(new OverlayTokenData(key, value, lang));
            overlay.Add();
            return overlay;
        }
        
        /// <summary>
        /// Add temporary language tokens from a file via path (.language is added automatically). Call LanguageOverlay.Remove() on the result to undo all contained changes. May return null.
        /// </summary>
        /// <param name="path">absolute path to file</param>
        /// <returns>A LanguageOverlay representing your language addition/override; call .Remove() on it to undo the change. Returns null if the target file is missing or cannot be parsed, or if no changes would otherwise be made.</returns>
        public static LanguageOverlay AddOverlayPath(string? path) {
            if(!Loaded) {
                throw new InvalidOperationException($"{nameof(LanguageAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(LanguageAPI)})]");
            }
            if (File.Exists(path)) {
                return AddOverlay(File.ReadAllText(path));
            } else {
                R2API.Logger.LogError($"LanguageAPI.AddOverlayPath: Couldn't find language file at path \"{path}\"");
                return null;
            }
        }

        /// <summary>
        /// Add temporary language tokens from a file via string. Call LanguageOverlay.Remove() on the result to undo all contained changes. May return null.
        /// </summary>
        /// <param name="file">entire file as string</param>
        /// <returns>A LanguageOverlay representing your language addition/override; call .Remove() on it to undo the change. Returns null if no changes would be made.</returns>
        public static LanguageOverlay AddOverlay(string? file) {
            if(!Loaded) {
                throw new InvalidOperationException($"{nameof(LanguageAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(LanguageAPI)})]");
            }
            var data = LoadOverlayTokensFromFile(file);
            if(data.Count == 0) {
                R2API.Logger.LogError("LanguageAPI.AddOverlay(string file): Result contains zero tokens");
                return null;
            }
            return new LanguageOverlay(data.ToArray());
        }
        
        /// <summary>
        /// Adds multiple temporary language tokens, and corresponding values, to all languages. Language-specific tokens, as well as overlays added later in time, will take precedence. Call LanguageOverlay.Remove() on the result to remove your changes to these language tokens.
        /// </summary>
        /// <param name="tokenDictionary">dictionaries of key-value (eg ["mytoken"]="mystring")</param>
        /// <returns>A LanguageOverlay representing your language addition/override; call .Remove() on it to undo the change.</returns>
        public static LanguageOverlay AddOverlay(Dictionary<string?, string?>? tokenDictionary) {
            var overlay = new LanguageOverlay(tokenDictionary.Select(kvp => {return new OverlayTokenData(kvp.Key, kvp.Value);}).ToArray());
            overlay.Add();
            return overlay;
        }

        /// <summary>
        /// Adds multiple temporary language tokens, and corresponding values, to a specific language. Overlays added later in time will take precedence. Call LanguageOverlay.Remove() on the result to remove your changes to these language tokens.
        /// </summary>
        /// <param name="tokenDictionary">dictionaries of key-value (eg ["mytoken"]="mystring")</param>
        /// <param name="language">Language you want to add this to</param>
        /// <returns>A LanguageOverlay representing your language addition/override; call .Remove() on it to undo the change.</returns>
        public static LanguageOverlay AddOverlay(Dictionary<string?, string?>? tokenDictionary, string? language) {
            var overlay = new LanguageOverlay(tokenDictionary.Select(kvp => {return new OverlayTokenData(kvp.Key, kvp.Value, language);}).ToArray());
            overlay.Add();
            return overlay;
        }
        
        /// <summary>
        /// Adds multiple temporary language tokens, and corresponding values, to mixed languages. Overlays added later in time will take precedence. Call LanguageOverlay.Remove() on the result to remove your changes to these language tokens.
        /// </summary>
        /// <param name="languageDictionary">dictionary of languages containing dictionaries of key-value (eg ["en"]["mytoken"]="mystring")</param>
        /// <returns>A LanguageOverlay representing your language addition/override; call .Remove() on it to undo the change.</returns>
        public static LanguageOverlay AddOverlay(Dictionary<string?, Dictionary<string?, string?>?>? languageDictionary) {
            var overlay = new LanguageOverlay(
                languageDictionary.SelectMany(subdict => {
                    return subdict.Value.Select(kvp => {
                        return new OverlayTokenData(kvp.Key, kvp.Value, subdict.Key);
                    });
                }).ToArray());

            overlay.Add();
            return overlay;
        }
        
        private static List<OverlayTokenData> LoadOverlayTokensFromFile(string? file) {
            var data = new List<OverlayTokenData>();
            try {
                JSONNode jsonNode = JSON.Parse(file);
                if (jsonNode == null) {
                    return data;
                }

                var genericsAdded = false;
                var languages = jsonNode.Keys;
                foreach (var language in languages) {
                    JSONNode languageTokens = jsonNode[language];
                    if (languageTokens == null) {
                        return data;
                    }

                    if (!genericsAdded) {
                        foreach (string text in languageTokens.Keys) {
                            data.Add(new OverlayTokenData(text, languageTokens[text].Value));
                        }
                        genericsAdded = true;
                    }

                    foreach (string text in languageTokens.Keys) {
                        data.Add(new OverlayTokenData(text, languageTokens[text].Value, language));
                    }
                }
                return data;
            }
            catch (Exception ex) {
                Debug.LogFormat("Parsing error in language file , Error: {0}", ex);
                return data;
            }
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

            internal OverlayTokenData(string? _key, string? _value, string? _lang) {
                key = _key;
                value = _value;
                lang = _lang;
                isGeneric = false;
            }
            internal OverlayTokenData(string? _key, string? _value) {
                key = _key;
                value = _value;
                lang = "";
                isGeneric = true;
            }
        }
    }
}
