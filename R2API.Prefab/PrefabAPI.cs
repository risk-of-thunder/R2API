using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using R2API.AutoVersionGen;
using R2API.ContentManagement;
using R2API.Utils;
using UnityEngine;
using UnityEngine.Networking;
using UnityObject = UnityEngine.Object;

// ReSharper disable UnusedMember.Global

namespace R2API;

// ReSharper disable once InconsistentNaming
#pragma warning disable CS0436 // Type conflicts with imported type
[AutoVersion]
#pragma warning restore CS0436 // Type conflicts with imported type
public static partial class PrefabAPI
{
    public const string PluginGUID = R2API.PluginGUID + ".prefab";
    public const string PluginName = R2API.PluginName + ".Prefab";

    /// <summary>
    /// Return true if the submodule is loaded.
    /// </summary>
    // ReSharper disable once MemberCanBePrivate.Global
    // ReSharper disable once ConvertToAutoProperty
#pragma warning disable CS0618 // Type or member is obsolete
    [Obsolete(R2APISubmoduleDependency.PropertyObsolete)]
#pragma warning restore CS0618 // Type or member is obsolete
    public static bool Loaded => true;

    private static GameObject _parent;
    private static readonly List<HashStruct> _networkedPrefabs = new();

    /// <summary>
    /// Is the prefab network registered
    /// </summary>
    /// <param name="prefabToCheck"></param>
    /// <returns></returns>
    public static bool IsPrefabHashed(GameObject prefabToCheck) => _networkedPrefabs.Select(hash => hash.Prefab).Contains(prefabToCheck);

#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)

    /// <summary>
    /// Duplicates a GameObject and leaves it in a "sleeping" state where it is inactive, but becomes active when spawned.
    /// Also registers the clone to network.
    /// </summary>
    /// <param name="g">The GameObject to clone</param>
    /// <param name="nameToSet">The name to give the clone (Should be unique)</param>
    /// <returns>The GameObject of the clone</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static GameObject InstantiateClone(this GameObject g, string nameToSet)
    {
        return InstantiateCloneInternal(g, nameToSet, true);
    }

    /// <summary>
    /// Duplicates a GameObject and leaves it in a "sleeping" state where it is inactive, but becomes active when spawned.
    /// Also registers the clone to network if registerNetwork is not set to false.
    /// </summary>
    /// <param name="g">The GameObject to clone</param>
    /// <param name="nameToSet">The name to give the clone (Should be unique)</param>
    /// <param name="registerNetwork">Should the object be registered to network</param>
    /// <returns>The GameObject of the clone</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static GameObject InstantiateClone(this GameObject g, string nameToSet, bool registerNetwork)
    {
        return InstantiateCloneInternal(g, nameToSet, registerNetwork);
    }

    [Obsolete("Left over to not break old mods")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static GameObject InstantiateClone(this GameObject? g, string? nameToSet, bool registerNetwork = true, [CallerFilePath] string? file = "", [CallerMemberName] string? member = "", [CallerLineNumber] int line = 0)
    {
        return InstantiateCloneInternal(g, nameToSet, registerNetwork);
    }

    private static GameObject InstantiateCloneInternal(this GameObject g, string nameToSet, bool registerNetwork)
    {
        var prefab = UnityObject.Instantiate(g, GetParent().transform);
        prefab.name = nameToSet;
        if (registerNetwork)
        {
            RegisterPrefabInternal(prefab, new StackFrame(2));
        }
        return prefab;
    }

    /// <summary>
    /// Registers a prefab so that NetworkServer.Spawn will function properly with it.
    /// Only will work on prefabs with a NetworkIdentity component.
    /// Is never needed for existing objects unless you have cloned them.
    /// </summary>
    /// <param name="g">The prefab to register</param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void RegisterNetworkPrefab(this GameObject g)
    {
        RegisterNetworkPrefabInternal(g);
    }

    [Obsolete("Left over to not break old mods.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void RegisterNetworkPrefab(this GameObject? g, [CallerFilePath] string? file = "", [CallerMemberName] string? member = "", [CallerLineNumber] int line = 0)
    {
        RegisterNetworkPrefabInternal(g);
    }

    private static void RegisterNetworkPrefabInternal(GameObject g)
    {
        RegisterPrefabInternal(g, new StackFrame(2));
    }

#pragma warning restore CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)

    private static GameObject GetParent()
    {
        if (!_parent)
        {
            const string ModdedPrefabsGameObjectHolderName = "ModdedPrefabs";

            _parent = new GameObject(ModdedPrefabsGameObjectHolderName);
            UnityObject.DontDestroyOnLoad(_parent);
            _parent.hideFlags = HideFlags.HideAndDontSave;
            _parent.SetActive(false);

            On.RoR2.Util.IsPrefab += (orig, obj) =>
            {
                if (obj.transform.parent && obj.transform.parent.gameObject.name == ModdedPrefabsGameObjectHolderName) return true;
                return orig(obj);
            };
        }

        return _parent;
    }

    private struct HashStruct
    {
        public GameObject Prefab;
        public string GoName;
        public string TypeName;
        public string MethodName;
        public Assembly Assembly;
    }

    private static void RegisterPrefabInternal(GameObject prefab, StackFrame frame)
    {
        var method = frame.GetMethod();
        var h = new HashStruct
        {
            Prefab = prefab,
            GoName = prefab.name,
            TypeName = method.DeclaringType.AssemblyQualifiedName,
            MethodName = method.Name,
            Assembly = method.DeclaringType.Assembly
        };
        _networkedPrefabs.Add(h);

        static void AddToNetworkedObjectPrefabs(HashStruct h)
        {
            var contentPack = R2APIContentManager.GetOrCreateSerializableContentPack(h.Assembly);
            var networkedObjectPrefabs = contentPack.networkedObjectPrefabs.ToList();
            networkedObjectPrefabs.Add(h.Prefab);
            contentPack.networkedObjectPrefabs = networkedObjectPrefabs.ToArray();
        }

        AddToNetworkedObjectPrefabs(h);

        var networkIdentity = h.Prefab.GetComponent<NetworkIdentity>();
        if (networkIdentity)
        {
            networkIdentity.SetFieldValue("m_AssetId", NetworkHash128.Parse(MakeHash(h.GoName + h.TypeName + h.MethodName)));
        }
        else
        {
            // some mod creators are adding the NetworkIdentity component after the call to InstantiateClone,
            // and are not calling PrefabAPI.RegisterNetworkPrefab after that.
            // we need to do this else its a breaking change compared to the old behavior.

            _prefabsWithDelayedAddedNetworkIdentities.Add(h);

            if (!_delayedPrefabsHookEnabled)
            {
                On.RoR2.Networking.NetworkManagerSystem.OnStartClient += SetProperAssetIdForDelayedPrefabs;

                _delayedPrefabsHookEnabled = true;
            }
        }
    }

    private static bool _delayedPrefabsHookEnabled = false;
    private static List<HashStruct> _prefabsWithDelayedAddedNetworkIdentities = new();
    private static void SetProperAssetIdForDelayedPrefabs(On.RoR2.Networking.NetworkManagerSystem.orig_OnStartClient orig, RoR2.Networking.NetworkManagerSystem self, NetworkClient newClient)
    {
        try
        {
            foreach (var h in _prefabsWithDelayedAddedNetworkIdentities)
            {
                var networkIdentity = h.Prefab.GetComponent<NetworkIdentity>();
                if (networkIdentity)
                {
                    networkIdentity.SetFieldValue("m_AssetId", NetworkHash128.Parse(MakeHash(h.GoName + h.TypeName + h.MethodName)));
                }
                else
                {
                    PrefabPlugin.Logger.LogError($"{h.Prefab} don't have a NetworkIdentity Component but was marked for network registration.");
                }
            }
        }
        catch (Exception e)
        {
            PrefabPlugin.Logger.LogError(e);
        }

        orig(self, newClient);
    }

    private static string MakeHash(string s)
    {
        var hash = MD5.Create();
        byte[] prehash = hash.ComputeHash(Encoding.UTF8.GetBytes(s));
        hash.Dispose();
        var sb = new StringBuilder();

        foreach (var t in prehash)
        {
            sb.Append(t.ToString("x2"));
        }

        return sb.ToString();
    }
}
