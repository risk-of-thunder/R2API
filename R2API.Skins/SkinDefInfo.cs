using RoR2;
using UnityEngine;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

namespace R2API;

/// <summary>
/// A container struct for all SkinDef and SkinDefParams data.
/// Use this to set skinDef values, then call CreateNewSkinDef().
/// Leave SkinDefParams null to create one automatically with the given arrays.
/// If SkinDefParams is not null, the arrays will be ignored and the SkinDefParams fields will take priority.
/// </summary>
public struct SkinDefParamsInfo
{
    /// <summary> Unity's internal name. Don't use spaces, cannot be null. </summary>
    public string Name;

    /// <summary> NameToken used for localization, or the user visible name if not found. Don't use spaces, cannot be null. </summary>
    public string NameToken;

    /// <summary> Icon associated with the skin. </summary>
    public Sprite? Icon;

    /// <summary> UnlockableDef associated with the skin. </summary>
    public UnlockableDef? UnlockableDef;

    /// <summary> Root CharacterModel object. </summary>
    public GameObject? RootObject;

    /// <summary> The skins which will be applied before this one. </summary>
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

/// <summary>
/// Use <see cref="SkinDefParamsInfo"/> instead
/// </summary>
[System.Obsolete]
public struct SkinDefInfo
{
    /// <summary> Use <see cref="SkinDefParamsInfo.BaseSkins"/> instead </summary>
    [System.Obsolete]
    public SkinDef?[]? BaseSkins;

    /// <summary> Use <see cref="SkinDefParamsInfo.Icon"/> instead  </summary>
    [System.Obsolete]
    public Sprite? Icon;

    /// <summary> Use <see cref="SkinDefParamsInfo.NameToken"/> instead  </summary>
    [System.Obsolete]
    public string? NameToken;

    /// <summary> Use <see cref="SkinDefParamsInfo.UnlockableDef"/> instead  </summary>
    [System.Obsolete]
    public UnlockableDef? UnlockableDef;

    /// <summary> Use <see cref="SkinDefParamsInfo.RootObject"/> instead  </summary>
    [System.Obsolete]
    public GameObject? RootObject;

    /// <summary> Use <see cref="SkinDefParamsInfo.RendererInfos"/> instead  </summary>
    [System.Obsolete]
    public CharacterModel.RendererInfo[]? RendererInfos;

    /// <summary> Use <see cref="SkinDefParamsInfo.MeshReplacements"/> instead  </summary>
    [System.Obsolete]
    public SkinDef.MeshReplacement[]? MeshReplacements;

    /// <summary> Use <see cref="SkinDefParamsInfo.GameObjectActivations"/> instead  </summary>
    [System.Obsolete]
    public SkinDef.GameObjectActivation[]? GameObjectActivations;

    /// <summary> Use <see cref="SkinDefParamsInfo.ProjectileGhostReplacements"/> instead  </summary>
    [System.Obsolete]
    public SkinDef.ProjectileGhostReplacement[]? ProjectileGhostReplacements;

    /// <summary> Use <see cref="SkinDefParamsInfo.MinionSkinReplacements"/> instead  </summary>
    [System.Obsolete]
    public SkinDef.MinionSkinReplacement[]? MinionSkinReplacements;

    /// <summary> Use <see cref="SkinDefParamsInfo.Name"/> instead  </summary>
    [System.Obsolete]
    public string? Name;

    // For backwards compat
    // yeah it's happened again.
    /// <summary> Converts to <see cref="SkinDefParamsInfo"/> </summary>
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
