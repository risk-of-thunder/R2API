using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Configuration;
using Facepunch.Steamworks;
using R2API.Utils;
using RoR2.Networking;
using UnityEngine;
using UnityEngine.Networking;

namespace R2API {
    // ReSharper disable once InconsistentNaming
    /// <summary>
    /// An API for sending and retreiving a list of mods (and config settings) for connected players and the server in multiplayer.
    /// </summary>
    [R2APISubmodule]
    public static class ModListAPI {
        //This needs to be active at all times, rather than enabled as a submodule.
        //Central idea is for any client with any mods to send a list of installed mods to server.
        //Server also sends its list to client.
        //In addition to a list of mods, all config settings for mods are also sent as part of the list.
        //Those config settings are accessible 
        //I used Try Catch excessively to hopefully ensure that any errors this causes due to future updates are zero impact.

        /// <summary>
        /// The constant index used by the networkMessages
        /// </summary>
        public const short messageIndex = 200;
        /// <summary>
        /// The amount of time that the server waits to recieve a message before assuming a client is vanilla.
        /// </summary>
        public const float messageWaitTimeServer = 4f;

        /// <summary>
        /// The amount of time a client waits to recieve a message before assuming a server is vanilla.
        /// </summary>
        public const float messageWaitTimeClient = 3.5f;

        /// <summary>
        /// Is ModListAPI loaded and working?
        /// </summary>
        public static bool Loaded { get; private set; }

        /// <summary>
        /// The ModList from the current server. null if not connected to a server.
        /// </summary>
        public static ModList currentServerMods { get; private set; }
        /// <summary>
        /// A dictionary of ModLists for every connected player (and players who have been connected) by steamID. null if server inactive.
        /// </summary>
        public static Dictionary<CSteamID, ModList> connectedClientMods { get; private set; }

        /// <summary>
        /// An event invoked whenever a server sends its ModList.
        /// </summary>
        public static event Action<NetworkConnection, ModList> modlistRecievedFromServer;
        /// <summary>
        /// An event invoked whenever a client sends its ModList to server.
        /// </summary>
        public static event Action<NetworkConnection, ModList, CSteamID> modlistRecievedFromClient;

        /// <summary>
        /// Must be called any time config values are changed and if (somehow) mods are installed runtime.
        /// </summary>
        public static void RebuildModList() {
            BuildModList();
            R2API.Logger.LogWarning( "Local ModList rebuilt." );
        }

        internal static void Init() {
            try {
                Loaded = true;
                On.RoR2.Networking.NetworkMessageHandlerAttribute.CollectHandlers += ScanForNetworkAttributes;
                GameNetworkManager.onClientConnectGlobal += ClientsideConnect;
                GameNetworkManager.onServerConnectGlobal += ServersideConnect;
                connectedClientMods = new Dictionary<CSteamID, ModList>();
#if DEBUG
                modlistRecievedFromServer += ( conn, list ) => R2API.Logger.LogInfo( list.TextRepresentation() );
                modlistRecievedFromClient += ( conn, list, steamID ) => R2API.Logger.LogInfo( list.TextRepresentation() );
#endif

            } catch ( Exception e ) {
                Fail( e, "Init" );
            }

        }

        private static void Fail( Exception e, string location ) {
            R2API.Logger.LogWarning( "ModlistAPI did not load properly and will not function." );
            R2API.Logger.LogWarning( "Location: " + location );
            R2API.Logger.LogError( e );
            Loaded = false;
        }

        #region Setup Network Attributes
        private static void ScanForNetworkAttributes( On.RoR2.Networking.NetworkMessageHandlerAttribute.orig_CollectHandlers orig ) {
            orig();
            try {
                var allFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic;

                var clientListInfo = typeof(NetworkMessageHandlerAttribute).GetField("clientMessageHandlers", allFlags);
                var clientList = (List<NetworkMessageHandlerAttribute>)clientListInfo.GetValue(typeof(NetworkMessageHandlerAttribute));

                var serverListInfo = typeof(NetworkMessageHandlerAttribute).GetField("serverMessageHandlers", allFlags );
                var serverList = (List<NetworkMessageHandlerAttribute>)serverListInfo.GetValue(typeof(NetworkMessageHandlerAttribute));

                FieldInfo netAtribHandler = typeof(NetworkMessageHandlerAttribute).GetField( "messageHandler", allFlags );

                foreach( MemberInfo m in typeof( ModListAPI ).GetMembers( allFlags ) ) {
                    var attrib = m.GetCustomAttribute<NetworkMessageHandlerAttribute>();
                    if( attrib == null ) continue;

                    var del = (NetworkMessageDelegate)Delegate.CreateDelegate(typeof(NetworkMessageDelegate), (MethodInfo)m);
                    netAtribHandler.SetValue( attrib, del );


                    if( attrib.client ) clientList.Add( attrib );
                    if( attrib.server ) serverList.Add( attrib );
                }
            } catch ( Exception e ) {
                Fail( e, "ScanForNetworkAttributes" );
            }
        }
        #endregion

        #region Actually do networking stuff
        private static Dictionary<NetworkConnection, ModList> tempConnectionInfo = new Dictionary<NetworkConnection, ModList>();
        private static ModList localModList;
        private static ModList tempServerModList;

        private static void ServersideConnect( UnityEngine.Networking.NetworkConnection connection ) {
            try {
                ModListMessage msg = new ModListMessage( localModList, true, new CSteamID() );
                connection.SendByChannel( messageIndex, msg, QosChannelIndex.defaultReliable.intVal );
                MessageWaitServerAsync( connection, messageWaitTimeServer, msg.steamID );
            } catch (Exception e ) {
                Fail( e, "ServersideConnect" );
            }
        }
        private static void ClientsideConnect( UnityEngine.Networking.NetworkConnection connection ) {
            try {
                ModListMessage msg = new ModListMessage( localModList, false, new CSteamID(Client.Instance.SteamId ) );
                connection.SendByChannel( messageIndex, msg, QosChannelIndex.defaultReliable.intVal );
                MessageWaitClientAsync( connection, messageWaitTimeClient );
            } catch (Exception e ){
                Fail( e, "ClientsideConnect" );
            }
        }

        private static async void MessageWaitServerAsync( NetworkConnection conn, float duration, CSteamID from ) {
            await Task.Delay( TimeSpan.FromSeconds( duration ) );

            try {
                ModList list = null;
                if( tempConnectionInfo.ContainsKey( conn ) ) {
                    list = tempConnectionInfo[conn];
                } else {
                    list = ModList.vanilla;
                }
                if( from == null || from == new CSteamID() ) {
                    R2API.Logger.LogWarning( "Server recieved message with invalid SteamID" );
                    from = new CSteamID();
                }
                //CSteamID steamID = ServerAuthManager.FindAuthData( conn ).steamId;
                connectedClientMods[from] = list;
                modlistRecievedFromClient?.Invoke( conn, list, from );
                if( tempConnectionInfo.ContainsKey( conn ) ) {
                    tempConnectionInfo.Remove( conn );
                }
            } catch (Exception e) {
                Fail( e, "MessageWaitServerAsync" );
            }

        }

        private static async void MessageWaitClientAsync( NetworkConnection conn, float duration ) {
            await Task.Delay( TimeSpan.FromSeconds( duration ) );

            try {
                ModList list = null;
                if( tempServerModList != null ) {
                    list = tempServerModList;
                } else {
                    list = ModList.vanilla;
                }
                currentServerMods = list;
                modlistRecievedFromServer?.Invoke( conn, list );
                tempServerModList = null;
            } catch( Exception e ) {
                Fail( e, "MessageWaitClientAsync" );
            }
        }

        [NetworkMessageHandler( client = true, server = true, msgType = messageIndex )]
        private static void HandleModListMessage( NetworkMessage netMsg ) {
            ModListMessage modInfo = netMsg.ReadMessage<ModListMessage>();
            if( modInfo == null ) {
                R2API.Logger.LogError( "Invalid message sent to message index: " + messageIndex );
                return;
            }
            if( modInfo.fromServer ) {
                tempServerModList = modInfo.mods;
            } else {
                tempConnectionInfo[netMsg.conn] = modInfo.mods;
            }
        }

        private class ModListMessage : MessageBase {
            internal ModList mods { get; private set; }
            internal bool fromServer { get; private set; }
            internal CSteamID steamID;


            internal ModListMessage( ModList mods, bool fromServer, CSteamID steamID ) {
                this.mods = mods;
                this.fromServer = fromServer;
                this.steamID = steamID;
            }

            internal ModListMessage( ModList mods, bool fromServer, ulong steamID ) {
                this.mods = mods;
                this.fromServer = fromServer;
                this.steamID = new CSteamID(steamID);
            }

            /// <summary>
            /// Do not use, must be public for Unity to access.
            /// </summary>
            public ModListMessage() { }

            /// <summary>
            /// Do not use, must be public for Unity to access.
            /// </summary>
            /// <param name="writer"></param>
            public override void Serialize( NetworkWriter writer ) {
                writer.Write( this.fromServer);
                writer.Write( this.steamID.value );
                this.mods.Write( writer );
            }

            /// <summary>
            /// Do not use, must be public for Unity to access.
            /// </summary>
            /// <param name="reader"></param>
            public override void Deserialize( NetworkReader reader ) {
                this.fromServer = reader.ReadBoolean();
                this.steamID = new CSteamID( reader.ReadUInt64() );
                this.mods = ModList.Read( reader );
            }
        }
        #endregion

        #region Setting up the modlist classes
        /// <summary>
        /// A list of mods.
        /// </summary>
        public class ModList {
            private List<ModInfo> modInfos = new List<ModInfo>();

            /// <summary>
            /// A readonly collection of ModInfos.
            /// </summary>
            public ReadOnlyCollection<ModInfo> mods {
                get {
                    return this.modInfos.AsReadOnly();
                }
            }

            /// <summary>
            /// Get a string of all the mods contained in the list.
            /// </summary>
            /// <returns>the string with each mod on its own line</returns>
            public string TextRepresentation() {
                var builder = new StringBuilder( "Mod List:", 200 );
                foreach( ModInfo mod in this.modInfos ) {
                    builder.Append( "\n" );
                    builder.Append( mod.guid );
                    builder.Append( " v." );
                    builder.Append( mod.version );
                }
                return builder.ToString();
            }

            /// <summary>
            /// Is this modlist empty?
            /// </summary>
            public bool isVanilla {
                get {
                    return this.modInfos == null || this.modInfos.Count == 0;
                }
            }

            internal void Write( NetworkWriter writer ) {
                writer.Write( this.modInfos.Count );
                foreach( ModInfo mod in this.modInfos ) {
                    mod.Write( writer );
                }
            }

            private static ModList intVanilla;

            /// <summary>
            /// An empty modlist
            /// </summary>
            public static ModList vanilla {
                get {
                    if( intVanilla == null ) intVanilla = new ModList();
                    return intVanilla;
                }
            }

            internal static ModList Read( NetworkReader reader ) {
                var count = reader.ReadInt32();
                var mods = new List<ModInfo>(count);
                for( Int32 i = 0; i < count; i++ ) {
                    mods.Add( ModInfo.Read( reader ) );
                }
                return new ModList( mods );
            }

            internal ModList( List<ModInfo> mods ) {
                this.modInfos = mods;
            }

            internal ModList() {
                this.modInfos = new List<ModInfo>();
            }
        }

        /// <summary>
        /// An entry in a list of mods, represents a single mod.
        /// </summary>
        public class ModInfo {
            internal ModInfo( PluginInfo plugin ) {
                this.guid = plugin.Metadata.GUID;
                this.version = plugin.Metadata.Version;
                if( plugin.Instance.Config != null ) {
                    this.configData = new ConfigData( plugin.Instance.Config );
                } else {
                    this.configData = null;
                }
            }
            private ModInfo( String guid, System.Version version, ConfigData configData ) {
                this.guid = guid;
                this.version = version;
                this.configData = configData;
            }

            /// <summary>
            /// The (hopefully unique) GUID of the mod as a string.
            /// </summary>
            public String guid { get; private set; }

            /// <summary>
            /// The version of the Mod
            /// </summary>
            public System.Version version { get; private set; }

            /// <summary>
            /// Does this ModInfo have a config?
            /// </summary>
            public bool hasConfig {
                get {
                    return this.configData != null;
                }
            }

            /// <summary>
            /// Check if two mods are the same
            /// </summary>
            /// <param name="mod">The other mod</param>
            /// <returns>true if they are the same</returns>
            public bool MatchMod( ModInfo mod ) {
                return this.guid == mod.guid;
            }

            /// <summary>
            /// Checks if two mods are the same, and are the same version
            /// </summary>
            /// <param name="mod">The other mod</param>
            /// <returns>true if both mod and version are the same</returns>
            public bool MatchVersion( ModInfo mod ) {
                return this.MatchMod( mod ) && this.version == mod.version;
            }

            /// <summary>
            /// Checks if two mods are the same, are the same version, and have the same config settings
            /// </summary>
            /// <param name="mod">The other mod</param>
            /// <returns>true if mod, version, and config are the same</returns>
            public bool MatchConfig( ModInfo mod ) {
                return this.MatchVersion( mod ) && this.configData.Matches( mod.configData );
            }

            /// <summary>
            /// Gets a value in the mod's config from the ConfigEntry used to bind it.
            /// </summary>
            /// <typeparam name="T">The Type of the value to get</typeparam>
            /// <param name="configEntry">The ConfigEntry that was used to bind it.</param>
            /// <returns>The value from the config</returns>
            public T GetConfigValue<T>( ConfigEntry<T> configEntry ) {
                if( !this.hasConfig ) return default;
                return this.configData.LookupValue<T>( configEntry );
            }

            /// <summary>
            /// Gets a value in the mod's config from the key and section of the setting.
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="key">The key of the setting</param>
            /// <param name="section">The section the setting is in</param>
            /// <returns>The value from the config</returns>
            public T GetConfigValue<T>( string key, string section ) {
                if( !this.hasConfig ) return default;
                return this.configData.LookupValue<T>( key, section );
            }

            /// <summary>
            /// Gets a value in the mod's config from the key and section of the setting as System.Object.
            /// </summary>
            /// <param name="key">The key of the setting</param>
            /// <param name="section">The section the setting is in</param>
            /// <returns>The value from the config as System.Object</returns>
            public object GetConfigValue( string key, string section ) {
                if( !this.hasConfig ) return default;
                return this.configData.LookupValue( key, section );
            }


            private ConfigData configData;
            internal void Write( NetworkWriter writer ) {
                writer.Write( this.guid );
                writer.Write( this.version.ToString() );
                writer.Write( this.hasConfig );
                this.configData.Write( writer );
            }
            internal static ModInfo Read( NetworkReader reader ) {
                var guid = reader.ReadString();
                var version = new System.Version(reader.ReadString());
                ConfigData configData = null;
                if( reader.ReadBoolean() ) {
                    configData = ConfigData.Read( reader );
                }
                return new ModInfo( guid, version, configData );
            }
        }

        #region Config file serialization
        internal class ConfigData {
            private static string divider = ":-:";
            internal ConfigData( ConfigFile config ) {
                foreach( ConfigDefinition def in config.Keys ) {
                    this.data[def.Section + divider + def.Key] = new ConfigOption( config[def] );
                }
            }
            private ConfigData() { }

            private Dictionary< string, ConfigOption > data = new Dictionary<string, ConfigOption>();

            internal void Write( NetworkWriter writer ) {
                writer.Write( this.data.Count );
                foreach( KeyValuePair<string, ConfigOption> dataEntry in this.data ) {
                    writer.Write( dataEntry.Key );
                    dataEntry.Value.Write( writer );
                }
            }

            internal bool Matches( ConfigData data ) {
                foreach( KeyValuePair<string,ConfigOption> entry in this.data ) {
                    if( !data.data.ContainsKey( entry.Key ) || !data.data[entry.Key].Matches( this.data[entry.Key] ) ) return false;
                }
                return true;
            }

            internal static ConfigData Read( NetworkReader reader ) {
                int count = reader.ReadInt32();
                Dictionary<string, ConfigOption> data = new Dictionary<string, ConfigOption>(count);
                for( int i = 0; i < count; i++ ) {
                    var keyString = reader.ReadString();
                    data[keyString] = ConfigOption.Read( reader );
                }

                return new ConfigData {
                    data = data
                };
            }

            internal T LookupValue<T>( string key, string section ) {
                var intKey = section + divider + key;
                if( this.data.ContainsKey( intKey ) ) {
                    var option = this.data[intKey];

                    if( option.type == typeof( T ) ) {
                        return (T)option.value;
                    } else {
                        R2API.Logger.LogError( "Type mismatch for key: " + key + " in config. The correct type is: " + option.type );
                        return default;
                    }
                } else {
                    R2API.Logger.LogError( "The key: " + key + " does not exist in that config. Could be a different version of the mod." );
                    return default;
                }
            }

            internal T LookupValue<T>( ConfigEntry<T> entry ) {
                return LookupValue<T>( entry.Definition.Key, entry.Definition.Section );
            }

            internal object LookupValue( string key, string section ) {
                var intKey = section + divider + key;
                if( this.data.ContainsKey( intKey ) ) {
                    return this.data[intKey].value;
                } else {
                    R2API.Logger.LogError( "The key: " + key + " does not exist in that config. Could be a different version of the mod." );
                    return default;
                }
            }
        }

        private class ConfigOption {
            internal ConfigOption( ConfigEntryBase entry ) {
                this.type = entry.SettingType;
                this.value = entry.BoxedValue;
            }

            private ConfigOption( Type type, object value ) {
                this.type = type;
                this.value = value;
            }

            internal Type type;
            internal object value;

            internal void Write( NetworkWriter writer ) {
                writer.Write( GetTypeIndex(this.type) );
                writer.Write( TomlTypeConverter.GetConverter( this.type ).ConvertToString( this.value, this.type ) );
            }

            internal bool Matches( ConfigOption option ) {
                if( this.type != option.type ) return false;
                string val1 = null;
                string val2 = null;
                try {
                    val1 = TomlTypeConverter.GetConverter( this.type ).ConvertToString( this.value, this.type );
                    val2 = TomlTypeConverter.GetConverter( option.type ).ConvertToString( option.value, option.type );
                } catch {
                    return false;
                }
                if( val1 != val2 ) return false;
                return true;
            }


            internal static ConfigOption Read( NetworkReader reader ) {
                var typeString = reader.ReadString();
                var objString = reader.ReadString();
                var type = GetIndexType( typeString );
                if( type == null ) return new ConfigOption( typeof( int ), -999 );
                var obj = TomlTypeConverter.GetConverter( type ).ConvertToObject( objString, type);
                return new ConfigOption( type, obj );
            }

            private static Dictionary<Type,string> typeIndexMap = null;
            private static Dictionary<string,Type> indexTypeMap = null;
            private static string GetTypeIndex( Type type ) {
                if( typeIndexMap == null || indexTypeMap == null ) BuildTypeIndexMap();

                if( typeIndexMap.ContainsKey( type ) ) return typeIndexMap[type];

                R2API.Logger.LogError( "Invalid type found in Bepinex Config." );
                return "";
            }

            private static Type GetIndexType( string index ) {
                if( typeIndexMap == null || indexTypeMap == null ) BuildTypeIndexMap();
                if( indexTypeMap.ContainsKey( index ) ) return indexTypeMap[index];

                return null;
            }

            private static void BuildTypeIndexMap() {
                typeIndexMap = new Dictionary<Type, string>();
                indexTypeMap = new Dictionary<string, Type>(); 
                foreach( Type t in TomlTypeConverter.GetSupportedTypes() ) {
                    typeIndexMap[t] = t.AssemblyQualifiedName;
                    indexTypeMap[t.AssemblyQualifiedName] = t;
                }
            }
        }
        #endregion
        #endregion

        #region Build the modlist
        internal static void BuildModList() {
            try {
                var mods = new List<ModInfo>();
                foreach( KeyValuePair<string, PluginInfo> kv in BepInEx.Bootstrap.Chainloader.PluginInfos ) {
                    mods.Add( new ModInfo( kv.Value ) );
                }

                localModList = new ModList(mods);
            } catch (Exception e ) {
                Fail( e, "BuildModList" );
            }
        }
        #endregion
    }
}
