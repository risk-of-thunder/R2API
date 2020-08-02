using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using R2API.Utils;
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

            On.RoR2.Language.GetString_string_string += Language_GetString_string_string;
        }

        private static string Language_GetString_string_string(On.RoR2.Language.orig_GetString_string_string orig, string token, string language) {
            if (LanguageSpecificTokens.ContainsKey(language)) {
                if (LanguageSpecificTokens[language].ContainsKey(token)) {
                    return LanguageSpecificTokens[language][token];
                }
            }

            if (GenericTokens.ContainsKey(token)) {
                return GenericTokens[token];
            }

            return orig(token, language);
        }

        //based upon RoR2.language.LoadTokensFromFile but with specific language support
        private static void LoadCustomTokensFromFile(string file) {
            try {
                JSONNode jsonnode = JSON.Parse(file);
                if (jsonnode == null) {
                    return;
                }

                var languageTokens = jsonnode.Keys;
                foreach (var language in languageTokens) {
                    JSONNode generic = jsonnode[language];
                    if (generic == null) {
                        return;
                    }
                    foreach (string text in generic.Keys) {
                        Add(text, generic[text].Value);
                    }
                }
            }
            catch (Exception ex) {
                Debug.LogFormat("Parsing error in language file , Error: {0}", new object[]
                {
                        ex
                });
            }
        }

        /// <summary>
        /// Adds a single languagetoken and value
        /// </summary>
        /// <param name="key">Token the game asks</param>
        /// <param name="value">Value it gives back</param>
        public static void Add(string key, string value) {
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
        public static void Add(string key, string value, string language) {
            if (!LanguageSpecificTokens.ContainsKey(language)) {
                LanguageSpecificTokens.Add(language, new Dictionary<string, string>());
            }

            if (LanguageSpecificTokens[language].ContainsKey(key)) {
                GenericTokens[key] = value;
            }
            else {
                LanguageSpecificTokens[language].Add(key, value);
            }
        }

        /// <summary>
        /// adding an file via path (.language is added automatically)
        /// </summary>
        /// <param name="path">absolute path to file</param>
        public static void AddPath(string path) {
            if (File.Exists(path)) {
                Add(File.ReadAllText(path));
            }
        }

        /// <summary>
        /// Adding an file which is read into an string
        /// </summary>
        /// <param name="file">entire file as string</param>
        public static void Add(string file) {
            LoadCustomTokensFromFile(file);
        }

        /// <summary>
        /// Adds multiple languagetokens and value
        /// </summary>
        /// <param name="tokenDictionary">dictionaries of key-value (eg ["mytoken"]="mystring")</param>
        public static void Add(Dictionary<string, string> tokenDictionary) {
            Add(tokenDictionary);
            foreach (var token in tokenDictionary.Keys) {
                Add(token, tokenDictionary[token]);
            }
        }

        /// <summary>
        /// Adds multiple languagetokens and value to a specific language
        /// </summary>
        /// <param name="tokenDictionary">dictionaries of key-value (eg ["mytoken"]="mystring")</param>
        /// <param name="language">Language you want to add this to</param>
        public static void Add(Dictionary<string, string> tokenDictionary, string language) {
            foreach (var token in tokenDictionary.Keys) {
                Add(token, tokenDictionary[token], language);
            }
        }

        /// <summary>
        /// Adds multiple languagetokens and value to languages
        /// </summary>
        /// <param name="languageDictionary">dictionary of languages containing dictionaries of key-value (eg ["en"]["mytoken"]="mystring")</param>
        public static void Add(Dictionary<string, Dictionary<string, string>> languageDictionary) {
            foreach (var language in languageDictionary.Keys) {
                foreach (var token in languageDictionary[language].Keys) {
                    Add(languageDictionary[language][token], token, language);
                }
            }
        }

        internal static Dictionary<string, string> GenericTokens = new Dictionary<string, string>();

        internal static Dictionary<string, Dictionary<string, string>> LanguageSpecificTokens = new Dictionary<string, Dictionary<string, string>>();
    }
}
