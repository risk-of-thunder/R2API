using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text;
using BepInEx;
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
        public const float messageWaitTimeServer = 10f;
        public const float messageWaitTimeClient = 5f;

        public static bool Loaded { get; private set; }

        public static event Action<NetworkConnection, ModList> modlistRecievedFromServer;
        public static event Action<NetworkConnection, ModList, CSteamID> modlistRecievedFromClient;

        internal static void Init() {
            try {
                Loaded = true;
                localModList = BuildModList();
                On.RoR2.Networking.NetworkMessageHandlerAttribute.CollectHandlers += ScanForNetworkAttributes;
                GameNetworkManager.onClientConnectGlobal += ClientsideConnect;
                GameNetworkManager.onServerConnectGlobal += ServersideConnect;

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
        private static Dictionary<CSteamID, ModList> connectedModLists = new Dictionary<CSteamID, ModList>();
        private static ModList localModList;
        private static ModList serverModList;
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
                // This will very likely break on pirated copies of the game. Sounds like a good feature to me.
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
            connectedModLists[steamID] = list;
            modlistRecievedFromClient?.Invoke( conn, list, steamID );
        }

        private static IEnumerator MessageWaitClient( NetworkConnection conn, float duration ) {
            yield return new WaitForSeconds( duration * 0.5f );

            ModList list = null;
            if( tempServerModList != null ) {
                list = tempServerModList;
            } else {
                list = ModList.vanilla;
            }
            serverModList = list;
            modlistRecievedFromServer?.Invoke( conn, list );
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

            public ModInfo( PluginInfo plugin ) {
                this.guid = plugin.Metadata.GUID;
                this.version = plugin.Metadata.Version;
            }
            private ModInfo( String guid, System.Version version ) {
                this.guid = guid;
                this.version = version;
            }
            public void Write( NetworkWriter writer ) {
                writer.Write( this.guid );
                writer.Write( this.version.ToString() );
            }
            public static ModInfo Read( NetworkReader reader ) {
                var guid = reader.ReadString();
                var version = new System.Version(reader.ReadString());
                return new ModInfo( guid, version );
            }
        }
        #endregion

        #region Build the modlist
        private static ModList BuildModList() {
            var mods = new List<ModInfo>();
            foreach( KeyValuePair<string, PluginInfo> kv in BepInEx.Bootstrap.Chainloader.PluginInfos ) {
                mods.Add( new ModInfo( kv.Value ) );
            }
            return new ModList( mods );
        }
        #endregion
    }
}
