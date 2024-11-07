using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace R2API.AddressReferencedAssets;

/// <summary>
/// <inheritdoc cref="AddressReferencedAsset{T}"/>
/// <br>T is <see cref="FamilyDirectorCardCategorySelection"/></br>
/// </summary>
[Serializable]
public class AddressReferencedFamilyDirectorCardCategorySelection : AddressReferencedAsset<FamilyDirectorCardCategorySelection>
{
    /// <summary>
    /// Operator for casting <see cref="AddressReferencedFamilyDirectorCardCategorySelection"/> to a boolean value
    /// <br>Allows you to keep using the unity Syntax for checking if an object exists.</br>
    /// </summary>
    public static implicit operator bool(AddressReferencedFamilyDirectorCardCategorySelection addressReferencedAsset)
    {
        return addressReferencedAsset?.Asset;
    }

    /// <summary>
    /// Operator for casting <see cref="AddressReferencedFamilyDirectorCardCategorySelection"/> to it's currently loaded <see cref="Asset"/> value
    /// </summary>
    public static implicit operator FamilyDirectorCardCategorySelection(AddressReferencedFamilyDirectorCardCategorySelection addressReferencedAsset)
    {
        return addressReferencedAsset?.Asset;
    }

    /// <summary>
    /// Operator for encapsulating a <see cref="string"/> inside an <see cref="AddressReferencedFamilyDirectorCardCategorySelection"/>
    /// </summary>
    public static implicit operator AddressReferencedFamilyDirectorCardCategorySelection(string address)
    {
        return new AddressReferencedFamilyDirectorCardCategorySelection(address);
    }

    /// <summary>
    /// Operator for encapsulating an <see cref="FamilyDirectorCardCategorySelection"/> inside an <see cref="AddressReferencedFamilyDirectorCardCategorySelection"/>
    /// </summary>
    public static implicit operator AddressReferencedFamilyDirectorCardCategorySelection(FamilyDirectorCardCategorySelection asset)
    {
        return new AddressReferencedFamilyDirectorCardCategorySelection(asset);
    }

    /// <summary>
    /// <inheritdoc cref="AddressReferencedAsset{T}.AddressReferencedAsset()"/>
    /// <br>T is <see cref="FamilyDirectorCardCategorySelection"/></br>
    /// </summary>
    public AddressReferencedFamilyDirectorCardCategorySelection() : base() { }

    /// <summary>
    /// <inheritdoc cref="AddressReferencedAsset{T}.AddressReferencedAsset(T)"/>
    /// <br>T is <see cref="FamilyDirectorCardCategorySelection"/></br>
    /// </summary>
    public AddressReferencedFamilyDirectorCardCategorySelection(FamilyDirectorCardCategorySelection gameObject) : base(gameObject) { }

    /// <summary>
    /// <inheritdoc cref="AddressReferencedAsset{T}.AddressReferencedAsset(string)"/>
    /// <br>T is <see cref="FamilyDirectorCardCategorySelection"/></br>
    /// </summary>
    public AddressReferencedFamilyDirectorCardCategorySelection(string address) : base(address) { }
}
