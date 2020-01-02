using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using R2API.Utils;
using RoR2;
using RoR2.Networking;
using UnityEngine;
using UnityEngine.Networking;

namespace R2API {
    // ReSharper disable once InconsistentNaming
    [R2APISubmodule]
    public static class ModListAPI {
        //This needs to be active at all times, rather than enabled as a submodule.
        //Central idea is for any client with any mods to send a list of installed mods to server.
        //Server also sends its list to client. That, and the access to those lists is the only functionality of this API.
        //I used Try Catch excessively to hopefully ensure that any errors this causes due to future updates are zero impact.

        public const short messageIndex = 200;
        public const float messageWaitTimeServer = 1f;
        public const float messageWaitTimeClient = 1f;

        public static bool Loaded { get; private set; }

        public static ModList currentServerMods { get; private set; }
        public static Dictionary<CSteamID, ModList> connectedClientMods { get; private set; }

        public static event Action<NetworkConnection, ModList> modlistRecievedFromServer;
        public static event Action<NetworkConnection, ModList, CSteamID> modlistRecievedFromClient;

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
        //private static Dictionary<CSteamID, ModList> connectedModLists = new Dictionary<CSteamID, ModList>();
        private static ModList localModList;
        //private static ModList serverModList;
        private static ModList tempServerModList;

        private static void ServersideConnect( UnityEngine.Networking.NetworkConnection connection ) {
            try {
                ModListMessage msg = new ModListMessage( localModList, true );
                connection.SendByChannel( messageIndex, msg, QosChannelIndex.defaultReliable.intVal );
                R2API.instance.StartCoroutine( MessageWaitServer( connection, messageWaitTimeServer ) );
            } catch (Exception e ) {
                Fail( e, "ServersideConnect" );
            }
        }
        private static void ClientsideConnect( UnityEngine.Networking.NetworkConnection connection ) {
            try {
                ModListMessage msg = new ModListMessage( localModList, false );
                connection.SendByChannel( messageIndex, msg, QosChannelIndex.defaultReliable.intVal );
                R2API.instance.StartCoroutine( MessageWaitClient( connection, messageWaitTimeClient ) );
            } catch (Exception e ){
                Fail( e, "ClientsideConnect" );
            }
        }

        private static IEnumerator MessageWaitServer( NetworkConnection conn, float duration ) {
            yield return new WaitForSeconds( duration );
            ModList list = null;
            if( tempConnectionInfo.ContainsKey( conn ) ) {
                list = tempConnectionInfo[conn];
            } else {
                list = ModList.vanilla;
            }
            CSteamID steamID = ServerAuthManager.FindAuthData( conn ).steamId;
            connectedClientMods[steamID] = list;
            modlistRecievedFromClient?.Invoke( conn, list, steamID );
            if( tempConnectionInfo.ContainsKey( conn ) ) {
                tempConnectionInfo.Remove( conn );
            }
        }

        private static IEnumerator MessageWaitClient( NetworkConnection conn, float duration ) {
            yield return new WaitForSeconds( duration );

            ModList list = null;
            if( tempServerModList != null ) {
                list = tempServerModList;
            } else {
                list = ModList.vanilla;
            }
            currentServerMods = list;
            modlistRecievedFromServer?.Invoke( conn, list );
            tempServerModList = null;
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
            public ModList mods { get; private set; }
            public bool fromServer { get; private set; }

            public ModListMessage( ModList mods, bool fromServer ) {
                this.mods = mods;
                this.fromServer = fromServer;
            }

            public ModListMessage() { }

            public override void Serialize( NetworkWriter writer ) {
                writer.Write( this.fromServer);
                this.mods.Write( writer );
            }

            public override void Deserialize( NetworkReader reader ) {
                this.fromServer = reader.ReadBoolean();
                this.mods = ModList.Read( reader );
            }
        }
        #endregion

        #region Setting up the modlist classes
        public class ModList {
            private List<ModInfo> modInfos = new List<ModInfo>();

            [System.Runtime.CompilerServices.IndexerName( "mods" )]
            public ModInfo this[int index] {
                get {
                    return this.modInfos[index];
                }
                private set {
                    this.modInfos[index] = value;
                }
            }

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

            public bool isVanilla {
                get {
                    return this.modInfos == null || this.modInfos.Count == 0;
                }
            }

            public void Write( NetworkWriter writer ) {
                writer.Write( this.modInfos.Count );
                foreach( ModInfo mod in this.modInfos ) {
                    mod.Write( writer );
                }
            }

            private static ModList intVanilla;
            public static ModList vanilla {
                get {
                    if( intVanilla == null ) intVanilla = new ModList();
                    return intVanilla;
                }
            }

            public static ModList Read( NetworkReader reader ) {
                var count = reader.ReadInt32();
                var mods = new List<ModInfo>(count);
                for( Int32 i = 0; i < count; i++ ) {
                    mods.Add( ModInfo.Read( reader ) );
                }
                return new ModList( mods );
            }

            public ModList( List<ModInfo> mods ) {
                this.modInfos = mods;
            }

            internal ModList() {
                this.modInfos = new List<ModInfo>();
            }
        }

        public class ModInfo {
            public String guid { get; private set; }
            public System.Version version { get; private set; }
            public bool hasConfig {
                get {
                    return this.configData != null;
                }
            }
            public ConfigData configData { get; private set; }


            public ModInfo( PluginInfo plugin ) {
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
            public void Write( NetworkWriter writer ) {
                writer.Write( this.guid );
                writer.Write( this.version.ToString() );
                writer.Write( this.hasConfig );
                this.configData.Write( writer );
            }
            public static ModInfo Read( NetworkReader reader ) {
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
        public class ConfigData {
            private Dictionary< string, ConfigOption > data = new Dictionary<string, ConfigOption>();
            public ConfigData( ConfigFile config ) {
                foreach( ConfigDefinition def in config.Keys ) {
                    this.data[def.Section + ":-:" + def.Key] = new ConfigOption( config[def] );
                }
            }

            private ConfigData() { }

            public void Write( NetworkWriter writer ) {
                writer.Write( this.data.Count );
                foreach( KeyValuePair<string, ConfigOption> dataEntry in this.data ) {
                    writer.Write( dataEntry.Key );
                    dataEntry.Value.Write( writer );
                }
            }

            public static ConfigData Read( NetworkReader reader ) {
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
        }

        public class ConfigOption {
            public ConfigOption( ConfigEntryBase entry ) {
                this.type = entry.SettingType;
                this.value = entry.BoxedValue;
            }

            public ConfigOption( Type type, object value ) {
                this.type = type;
                this.value = value;
            }

            public Type type { get; private set; }
            public object value { get; private set; }

            public void Write( NetworkWriter writer ) {
                writer.Write( GetTypeIndex(this.type) );
                writer.Write( TomlTypeConverter.GetConverter( this.type ).ConvertToString( this.value, this.type ) );
            }

            public static ConfigOption Read( NetworkReader reader ) {
                var typeString = reader.ReadString();
                var objString = reader.ReadString();
                var type = GetIndexType( typeString );
                var obj = TomlTypeConverter.GetConverter( type ).ConvertToObject( objString, type);
                return new ConfigOption( type, obj );
            }

            public static Dictionary<Type,string> typeIndexMap = null;
            public static Dictionary<string,Type> indexTypeMap = null;
            public static string GetTypeIndex( Type type ) {
                if( typeIndexMap == null || indexTypeMap == null ) BuildTypeIndexMap();

                if( typeIndexMap.ContainsKey( type ) ) return typeIndexMap[type];

                R2API.Logger.LogError( "Invalid type found in Bepinex Config." );
                return "";
            }

            public static Type GetIndexType( string index ) {
                if( typeIndexMap == null || indexTypeMap == null ) BuildTypeIndexMap();

                return indexTypeMap[index];
            }

            public static void BuildTypeIndexMap() {
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
