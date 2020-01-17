using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using Facepunch.Steamworks;
using R2API.Utils;
using RoR2.Networking;
using UnityEngine.Networking;
using Version = System.Version;
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace R2API {
    // ReSharper disable once InconsistentNaming
    /// <summary>
    /// An API for sending and retreiving a list of mods (and config settings) in multiplayer.
    /// </summary>
    [R2APISubmodule]
    public static class ModListAPI {
        //This needs to be active at all times, rather than enabled as a submodule.
        //Central idea is for any client with any mods to send a list of installed mods to server.
        //Server also sends its list to client. That, and the access to those lists is the only functionality of this API.
        //I used Try Catch excessively to hopefully ensure that any errors this causes due to future updates are zero impact.

        /// <summary>
        /// The constant index used by the networkMessages
        /// </summary>
        public const short MessageIndex = 200;
        /// <summary>
        /// The amount of time that the server waits to receive a message before assuming a client is vanilla.
        /// </summary>
        public const float MessageWaitTimeServer = 4f;

        /// <summary>
        /// The amount of time a client waits to receive a message before assuming a server is vanilla.
        /// </summary>
        public const float MessageWaitTimeClient = 3.5f;

        /// <summary>
        /// Is ModListAPI loaded and working?
        /// </summary>
        public static bool Loaded { get; private set; }

        /// <summary>
        /// The ModList from the current server. null if not connected to a server.
        /// </summary>
        public static ModList CurrentServerMods { get; private set; }
        /// <summary>
        /// A dictionary of ModLists for every connected player (and players who have been connected) by NetworkConnection. null if server inactive.
        /// </summary>
        public static Dictionary<NetworkConnection, ModList> ConnectedClientMods { get; private set; }

        /// <summary>
        /// An event invoked whenever a server sends its ModList.
        /// </summary>
        public static event Action<NetworkConnection, ModList> ModListReceivedFromServer;
        /// <summary>
        /// An event invoked whenever a client sends its ModList to server.
        /// </summary>
        public static event Action<NetworkConnection, ModList> ModListReceivedFromClient;

        /// <summary>
        /// Must be called any time config values are changed and if (somehow) mods are installed runtime.
        /// </summary>
        public static void RebuildModList() {
            BuildModList();
            R2API.Logger.LogWarning("Local ModList rebuilt.");
        }

        internal static void Init() {
            try {
                On.RoR2.Networking.NetworkMessageHandlerAttribute.CollectHandlers += ScanForNetworkAttributes;
                GameNetworkManager.onClientConnectGlobal += ClientSideConnect;
                GameNetworkManager.onServerConnectGlobal += ServerSideConnect;
                ConnectedClientMods = new Dictionary<NetworkConnection, ModList>();
#if DEBUG
                ModListReceivedFromServer += (conn, list) => R2API.Logger.LogInfo($"From Server: {list.TextRepresentation()}");
                ModListReceivedFromClient += (conn, list) => R2API.Logger.LogInfo($"From Client: {list.TextRepresentation()}");
#endif
                Loaded = true;
            }
            catch (Exception e) {
                Fail(e, "Init");
            }

        }

        private static void Fail(Exception e, string location) {
            R2API.Logger.LogWarning("ModListAPI did not load properly and will not function.");
            R2API.Logger.LogWarning("Location: " + location);
            R2API.Logger.LogError(e);
            Loaded = false;
        }

        #region Setup Network Attributes
        private static void ScanForNetworkAttributes(On.RoR2.Networking.NetworkMessageHandlerAttribute.orig_CollectHandlers orig) {
            orig();
            try {
                const BindingFlags allFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic;

                var clientListInfo = typeof(NetworkMessageHandlerAttribute).GetField("clientMessageHandlers", allFlags);
                var clientList = (List<NetworkMessageHandlerAttribute>)clientListInfo?.GetValue(typeof(NetworkMessageHandlerAttribute));

                var serverListInfo = typeof(NetworkMessageHandlerAttribute).GetField("serverMessageHandlers", allFlags);
                var serverList = (List<NetworkMessageHandlerAttribute>)serverListInfo?.GetValue(typeof(NetworkMessageHandlerAttribute));

                FieldInfo netAttribHandler = typeof(NetworkMessageHandlerAttribute).GetField("messageHandler", allFlags);

                foreach (MemberInfo m in typeof(ModListAPI).GetMembers(allFlags)) {
                    var attrib = m.GetCustomAttribute<NetworkMessageHandlerAttribute>();
                    if (attrib == null)
                        continue;

                    var del = (NetworkMessageDelegate)Delegate.CreateDelegate(typeof(NetworkMessageDelegate), (MethodInfo)m);
                    netAttribHandler?.SetValue(attrib, del);

                    if (attrib.client)
                        clientList?.Add(attrib);
                    if (attrib.server)
                        serverList?.Add(attrib);
                }
            }
            catch (Exception e) {
                Fail(e, "ScanForNetworkAttributes");
            }
        }
        #endregion

        #region Networking
        private static ModList _localModList;
        private static ModList _tempServerModList;

        private static void ServerSideConnect(NetworkConnection connection) {
            try {
                var msg = new ModListMessage(_localModList, CSteamID.nil, true);
                connection.SendByChannel(MessageIndex, msg, QosChannelIndex.defaultReliable.intVal);
                MessageWaitServerAsync(connection, MessageWaitTimeServer);
            }
            catch (Exception e) {
                Fail(e, "ServerSideConnect");
            }
        }
        private static void ClientSideConnect(NetworkConnection connection) {
            try {
                var msg = new ModListMessage(_localModList, new CSteamID(Client.Instance.SteamId));
                connection.SendByChannel(MessageIndex, msg, QosChannelIndex.defaultReliable.intVal);
                MessageWaitClientAsync(connection, MessageWaitTimeClient);
            }
            catch (Exception e){
                Fail(e, "ClientSideConnect");
            }
        }

        private static async void MessageWaitServerAsync(NetworkConnection conn, float duration) {
            await Task.Delay(TimeSpan.FromSeconds(duration));

            if (!ConnectedClientMods.ContainsKey(conn))
                ConnectedClientMods.Add(conn, ModList.Vanilla);

            ModListReceivedFromClient?.Invoke(conn, ConnectedClientMods[conn]);
        }

        private static async void MessageWaitClientAsync(NetworkConnection conn, float duration) {
            await Task.Delay(TimeSpan.FromSeconds(duration));

            ModList list = _tempServerModList ?? ModList.Vanilla;
            CurrentServerMods = list;
            ModListReceivedFromServer?.Invoke(conn, list);
            _tempServerModList = null;
        }

        [NetworkMessageHandler(client = true, server = true, msgType = MessageIndex)]
        private static void HandleModListMessage(NetworkMessage netMsg) {
            var modListMessage = netMsg.ReadMessage<ModListMessage>();

            if (modListMessage == null) {
                R2API.Logger.LogError("Invalid message sent to message index: " + MessageIndex);
                return;
            }

            if (modListMessage.FromServer)
                _tempServerModList = modListMessage.Mods;
            else {
                if (!modListMessage.SteamId.isValid)
                    R2API.Logger.LogWarning("Server received message with invalid SteamID");
                ConnectedClientMods.Add(netMsg.conn, modListMessage.Mods);
            }
        }

        private class ModListMessage : MessageBase {
            internal ModList Mods { get; private set; }
            internal CSteamID SteamId { get; private set; }
            internal bool FromServer { get; private set; }

            internal ModListMessage(ModList mods, CSteamID steamId, bool fromServer = false) {
                Mods = mods;
                SteamId = steamId;
                FromServer = fromServer;
            }

            internal ModListMessage(ModList mods, ulong steamId, bool fromServer = false) {
                Mods = mods;
                SteamId = new CSteamID(steamId);
                FromServer = fromServer;
            }

            /// <summary>
            /// Do not use, must be public for Unity to access.
            /// </summary>
            public ModListMessage() { }

            /// <summary>
            /// Do not use, must be public for Unity to access.
            /// </summary>
            /// <param name="writer"></param>
            public override void Serialize(NetworkWriter writer) {
                Mods.Write(writer);
                writer.Write(SteamId.value);
                writer.Write(FromServer);
            }

            /// <summary>
            /// Do not use, must be public for Unity to access.
            /// </summary>
            /// <param name="reader"></param>
            public override void Deserialize(NetworkReader reader) {
                Mods = ModList.Read(reader);
                SteamId = new CSteamID(reader.ReadUInt64());
                FromServer = reader.ReadBoolean();
            }
        }
        #endregion

        #region Setting up the modlist classes
        /// <summary>
        /// A list of mods.
        /// </summary>
        public class ModList {
            private readonly List<ModInfo> _modInfos;

            /// <summary>
            /// A readonly collection of ModInfos.
            /// </summary>
            public ReadOnlyCollection<ModInfo> Mods => _modInfos.AsReadOnly();

            /// <summary>
            /// Is this modlist empty?
            /// </summary>
            public bool IsVanilla => _modInfos == null || _modInfos.Count == 0;

            private static ModList _intVanilla;

            /// <summary>
            /// An empty modlist
            /// </summary>
            public static ModList Vanilla => _intVanilla ?? (_intVanilla = new ModList());

            /// <summary>
            /// Get a string of all the mods contained in the list.
            /// </summary>
            /// <returns>the string with each mod on its own line</returns>
            public string TextRepresentation() {
                var builder = new StringBuilder("Mod List:", 200);
                foreach (ModInfo mod in _modInfos) {
                    builder.Append("\n");
                    builder.Append(mod.Guid);
                    builder.Append(" v.");
                    builder.Append(mod.Version);
                }
                return builder.ToString();
            }

            internal void Write(NetworkWriter writer) {
                writer.Write(_modInfos.Count);
                foreach (ModInfo mod in _modInfos) {
                    mod.Write(writer);
                }
            }

            internal static ModList Read(NetworkReader reader) {
                var count = reader.ReadInt32();
                var mods = new List<ModInfo>(count);

                for (var i = 0; i < count; i++)
                    mods.Add(ModInfo.Read(reader));

                return new ModList(mods);
            }

            internal ModList(List<ModInfo> mods) {
                _modInfos = mods;
            }

            private ModList() {
                _modInfos = new List<ModInfo>();
            }
        }

        /// <summary>
        /// An entry in a list of mods, represents a single mod.
        /// </summary>
        public class ModInfo {
            private readonly ConfigData _configData;

            /// <summary>
            /// The (hopefully unique) GUID of the mod as a string.
            /// </summary>
            public string Guid { get; }

            /// <summary>
            /// The version of the Mod
            /// </summary>
            public Version Version { get; }

            /// <summary>
            /// Does this ModInfo have a config?
            /// </summary>
            public bool HasConfig => _configData != null;
            
            internal ModInfo(PluginInfo plugin) {
                Guid = plugin.Metadata.GUID;
                Version = plugin.Metadata.Version;

                _configData = plugin.Instance.Config != null ? new ConfigData(plugin.Instance.Config) : null;
            }

            private ModInfo(string guid, Version version, ConfigData data) {
                Guid = guid;
                Version = version;
                _configData = data;
            }

            /// <summary>
            /// Check if two mods are the same
            /// </summary>
            /// <param name="mod">The other mod</param>
            /// <returns>true if they are the same</returns>
            public bool MatchMod(ModInfo mod) {
                return Guid == mod.Guid;
            }

            /// <summary>
            /// Checks if two mods are the same, and are the same version
            /// </summary>
            /// <param name="mod">The other mod</param>
            /// <returns>true if both mod and version are the same</returns>
            public bool MatchVersion(ModInfo mod) {
                return MatchMod(mod) && Version == mod.Version;
            }

            /// <summary>
            /// Checks if two mods are the same, are the same version, and have the same config settings
            /// </summary>
            /// <param name="mod">The other mod</param>
            /// <returns>true if mod, version, and config are the same</returns>
            public bool MatchConfig(ModInfo mod) {
                return MatchVersion(mod) && _configData.Matches(mod._configData);
            }

            /// <summary>
            /// Gets a value in the config for the mod from the ConfigEntry used to bind it.
            /// </summary>
            /// <typeparam name="T">The Type of the value to get</typeparam>
            /// <param name="configEntry">The ConfigEntry that was used to bind it.</param>
            /// <returns>The value from the config</returns>
            public T GetConfigValue<T>(ConfigEntry<T> configEntry) {
                return !HasConfig ? default : _configData.LookupValue(configEntry);
            }

            internal void Write(NetworkWriter writer) {
                writer.Write(Guid);
                writer.Write(Version.ToString());
                writer.Write(HasConfig);
                _configData.Write(writer);
            }
            internal static ModInfo Read(NetworkReader reader) {
                var guid = reader.ReadString();
                var version = new Version(reader.ReadString());
                ConfigData configData = null;

                if (reader.ReadBoolean())
                    configData = ConfigData.Read(reader);

                return new ModInfo(guid, version, configData);
            }
        }

        #region Config file serialization
        internal class ConfigData {
            private const string Divider = ":-:";

            private Dictionary<string, ConfigOption> _data = new Dictionary<string, ConfigOption>();

            private ConfigData() { }

            internal ConfigData(ConfigFile config) {
                foreach (ConfigDefinition def in config.Keys)
                    _data[def.Section + Divider + def.Key] = new ConfigOption(config[def]);
            }

            internal void Write(NetworkWriter writer) {
                writer.Write(_data.Count);

                foreach (KeyValuePair<string, ConfigOption> dataEntry in _data) {
                    writer.Write(dataEntry.Key);
                    dataEntry.Value.Write(writer);
                }
            }

            internal static ConfigData Read(NetworkReader reader) {
                int count = reader.ReadInt32();
                var data = new Dictionary<string, ConfigOption>(count);

                for (int i = 0; i < count; i++) {
                    var keyString = reader.ReadString();
                    data[keyString] = ConfigOption.Read(reader);
                }

                return new ConfigData {
                    _data = data
                };
            }

            internal bool Matches(ConfigData configData) {
                foreach (KeyValuePair<string,ConfigOption> entry in _data)
                    if (!configData._data.ContainsKey(entry.Key) || !configData._data[entry.Key].Matches(_data[entry.Key]))
                        return false;

                return true;
            }

            internal T LookupValue<T>(ConfigEntry<T> entry) {
                var key = entry.Definition.Section + Divider + entry.Definition.Key;
                if (_data.ContainsKey(key)) {
                    var option = _data[key];

                    if (option.Type == typeof(T)) {
                        return (T)option.Value;
                    }

                    R2API.Logger.LogError("Type mismatch for key: " + entry.Definition.Key + " in config.");
                    return default;
                }

                R2API.Logger.LogError("The key: " + entry.Definition.Key + " does not exist in that config. Could be a different version of the mod.");
                return default;
            }
        }

        private class ConfigOption {
            private static Dictionary<Type, string> _typeIndexMap;
            private static Dictionary<string, Type> _indexTypeMap;

            internal readonly Type Type;
            internal readonly object Value;

            internal ConfigOption(ConfigEntryBase entry) {
                Type = entry.SettingType;
                Value = entry.BoxedValue;
            }

            private ConfigOption(Type type, object value) {
                Type = type;
                Value = value;
            }

            internal void Write(NetworkWriter writer) {
                writer.Write(GetTypeIndex(Type));
                writer.Write(TomlTypeConverter.GetConverter(Type).ConvertToString(Value, Type));
            }

            internal static ConfigOption Read(NetworkReader reader) {
                var typeString = reader.ReadString();
                var objString = reader.ReadString();
                var type = GetIndexType(typeString);

                if (type == null)
                    return new ConfigOption(typeof(int), -999);
                var obj = TomlTypeConverter.GetConverter(type).ConvertToObject(objString, type);

                return new ConfigOption(type, obj);
            }

            internal bool Matches(ConfigOption option) {
                if (Type != option.Type)
                    return false;

                string val1;
                string val2;

                try {
                    val1 = TomlTypeConverter.GetConverter(Type).ConvertToString(Value, Type);
                    val2 = TomlTypeConverter.GetConverter(option.Type).ConvertToString(option.Value, option.Type);
                }
                catch {
                    return false;
                }

                return val1 == val2;
            }

            private static string GetTypeIndex(Type type) {
                if (_typeIndexMap == null || _indexTypeMap == null) BuildTypeIndexMap();

                if (_typeIndexMap != null && _typeIndexMap.ContainsKey(type)) {
                    return _typeIndexMap[type];
                }

                R2API.Logger.LogError("Invalid type found in Bepinex Config.");
                return "";
            }

            private static Type GetIndexType(string index) {
                if (_typeIndexMap == null || _indexTypeMap == null)
                    BuildTypeIndexMap();

                return _indexTypeMap != null && _indexTypeMap.ContainsKey(index) ? _indexTypeMap[index] : null;
            }

            private static void BuildTypeIndexMap() {
                _typeIndexMap = new Dictionary<Type, string>();
                _indexTypeMap = new Dictionary<string, Type>();

                foreach (var t in TomlTypeConverter.GetSupportedTypes()) {
                    _typeIndexMap[t] = t.AssemblyQualifiedName;
                    if (t.AssemblyQualifiedName != null)
                        _indexTypeMap[t.AssemblyQualifiedName] = t;
                }
            }
        }
        #endregion
        #endregion
        
        internal static void BuildModList() {
            try {
                var mods = new List<ModInfo>();
                foreach (KeyValuePair<string, PluginInfo> kv in Chainloader.PluginInfos) {
                    mods.Add(new ModInfo(kv.Value));
                }

                _localModList = new ModList(mods);
            }
            catch (Exception e) {
                Fail(e, "BuildModList");
            }
        }
    }
}
