using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace R2API;

/// <summary>
/// Instantiates the prefab specified in <see cref="address"/>
/// </summary>
[ExecuteAlways]
public class InstantiateAddressablePrefab : MonoBehaviour
{
    [Tooltip("The address to use to load the prefab")]
    [SerializeField] private string address;
    [Tooltip("When the prefab is instantiated, and this is true, the prefab's position and rotation will be set to 0")]
    [SerializeField] private bool setPositionAndRotationToZero;
    [Tooltip("setPositionAndRotationToZero would work relative to it's parent")]
    [SerializeField] private bool useLocalPositionAndRotation;
    [Tooltip("Wether the Refresh method will be called in the editor")]
    [SerializeField] private bool refreshInEditor;
    [SerializeField, HideInInspector] private bool hasNetworkIdentity;

    /// <summary>
    /// The instantiated prefab
    /// </summary>
    public GameObject Instance => instance;
    [NonSerialized]
    private GameObject instance;

    private void OnEnable() => Refresh();
    private void OnDisable()
    {
        if (instance)
            DestroyImmediateSafe(instance, true);
    }
    /// <summary>
    /// Destroys the instantiated object and re-instantiates using the prefab that's loaded via <see cref="address"/>
    /// </summary>
    public void Refresh()
    {
        if (Application.isEditor && !refreshInEditor)
            return;

        if (instance)
        {
            DestroyImmediateSafe(instance, true);
        }

        if (string.IsNullOrWhiteSpace(address) || string.IsNullOrEmpty(address))
        {
            Log.Warning($"Invalid address in {this}, address is null, empty, or white space");
            return;
        }

        GameObject prefab = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<GameObject>(address).WaitForCompletion();
        hasNetworkIdentity = prefab.GetComponent<NetworkIdentity>();

        if (hasNetworkIdentity && !Application.isEditor)
        {
            if (NetworkServer.active)
            {
                instance = Instantiate(prefab, transform);
                NetworkServer.Spawn(instance);
            }
        }
        else
        {
            instance = Instantiate(prefab, transform);
        }

        instance.hideFlags |= HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild | HideFlags.NotEditable;
        foreach (Transform t in instance.GetComponentsInChildren<Transform>())
        {
            t.gameObject.hideFlags = instance.hideFlags;
        }
        if (setPositionAndRotationToZero)
        {
            Transform t = instance.transform;
            if (useLocalPositionAndRotation)
            {
                t.localPosition = Vector3.zero;
                t.localRotation = Quaternion.identity;
            }
            else
            {
                t.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            }
        }
    }

    public static void DestroyImmediateSafe(UnityEngine.Object obj, bool allowDestroyingAssets = false)
    {
#if UNITY_EDITOR
        GameObject.DestroyImmediate(obj, allowDestroyingAssets);
#else
        GameObject.Destroy(obj);
#endif
    }
}
