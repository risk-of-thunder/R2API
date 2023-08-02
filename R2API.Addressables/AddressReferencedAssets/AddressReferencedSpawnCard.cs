using RoR2;
using System;
using System.Collections.Generic;
using System.Text;

namespace R2API.AddressReferencedAssets;

/// <summary>
/// <inheritdoc cref="AddressReferencedAsset{T}"/>
/// <br>T is <see cref="SpawnCard"/></br>
/// </summary>
[Serializable]
public class AddressReferencedSpawnCard : AddressReferencedAsset<SpawnCard>
{
    /// <summary>
    /// Operator for casting <see cref="AddressReferencedSpawnCard"/> to a boolean value
    /// <br>Allows you to keep using the unity Syntax for checking if an object exists.</br>
    /// </summary>
    public static implicit operator bool(AddressReferencedSpawnCard addressReferencedAsset)
    {
        return addressReferencedAsset?.Asset;
    }

    /// <summary>
    /// Operator for casting <see cref="AddressReferencedSpawnCard"/> to it's currently loaded <see cref="Asset"/> value
    /// </summary>
    public static implicit operator SpawnCard(AddressReferencedSpawnCard addressReferencedAsset)
    {
        return addressReferencedAsset?.Asset;
    }

    /// <summary>
    /// Operator for encapsulating a <see cref="string"/> inside an <see cref="AddressReferencedSpawnCard"/>
    /// </summary>
    public static implicit operator AddressReferencedSpawnCard(string address)
    {
        return new AddressReferencedSpawnCard(address);
    }

    /// <summary>
    /// Operator for encapsulating an <see cref="SpawnCard"/> inside an <see cref="AddressReferencedSpawnCard"/>
    /// </summary>
    public static implicit operator AddressReferencedSpawnCard(SpawnCard asset)
    {
        return new AddressReferencedSpawnCard(asset);
    }

    /// <summary>
    /// <inheritdoc cref="AddressReferencedAsset{T}.AddressReferencedAsset()"/>
    /// <br>T is <see cref="SpawnCard"/></br>
    /// </summary>
    public AddressReferencedSpawnCard() : base() { }

    /// <summary>
    /// <inheritdoc cref="AddressReferencedAsset{T}.AddressReferencedAsset(T)"/>
    /// <br>T is <see cref="SpawnCard"/></br>
    /// </summary>
    public AddressReferencedSpawnCard(SpawnCard spawnCard) : base(spawnCard) { }

    /// <summary>
    /// <inheritdoc cref="AddressReferencedAsset{T}.AddressReferencedAsset(string)"/>
    /// <br>T is <see cref="SpawnCard"/></br>
    /// </summary>
    public AddressReferencedSpawnCard(string address) : base(address) { }
}
