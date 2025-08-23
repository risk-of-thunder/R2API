using RoR2;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace R2API.AddressReferencedAssets;

/// <summary>
/// <inheritdoc cref="AddressReferencedAsset{T}"/>
/// <br>T is <see cref="EquipmentDef"/></br>
/// <br>The <see cref="EquipmentDef"/> can also be loaded via the <see cref="EquipmentCatalog"/>, as such, you should wait until <see cref="AddressReferencedAsset.OnAddressReferencedAssetsLoaded"/> gets raised.</br>
/// </summary>
[Serializable]
public class AddressReferencedEquipmentDef : AddressReferencedAsset<EquipmentDef>
{
    public override bool CanLoadFromCatalog { get => _canLoadFromCatalog; protected set => _canLoadFromCatalog = value; }

    [SerializeField, HideInInspector]
    private bool _canLoadFromCatalog = true;

    protected override IEnumerator LoadAsyncCoroutine()
    {
        if (CanLoadFromCatalog)
        {
            EquipmentIndex index = EquipmentCatalog.FindEquipmentIndex(Address);
            if (index != EquipmentIndex.None)
            {
                Asset = EquipmentCatalog.GetEquipmentDef(index);
                yield break;
            }
        }
        var subroutine = LoadFromAddressAsyncCoroutine();
        while(subroutine.MoveNext())
        {
            yield return null;
        }
    }

    [Obsolete("Call LoadAsyncCoroutine instead.")]
    protected override async Task LoadAsync()
    {
        if(CanLoadFromCatalog)
        {
            EquipmentIndex index = EquipmentCatalog.FindEquipmentIndex(Address);
            if (index != EquipmentIndex.None)
            {
                Asset = EquipmentCatalog.GetEquipmentDef(index);
                return;
            }
        }
        await LoadFromAddressAsync();
    }

    protected override void Load()
    {
        if(CanLoadFromCatalog)
        {
            EquipmentIndex index = EquipmentCatalog.FindEquipmentIndex(Address);
            if (index != EquipmentIndex.None)
            {
                Asset = EquipmentCatalog.GetEquipmentDef(index);
                return;
            }
        }
        LoadFromAddress();
    }

    /// <summary>
    /// Operator for casting <see cref="AddressReferencedEquipmentDef"/> to a boolean value
    /// <br>Allows you to keep using the unity Syntax for checking if an object exists.</br>
    /// </summary>
    public static implicit operator bool(AddressReferencedEquipmentDef addressReferencedAsset)
    {
        return addressReferencedAsset?.Asset;
    }

    /// <summary>
    /// Operator for casting <see cref="AddressReferencedEquipmentDef"/> to it's currently loaded <see cref="AddressReferencedAsset{T}.Asset"/> value
    /// </summary>
    public static implicit operator EquipmentDef(AddressReferencedEquipmentDef addressReferencedAsset)
    {
        return addressReferencedAsset?.Asset;
    }

    /// <summary>
    /// Operator for encapsulating a <see cref="string"/> inside an <see cref="AddressReferencedEquipmentDef"/>
    /// </summary>
    public static implicit operator AddressReferencedEquipmentDef(string address)
    {
        return new AddressReferencedEquipmentDef(address);
    }

    /// <summary>
    /// Operator for encapsulating an <see cref="EquipmentDef"/> inside an <see cref="AddressReferencedEquipmentDef"/>
    /// </summary>
    public static implicit operator AddressReferencedEquipmentDef(EquipmentDef asset)
    {
        return new AddressReferencedEquipmentDef(asset);
    }

    /// <summary>
    /// <inheritdoc cref="AddressReferencedAsset{T}.AddressReferencedAsset()"/>
    /// <br>T is <see cref="EquipmentDef"/></br>
    /// </summary>
    public AddressReferencedEquipmentDef() : base() { }

    /// <summary>
    /// <inheritdoc cref="AddressReferencedAsset{T}.AddressReferencedAsset(T)"/>
    /// <br>T is <see cref="EquipmentDef"/></br>
    /// </summary>
    public AddressReferencedEquipmentDef(EquipmentDef def) : base(def) { }

    /// <summary>
    /// <inheritdoc cref="AddressReferencedAsset{T}.AddressReferencedAsset(string)"/>
    /// <br>T is <see cref="EquipmentDef"/></br>
    /// </summary>
    public AddressReferencedEquipmentDef(string addressOrName) : base(addressOrName) { }
}
