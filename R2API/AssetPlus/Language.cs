using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using SimpleJSON;
using UnityEngine;

namespace R2API.AssetPlus {
    internal static class TextPlus
    {
        internal static void LanguageAwake()
        {
            var languagePaths = AssetPlus.GetFiles("language");
            foreach (var path in languagePaths)
            {
                Languages.AddPath(path);
            }
            On.RoR2.Language.LoadAllFilesForLanguage += Language_LoadAllFilesForLanguage;
        }

        private static bool Language_LoadAllFilesForLanguage(On.RoR2.Language.orig_LoadAllFilesForLanguage orig, string language)
        {
            var tmp = orig(language);
            ImportCustomLanguageFiles(language);
            return tmp;
        }

        private static void ImportCustomLanguageFiles(string language)
        {
            Dictionary<string, string> dictionary = LoadCustomLanguageDictionary(language);

            foreach (var file in Languages.languages)
            {
                foreach (KeyValuePair<string, string> keyValuePair in LoadCustomTokensFromFile(file, language))
                {
                    AddToken(dictionary, keyValuePair);
                }
            }

            foreach (KeyValuePair<string, string> item in Languages.GenericTokens)
            {
                AddToken(dictionary, item);
            }

            if (Languages.LanguageSpecificTokens.ContainsKey(language))
            {
                foreach (var item in Languages.LanguageSpecificTokens[language])
                {
                    AddToken(dictionary, item);
                }
            }
        }

        private static void AddToken(Dictionary<string, string> dictionary, KeyValuePair<string, string> keyValuePair)
        {
            if (dictionary.ContainsKey(keyValuePair.Key))
            {
                dictionary[keyValuePair.Key] = keyValuePair.Value;
            }
            else
            {
                dictionary.Add(keyValuePair.Key, keyValuePair.Value);
            }
        }

        //based upon RoR2.language.LoadTokensFromFile but with specific language support
        private static IEnumerable<KeyValuePair<string, string>> LoadCustomTokensFromFile(string file, string language)
        {
            try
            {
                JSONNode jsonnode = JSON.Parse(file);
                if (jsonnode != null)
                {
                    JSONNode generic = jsonnode["strings"];
                    if (generic != null)
                    {
                        KeyValuePair<string, string>[] array = new KeyValuePair<string, string>[generic.Count];
                        int num = 0;
                        foreach (string text in generic.Keys)
                        {
                            array[num++] = new KeyValuePair<string, string>(text, generic[text].Value);
                        }
                        return array;
                    }

                    //Specific language support
                    JSONNode specific = jsonnode[language];
                    if (specific != null)
                    {
                        KeyValuePair<string, string>[] array = new KeyValuePair<string, string>[specific.Count];
                        int num = 0;
                        foreach (string text in specific.Keys)
                        {
                            array[num++] = new KeyValuePair<string, string>(text, specific[text].Value);
                        }
                        return array;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogFormat("Parsing error in language file , Error: {0}", new object[]
                {
                        ex
                });
            }
            return Array.Empty<KeyValuePair<string, string>>();
        }

        private static Dictionary<string, string> LoadCustomLanguageDictionary(string language)
        {
            Dictionary<string, Dictionary<string, string>> originalDictionary = loadOriginalDictionary();
            if (!originalDictionary.TryGetValue(language, out Dictionary<string, string> dictionary))
            {
                dictionary = new Dictionary<string, string>();
                originalDictionary[language] = dictionary;
            }
            return dictionary;
        }

        private static Dictionary<string, Dictionary<string, string>> loadOriginalDictionary()
        {
            var dictionary = typeof(RoR2.Language).GetField("languageDictionaries", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            if (dictionary == null)
            {
                return new Dictionary<string, Dictionary<string, string>>();
            }
            else
            {
                return (Dictionary<string, Dictionary<string, string>>)dictionary;
            }
        }
    }

    /// <summary>
    /// class for language files to load
    /// </summary>
    public static class Languages
    {
        /// <summary>
        /// adding an file via path (.language is added automatically )
        /// </summary>
        /// <param name="path">absolute path to file</param>
        public static void AddPath(string path)
        {
            if (File.Exists(path))
            {
                languages.Add(File.ReadAllText(path));
            }

            ReloadLanguage();
        }

        /// <summary>
        /// Adds a single languagetoken and value
        /// </summary>
        /// <param name="key">Token the game asks</param>
        /// <param name="value">Value it gives back</param>
        public static void AddToken(string key, string value)
        {
            AddToken(key, value, true);
        }

        /// <summary>
        /// Adds a single languagetoken and value with optional reload
        /// </summary>
        /// <param name="key">Token the game asks</param>
        /// <param name="value">Value it gives back</param>
        /// <param name="reload">if the dictionary of the game should be reloaded</param>
        public static void AddToken(string key, string value, bool reload = true)
        {

            if (GenericTokens.ContainsKey(key))
            {
                GenericTokens[key] = value;
            }
            else
            {
                GenericTokens.Add(key, value);
            }

            if (reload)
            {
                ReloadLanguage();
            }
        }

        /// <summary>
        /// Adds a single languagetoken and value to a specific language
        /// </summary>
        /// <param name="key">Token the game asks</param>
        /// <param name="value">Value it gives back</param>
        /// <param name="language">Language you want to add this to</param>
        public static void AddToken(string key, string value, string language)
        {
            AddToken(key, value, language,true);
        }

        /// <summary>
        /// Adds a single languagetoken and value to a specific language
        /// </summary>
        /// <param name="key">Token the game asks</param>
        /// <param name="value">Value it gives back</param>
        /// <param name="language">Language you want to add this to</param>
        /// <param name="reload">if the dictionary of the game should be reloaded</param>
        public static void AddToken(string key, string value, string language, bool reload = true)
        {
            if (!LanguageSpecificTokens.ContainsKey(language))
            {
                LanguageSpecificTokens.Add(language, new Dictionary<string, string>());
            }

            if (LanguageSpecificTokens[language].ContainsKey(key))
            {
                GenericTokens[key] = value;
            }
            else
            {
                LanguageSpecificTokens[language].Add(key, value);
            }

            if (reload)
            {
                ReloadLanguage();
            }
        }

        /// <summary>
        /// Adds multiple languagetokens and value to languages
        /// </summary>
        /// <param name="languageDictionary">dictionary of languages containing dictionaries of key-value (eg ["en"]["mytoken"]="mystring")</param>
        public static void AddToken(Dictionary<string, Dictionary<string, string>> languageDictionary) 
        {
            AddToken(languageDictionary,true);
        }

        /// <summary>
        /// Adds multiple languagetokens and value to languages
        /// </summary>
        /// <param name="languageDictionary">dictionary of languages containing dictionaries of key-value (eg ["en"]["mytoken"]="mystring")</param>
        /// <param name="reload">if the dictionary of the game should be reloaded</param>
        public static void AddToken(Dictionary<string, Dictionary<string, string>> languageDictionary, bool reload = true) {

            foreach (var language in languageDictionary.Keys) {
                foreach (var token in languageDictionary[language].Keys) {
                    AddToken(languageDictionary[language][token], token, language,false);
                }
            }
            if (reload)
            {
                ReloadLanguage();
            }
        }

        /// <summary>
        /// Adds multiple languagetokens and value to a specific language
        /// </summary>
        /// <param name="languageDictionary">dictionaries of key-value (eg ["mytoken"]="mystring")</param>
        /// <param name="reload">if the dictionary of the game should be reloaded</param>
        public static void AddToken(Dictionary<string, string> tokenDictionary, string language)
        {
            AddToken(tokenDictionary, language,true);

        }

                /// <summary>
        /// Adds multiple languagetokens and value to a specific language
        /// </summary>
        /// <param name="languageDictionary">dictionaries of key-value (eg ["mytoken"]="mystring")</param>
        /// <param name="reload">if the dictionary of the game should be reloaded</param>
        public static void AddToken(Dictionary<string, string> tokenDictionary, string language, bool reload = true)
        {
            foreach (var token in tokenDictionary.Keys)
            {
                AddToken(token, tokenDictionary[token], language, false);
            }

            if (reload)
            {
                ReloadLanguage();
            }
        }

        /// <summary>
        /// Adds multiple languagetokens and value
        /// </summary>
        /// <param name="languageDictionary">dictionaries of key-value (eg ["mytoken"]="mystring")</param>
        public static void AddToken(Dictionary<string, string> tokenDictionary)
        {
            AddToken(tokenDictionary, true);

        }

        /// <summary>
        /// Adds multiple languagetokens and value
        /// </summary>
        /// <param name="languageDictionary">dictionaries of key-value (eg ["mytoken"]="mystring")</param>
        /// <param name="reload">if the dictionary of the game should be reloaded</param>
        public static void AddToken(Dictionary<string, string> tokenDictionary, bool reload = true) 
        {
            foreach (var token in tokenDictionary.Keys)
            {
                AddToken(token, tokenDictionary[token], false);
            }

            if (reload) {
                ReloadLanguage();
            }
        }

        /// <summary>
        /// Adding an file which is read into an string
        /// </summary>
        /// <param name="file">entire file as string</param>
        public static void Add(string file)
        {
            languages.Add(file);
            ReloadLanguage();
        }

		/// <summary>
        /// Reloads the game language if it is already loaded
        /// </summary>
        public static void ReloadLanguage()
        {
            if (RoR2.Language.currentLanguage != "")
            {
                RoR2.Language.SetCurrentLanguage(RoR2.Language.currentLanguage);
            }
        }

        internal static Dictionary<string, string> GenericTokens = new Dictionary<string, string>();

        internal static Dictionary<string, Dictionary<string, string>> LanguageSpecificTokens = new Dictionary<string, Dictionary<string, string>>();

        internal static List<string> languages = new List<string>();
    }
}
