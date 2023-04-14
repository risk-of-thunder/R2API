using RoR2;
using UnityEngine;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

namespace R2API;

/// <summary>
/// A container struct for all SkinDef parameters.
/// Use this to set skinDef values, then call CreateNewSkinDef().
/// </summary>
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
}
