using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using R2API.Utils;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace R2API {
    // ReSharper disable once InconsistentNaming
    [R2APISubmodule]
    public static class PrefabAPI {

        private static Boolean needToRegister = false;
        private static GameObject parent;
        private static List<HashStruct> thingsToHash = new List<HashStruct>();

        /// <summary>
        /// Duplicates a GameObject and leaves it in a "sleeping" state where it is inactive, but becomes active when spawned.
        /// Also registers the clone to network if registerNetwork is not set to false.
        /// Do not override the file, member, and line number parameters. They are used to generate a unique hash for the network ID.
        /// </summary>
        /// <param name="g">The GameObject to clone</param>
        /// <param name="nameToSet">The name to give the clone (Should be unique)</param>
        /// <param name="registerNetwork">Should the object be registered to network</param>
        /// <returns>The GameObject of the clone</returns>
        public static GameObject InstantiateClone( this GameObject g, System.String nameToSet, System.Boolean registerNetwork = true, [CallerFilePath] System.String file = "", [CallerMemberName] System.String member = "", [CallerLineNumber] System.Int32 line = 0 ) {
            GameObject prefab = MonoBehaviour.Instantiate<GameObject>(g, GetParent().transform);
            prefab.name = nameToSet;
            if( registerNetwork ) {
                RegisterPrefabInternal( prefab, file, member, line );
            }
            return prefab;
        }

        /// <summary>
        /// Registers a prefab so that NetworkServer.Spawn will function properly with it.
        /// Only will work on prefabs with a NetworkIdentity component.
        /// Is never needed for existing objects unless you have cloned them.
        /// Do not override the file, member, and line number parameters. They are used to generate a unique hash for the network ID.
        /// </summary>
        /// <param name="g">The prefab to register</param>
        public static void RegisterNetworkPrefab( this GameObject g, [CallerFilePath] System.String file = "", [CallerMemberName] System.String member = "", [CallerLineNumber] System.Int32 line = 0 ) {
            RegisterPrefabInternal( g, file, member, line );
        }
        
        [R2APISubmoduleInit(Stage = InitStage.SetHooks)]
        internal static void SetHooks() {

        }

        [R2APISubmoduleInit(Stage = InitStage.UnsetHooks)]
        internal static void UnsetHooks() {

        }

        private static GameObject GetParent() {
            if( !parent ) {
                parent = new GameObject( "ModdedPrefabs" );
                MonoBehaviour.DontDestroyOnLoad( parent );
                parent.SetActive( false );

                On.RoR2.Util.IsPrefab += ( orig, obj ) => {
                    if( obj.transform.parent && obj.transform.parent.gameObject.name == "ModdedPrefabs" ) return true;
                    return orig( obj );
                };
            }

            return parent;
        }

        private struct HashStruct {
            public GameObject prefab;
            public System.String goName;
            public System.String callPath;
            public System.String callMember;
            public System.Int32 callLine;
        }

        private static void RegisterPrefabInternal( GameObject prefab, System.String callPath, System.String callMember, System.Int32 callLine ) {
            HashStruct h = new HashStruct
            {
                prefab = prefab,
                goName = prefab.name,
                callPath = callPath,
                callMember = callMember,
                callLine = callLine
            };
            thingsToHash.Add( h );
            SetupRegistrationEvent();
        }

        private static void SetupRegistrationEvent() {
            if( !needToRegister ) {
                needToRegister = true;
                On.RoR2.Networking.GameNetworkManager.OnStartClient += RegisterClientPrefabsNStuff;
            }
        }

        private static NetworkHash128 nullHash = new NetworkHash128
        {
            i0 = 0,
            i1 = 0,
            i2 = 0,
            i3 = 0,
            i4 = 0,
            i5 = 0,
            i6 = 0,
            i7 = 0,
            i8 = 0,
            i9 = 0,
            i10 = 0,
            i11 = 0,
            i12 = 0,
            i13 = 0,
            i14 = 0,
            i15 = 0
        };

        private static void RegisterClientPrefabsNStuff( On.RoR2.Networking.GameNetworkManager.orig_OnStartClient orig, RoR2.Networking.GameNetworkManager self, UnityEngine.Networking.NetworkClient newClient ) {
            orig( self, newClient );
            foreach( HashStruct h in thingsToHash ) {
                if( (h.prefab.GetComponent<NetworkIdentity>() != null)) h.prefab.GetComponent<NetworkIdentity>().SetFieldValue<NetworkHash128>( "m_AssetId", nullHash );
                ClientScene.RegisterPrefab( h.prefab, NetworkHash128.Parse( MakeHash( h.goName + h.callPath + h.callMember + h.callLine.ToString() ) ) );
            }
        }

        private static System.String MakeHash( System.String s ) {
            MD5 hash = MD5.Create();
            System.Byte[] prehash = hash.ComputeHash( Encoding.UTF8.GetBytes( s ) );

            StringBuilder sb = new StringBuilder();

            for( System.Int32 i = 0; i < prehash.Length; i++ ) {
                sb.Append( prehash[i].ToString( "x2" ) );
            }

            return sb.ToString();
        }
    }
}
