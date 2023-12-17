using RoR2;
using UnityEngine;

namespace R2API;

/// <summary>
/// A container struct for a skin-specific VFX.
/// When the specified VFX would be spawned and the spawner's SkinDef matches, it will be modified by the values here.
/// Call AddSkinVFX() to register this. 
/// </summary>
public class SkinVFXInfo {
    /// <summary>
    /// SkinDef required before this replacement is applied.
    /// </summary>
    public SkinDef RequiredSkin;
    /// <summary>
    /// EffectIndex of the effect that should be replaced.
    /// </summary>
    public EffectIndex TargetEffect = EffectIndex.Invalid;
    /// <summary>
    /// A replacement prefab to spawn instead of the effect. This will be used instead of OnEffectSpawn if assigned.
    /// </summary>
    public GameObject? ReplacementEffectPrefab;
    /// <summary>
    /// A delegate that will be called when the effect is spawned by a character with a matching SkinDef.
    /// </summary>
    public SkinVFX.OnEffectSpawnedDelegate? OnEffectSpawned;
    /// <summary>
    /// An identifier used to track whether or not this effect met the skin condition. This will be automatically assigned, and shouldn't be modified.
    /// </summary>
    public uint Identifier = uint.MaxValue;

    /// <summary>
    /// The prefab of the effect that should be replaced. This will automatically fill out TargetEffect on EffectCatalog.Init
    /// </summary>
    public GameObject EffectPrefab;
    /// <summary>
    /// The name of the effect that should be replaced. This will automatically fill out TargetEffect on EffectCatalog.Init
    /// </summary>
    public string EffectString;
}