using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.AddressableAssets;
using UnityEngine;

namespace R2API.AddressReferencedAssets;

/// <summary>
/// An Attribute which can be used to mark a <see cref="AddressReferencedPrefab"/> or a <see cref="AssetReferenceGameObject"/> that it requires the GameObject to have a specific component.
/// <para></para>
/// This attribute does nothing at runtime, it's meant to be an Attribute for usage in an editor environment alongside RoR2EditorKit.
/// </summary>
public class AddressableComponentRequirementAttribute : Attribute
{
    /// <summary>
    /// The required component that must exist within the addressable GameObject
    /// </summary>
    public Type requiredComponentType { get; }

    /// <summary>
    /// If true, the component will be queried using <see cref="GameObject.GetComponentInChildren(Type)"/> instead of <see cref="GameObject.TryGetComponent(Type, out Component)"/>
    /// </summary>
    public bool searchInChildren { get; set; }

    /// <summary>
    /// Constructor for <see cref="AddressableComponentRequirementAttribute"/>
    /// </summary>
    /// <param name="requiredComponentType">The required component that must exist within the addressable GameObject</param>
#pragma warning disable IDE0290 // Use primary constructor
    public AddressableComponentRequirementAttribute(Type requiredComponentType)
#pragma warning restore IDE0290 // Use primary constructor
    {
        this.requiredComponentType = requiredComponentType;
    }
}
