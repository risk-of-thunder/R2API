using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using SimpleJSON;
using UnityEngine;

namespace R2API.AssetPlus {
    /// <summary>
    /// class for language files to load
    /// </summary>
    [Obsolete("Moved to R2API/LanguageAPI")]
    public static class Languages
    {
        /// <summary>
        /// adding an file via path (.language is added automatically )
        /// </summary>
        /// <param name="path">absolute path to file</param>
        [Obsolete("Moved to R2API/LanguageAPI")]
        public static void AddPath(string path)
        {
            LanguageAPI.AddPath(path);
        }

        /// <summary>
        /// Adds a single languagetoken and value
        /// </summary>
        /// <param name="key">Token the game asks</param>
        /// <param name="value">Value it gives back</param>
        [Obsolete("Moved to R2API/LanguageAPI")]
        public static void AddToken(string key, string value)
        {
            LanguageAPI.Add(key, value);
        }

        /// <summary>
        /// Adds a single languagetoken and value with optional reload
        /// </summary>
        /// <param name="key">Token the game asks</param>
        /// <param name="value">Value it gives back</param>
        /// <param name="reload">if the dictionary of the game should be reloaded</param>
        [Obsolete("Moved to R2API/LanguageAPI")]
        public static void AddToken(string key, string value, bool reload = true)
        {
            LanguageAPI.Add(key, value);
        }

        /// <summary>
        /// Adds a single languagetoken and value to a specific language
        /// </summary>
        /// <param name="key">Token the game asks</param>
        /// <param name="value">Value it gives back</param>
        /// <param name="language">Language you want to add this to</param>
        [Obsolete("Moved to R2API/LanguageAPI")]
        public static void AddToken(string key, string value, string language)
        {
            LanguageAPI.Add(key, value, language);
        }

        /// <summary>
        /// Adds a single languagetoken and value to a specific language
        /// </summary>
        /// <param name="key">Token the game asks</param>
        /// <param name="value">Value it gives back</param>
        /// <param name="language">Language you want to add this to</param>
        /// <param name="reload">if the dictionary of the game should be reloaded</param>
        [Obsolete("Moved to R2API/LanguageAPI")]
        public static void AddToken(string key, string value, string language, bool reload = true)
        {
            LanguageAPI.Add(key, value, language);
        }

        /// <summary>
        /// Adds multiple languagetokens and value to languages
        /// </summary>
        /// <param name="languageDictionary">dictionary of languages containing dictionaries of key-value (eg ["en"]["mytoken"]="mystring")</param>
        [Obsolete("Moved to R2API/LanguageAPI")]
        public static void AddToken(Dictionary<string, Dictionary<string, string>> languageDictionary) 
        {
            LanguageAPI.Add(languageDictionary);
        }

        /// <summary>
        /// Adds multiple languagetokens and value to languages
        /// </summary>
        /// <param name="languageDictionary">dictionary of languages containing dictionaries of key-value (eg ["en"]["mytoken"]="mystring")</param>
        /// <param name="reload">if the dictionary of the game should be reloaded</param>
        [Obsolete("Moved to R2API/LanguageAPI")]
        public static void AddToken(Dictionary<string, Dictionary<string, string>> languageDictionary, bool reload = true) {

            LanguageAPI.Add(languageDictionary);
        }

        /// <summary>
        /// Adds multiple languagetokens and value to a specific language
        /// </summary>
        [Obsolete("Moved to R2API/LanguageAPI")]
        public static void AddToken(Dictionary<string, string> tokenDictionary, string language)
        {
            LanguageAPI.Add(tokenDictionary, language);
        }

        /// <summary>
        /// Adds multiple languagetokens and value to a specific language
        /// </summary>
        [Obsolete("Moved to R2API/LanguageAPI")]
        public static void AddToken(Dictionary<string, string> tokenDictionary, string language, bool reload = true)
        {
            LanguageAPI.Add(tokenDictionary, language);
        }

        /// <summary>
        /// Adds multiple languagetokens and value
        /// </summary>
        [Obsolete("Moved to R2API/LanguageAPI")]
        public static void AddToken(Dictionary<string, string> tokenDictionary)
        {
            LanguageAPI.Add(tokenDictionary);
        }

        /// <summary>
        /// Adds multiple languagetokens and value
        /// </summary>
        [Obsolete("Moved to R2API/LanguageAPI")]
        public static void AddToken(Dictionary<string, string> tokenDictionary, bool reload = true) 
        {
            LanguageAPI.Add(tokenDictionary);
        }

        /// <summary>
        /// Adding an file which is read into an string
        /// </summary>
        /// <param name="file">entire file as string</param>
        [Obsolete("Moved to R2API/LanguageAPI")]
        public static void Add(string file)
        {
            LanguageAPI.Add(file);
        }

        /// <summary>
        /// Reloads the game language if it is already loaded
        /// </summary>
        [Obsolete("not needed anymore")]
        public static void ReloadLanguage()
        {
            return;
        }
    }
}
