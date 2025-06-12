using HG;
using RoR2;
using UnityEngine;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

namespace R2API;

/// <summary>
/// Use SkinDefParamsInfo instead
/// </summary>
[System.Obsolete]
public struct SkinDefInfo
{
    public SkinDef?[]? BaseSkins;
    public Sprite? Icon;
    public string? NameToken;
    public UnlockableDef? UnlockableDef;
    public GameObject? RootObject;
    public CharacterModel.RendererInfo[]? RendererInfos;
    public SkinDef.MeshReplacement[]? MeshReplacements;
    public SkinDef.GameObjectActivation[]? GameObjectActivations;
    public SkinDef.ProjectileGhostReplacement[]? ProjectileGhostReplacements;
    public SkinDef.MinionSkinReplacement[]? MinionSkinReplacements;
    public string? Name;

    // For backwards compat
    // yeah it's happened again.
    public static implicit operator SkinDefParamsInfo(SkinDefInfo orig)
    {
        var skinDefParams = new SkinDefParamsInfo
        {
            BaseSkins = orig.BaseSkins,
            Icon = orig.Icon,
            NameToken = orig.NameToken,
            UnlockableDef = orig.UnlockableDef,
            RootObject = orig.RootObject,
            RendererInfos = orig.RendererInfos,
            Name = orig.Name,
        };

        if (orig.GameObjectActivations is not null)
        {
            skinDefParams.GameObjectActivations = new SkinDefParams.GameObjectActivation[orig.GameObjectActivations.Length];
            for (int i = 0; i < orig.GameObjectActivations.Length; i++)
            {
                skinDefParams.GameObjectActivations[i] = orig.GameObjectActivations[i];
            }
        }

        if (orig.MeshReplacements is not null)
        {
            skinDefParams.MeshReplacements = new SkinDefParams.MeshReplacement[orig.MeshReplacements.Length];
            for (int j = 0; j < orig.MeshReplacements.Length; j++)
            {
                skinDefParams.MeshReplacements[j] = orig.MeshReplacements[j];
            }
        }

        if (orig.ProjectileGhostReplacements is not null)
        {
            skinDefParams.ProjectileGhostReplacements = new SkinDefParams.ProjectileGhostReplacement[orig.ProjectileGhostReplacements.Length];
            for (int k = 0; k < orig.ProjectileGhostReplacements.Length; k++)
            {
                skinDefParams.ProjectileGhostReplacements[k] = orig.ProjectileGhostReplacements[k];
            }
        }

        if (orig.MinionSkinReplacements is not null)
        {
            skinDefParams.MinionSkinReplacements = new SkinDefParams.MinionSkinReplacement[orig.MinionSkinReplacements.Length];
            for (int l = 0; l < orig.MinionSkinReplacements.Length; l++)
            {
                skinDefParams.MinionSkinReplacements[l] = orig.MinionSkinReplacements[l];
            }
        }

        return skinDefParams;
    }
}

/// <summary>
/// A container struct for all SkinDef and SkinDefParams parameters.
/// Use this to set skinDef values, then call CreateNewSkinDef().
/// Leave SkinDefParams null to create one automatically with the given arrays.
/// Otherwise, the arrays will be ignored and the SkinDefParams fields will take priority.
/// </summary>
public struct SkinDefParamsInfo
{
    public string? Name;
    public string? NameToken;
    public Sprite? Icon;
    public UnlockableDef? UnlockableDef;
    public GameObject? RootObject;
    public SkinDef?[]? BaseSkins;
    /// <summary> Leave null to create this automatically. </summary>
    public SkinDefParams? SkinDefParams;
    /// <summary> Used when SkinDefParams is null. </summary>
    public CharacterModel.RendererInfo[]? RendererInfos;
    /// <summary> Used when SkinDefParams is null. </summary>
    public SkinDefParams.MeshReplacement[]? MeshReplacements;
    /// <summary> Used when SkinDefParams is null. </summary>
    public SkinDefParams.GameObjectActivation[]? GameObjectActivations;
    /// <summary> Used when SkinDefParams is null. </summary>
    public SkinDefParams.ProjectileGhostReplacement[]? ProjectileGhostReplacements;
    /// <summary> Used when SkinDefParams is null. </summary>
    public SkinDefParams.MinionSkinReplacement[]? MinionSkinReplacements;
}
