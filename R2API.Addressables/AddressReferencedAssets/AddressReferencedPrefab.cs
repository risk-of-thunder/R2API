using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace R2API.AddressReferencedAssets;

/// <summary>
/// <inheritdoc cref="AddressReferencedAsset{T}"/>
/// <br>T is <see cref="GameObject"/></br>
/// </summary>
[Serializable]
public class AddressReferencedPrefab : AddressReferencedAsset<GameObject>
{
    /// <summary>
    /// Operator for casting <see cref="AddressReferencedPrefab"/> to a boolean value
    /// <br>Allows you to keep using the unity Syntax for checking if an object exists.</br>
    /// </summary>
    public static implicit operator bool(AddressReferencedPrefab addressReferencedAsset)
    {
        return addressReferencedAsset?.Asset;
    }

    /// <summary>
    /// Operator for casting <see cref="AddressReferencedPrefab"/> to it's currently loaded <see cref="AddressReferencedAsset{T}.Asset"/> value
    /// </summary>
    public static implicit operator GameObject(AddressReferencedPrefab addressReferencedAsset)
    {
        return addressReferencedAsset?.Asset;
    }

    /// <summary>
    /// Operator for encapsulating a <see cref="string"/> inside an <see cref="AddressReferencedPrefab"/>
    /// </summary>
    public static implicit operator AddressReferencedPrefab(string address)
    {
        return new AddressReferencedPrefab(address);
    }

    /// <summary>
    /// Operator for encapsulating an <see cref="GameObject"/> inside an <see cref="AddressReferencedPrefab"/>
    /// </summary>
    public static implicit operator AddressReferencedPrefab(GameObject asset)
    {
        return new AddressReferencedPrefab(asset);
    }

    /// <summary>
    /// <inheritdoc cref="AddressReferencedAsset{T}.AddressReferencedAsset()"/>
    /// <br>T is <see cref="GameObject"/></br>
    /// </summary>
    public AddressReferencedPrefab() : base() { }

    /// <summary>
    /// <inheritdoc cref="AddressReferencedAsset{T}.AddressReferencedAsset(T)"/>
    /// <br>T is <see cref="GameObject"/></br>
    /// </summary>
    public AddressReferencedPrefab(GameObject gameObject) : base(gameObject) { }

    /// <summary>
    /// <inheritdoc cref="AddressReferencedAsset{T}.AddressReferencedAsset(string)"/>
    /// <br>T is <see cref="GameObject"/></br>
    /// </summary>
    public AddressReferencedPrefab(string address) : base(address) { }
}
