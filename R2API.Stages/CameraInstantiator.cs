using System;
using UnityEngine;

namespace R2API;

/// <summary>
/// Instantiates the RoR2 main camera, which allows preview of post processing effects
/// <para>Do not leave this on finalized builds, as it causes errors</para>
/// </summary>
[ExecuteAlways]
public class CameraInstantiator : MonoBehaviour
{
    public const string CAMERA_ADDRESS = "RoR2/Base/Core/Main Camera.prefab";
    public GameObject CameraInstance { get => _cameraInstance; private set => _cameraInstance = value; }
    [NonSerialized] private GameObject _cameraInstance;
    private void OnEnable() => Refresh();
    private void OnDisable() => InstantiateAddressablePrefab.DestroyImmediateSafe(CameraInstance, true);

    /// <summary>
    /// Instantiates the camera or destroys the attached game object if the component is instantiated at runtime and not in the editor.
    /// </summary>
    public void Refresh()
    {
        if (Application.isPlaying && !Application.isEditor)
        {
            Log.Fatal($"Lingering camera injector in {gameObject}, Ensure that these scripts are NOT present on finalized builds!!!");
            Destroy(gameObject);
            return;
        }

        if (CameraInstance)
        {
            InstantiateAddressablePrefab.DestroyImmediateSafe(CameraInstance, true);
        }
        var go = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<GameObject>(CAMERA_ADDRESS).WaitForCompletion();
        CameraInstance = Instantiate(go, transform);
        CameraInstance.name = $"[EDITOR ONLY] {CameraInstance.name}";
        CameraInstance.hideFlags |= HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild | HideFlags.NotEditable;
        foreach (Transform t in CameraInstance.GetComponentsInChildren<Transform>())
        {
            t.gameObject.hideFlags = CameraInstance.hideFlags | HideFlags.HideInHierarchy;
        }
        CameraInstance.hideFlags &= ~HideFlags.HideInHierarchy;
    }


}
