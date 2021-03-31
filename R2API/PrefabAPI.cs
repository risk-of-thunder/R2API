using R2API.Utils;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityObject = UnityEngine.Object;

// ReSharper disable UnusedMember.Global

namespace R2API {

    // ReSharper disable once InconsistentNaming
    [R2APISubmodule]
    public static class PrefabAPI {

        /// <summary>
        /// Return true if the submodule is loaded.
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once ConvertToAutoProperty
        public static bool Loaded {
            get => _loaded;
            internal set => _loaded = value;
        }

        private static bool _loaded;

        private static bool needToRegister;
        private static GameObject _parent;
        private static readonly List<HashStruct> ThingsToHash = new List<HashStruct>();

#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)

        /// <summary>
        /// Duplicates a GameObject and leaves it in a "sleeping" state where it is inactive, but becomes active when spawned.
        /// Also registers the clone to network if registerNetwork is not set to false.
        /// Do not override the file, member, and line number parameters. They are used to generate a unique hash for the network ID.
        /// </summary>
        /// <param name="g">The GameObject to clone</param>
        /// <param name="nameToSet">The name to give the clone (Should be unique)</param>
        /// <param name="registerNetwork">Should the object be registered to network</param>
        /// <returns>The GameObject of the clone</returns>
        public static GameObject InstantiateClone(this GameObject? g, string? nameToSet, bool registerNetwork = true, [CallerFilePath] string? file = "", [CallerMemberName] string? member = "", [CallerLineNumber] int line = 0) {
            if (!Loaded) {
                throw new InvalidOperationException($"{nameof(PrefabAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(PrefabAPI)})]");
            }
            var prefab = UnityObject.Instantiate(g, GetParent().transform);
            prefab.name = nameToSet;
            if (registerNetwork) {
                RegisterPrefabInternal(prefab, file, member, line);
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
        public static void RegisterNetworkPrefab(this GameObject? g, [CallerFilePath] string? file = "", [CallerMemberName] string? member = "", [CallerLineNumber] int line = 0) {
            if (!Loaded) {
                R2API.Logger.LogError("PrefabAPI is not loaded. Please use [R2API.Utils.SubModuleDependency]");
                return;
            }
            RegisterPrefabInternal(g, file, member, line);
        }

#pragma warning restore CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)

        [R2APISubmoduleInit(Stage = InitStage.SetHooks)]
        internal static void SetHooks() {
        }

        [R2APISubmoduleInit(Stage = InitStage.UnsetHooks)]
        internal static void UnsetHooks() {
        }

        private static GameObject GetParent() {
            if (!_parent) {
                _parent = new GameObject("ModdedPrefabs");
                UnityObject.DontDestroyOnLoad(_parent);
                _parent.SetActive(false);

                On.RoR2.Util.IsPrefab += (orig, obj) => {
                    if (obj.transform.parent && obj.transform.parent.gameObject.name == "ModdedPrefabs") return true;
                    return orig(obj);
                };
            }

            return _parent;
        }

        private struct HashStruct {
            public GameObject Prefab;
            public string GoName;
            public string CallPath;
            public string CallMember;
            public int CallLine;
        }

        private static void RegisterPrefabInternal(GameObject prefab, string callPath, string callMember, int callLine) {
            var h = new HashStruct {
                Prefab = prefab,
                GoName = prefab.name,
                CallPath = callPath,
                CallMember = callMember,
                CallLine = callLine
            };
            ThingsToHash.Add(h);
            SetupRegistrationEvent();
        }

        private static void SetupRegistrationEvent() {
            if (!needToRegister) {
                needToRegister = true;
                RoR2.Networking.GameNetworkManager.onStartGlobal += RegisterClientPrefabsNStuff;
            }
        }

        private static readonly NetworkHash128 NullHash = new NetworkHash128 {
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

        private static void RegisterClientPrefabsNStuff() {
            foreach (var h in ThingsToHash) {
                if (h.Prefab.GetComponent<NetworkIdentity>() != null) h.Prefab.GetComponent<NetworkIdentity>().SetFieldValue("m_AssetId", NullHash);
                ClientScene.RegisterPrefab(h.Prefab, NetworkHash128.Parse(MakeHash(h.GoName + h.CallPath + h.CallMember + h.CallLine)));
            }
        }

        private static string MakeHash(string s) {
            var hash = MD5.Create();
            byte[] prehash = hash.ComputeHash(Encoding.UTF8.GetBytes(s));
            hash.Dispose();
            var sb = new StringBuilder();

            foreach (var t in prehash) {
                sb.Append(t.ToString("x2"));
            }

            return sb.ToString();
        }
    }
}
