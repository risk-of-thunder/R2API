using RoR2;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace R2API.AddressReferencedAssets;

/// <summary>
/// <inheritdoc cref="AddressReferencedAsset{T}"/>
/// <br>T is <see cref="BuffDef"/></br>
/// <br>The <see cref="BuffDef"/> can also be loaded via the <see cref="BuffCatalog"/>, as such, you should wait until <see cref="AddressReferencedAsset.OnAddressReferencedAssetsLoaded"/> gets raised.</br>
/// </summary>
[Serializable]
public class AddressReferencedBuffDef : AddressReferencedAsset<BuffDef>
{
    public override bool CanLoadFromCatalog { get => _canLoadFromCatalog; protected set => _canLoadFromCatalog = value; }

    [SerializeField, HideInInspector]
    private bool _canLoadFromCatalog = true;

    protected override IEnumerator LoadAsyncCoroutine()
    {
        if(CanLoadFromCatalog)
        {
            BuffIndex index = BuffCatalog.FindBuffIndex(Address);
            if(index != BuffIndex.None)
            {
                Asset = BuffCatalog.GetBuffDef(index);
                yield break;
            }
        }
        var subroutine = LoadFromAddressAsyncCoroutine();
        while(subroutine.MoveNext())
        {
            yield return null;
        }
    }

    [Obsolete("Call LoadAsyncCoroutine instead")]
    protected override async Task LoadAsync()
    {
        if(CanLoadFromCatalog)
        {
            BuffIndex index = BuffCatalog.FindBuffIndex(Address);
            if (index != BuffIndex.None)
            {
                Asset = BuffCatalog.GetBuffDef(index);
                return;
            }
        }
        await LoadFromAddressAsync();
    }

    protected override void Load()
    {
        if(CanLoadFromCatalog)
        {
            BuffIndex index = BuffCatalog.FindBuffIndex(Address);
            if (index != BuffIndex.None)
            {
                Asset = BuffCatalog.GetBuffDef(index);
                return;
            }
        }
        LoadFromAddress();
    }

    /// <summary>
    /// Operator for casting <see cref="AddressReferencedBuffDef"/> to a boolean value
    /// <br>Allows you to keep using the unity Syntax for checking if an object exists.</br>
    /// </summary>
    public static implicit operator bool(AddressReferencedBuffDef addressReferencedAsset)
    {
        return addressReferencedAsset?.Asset;
    }

    /// <summary>
    /// Operator for casting <see cref="AddressReferencedBuffDef"/> to it's currently loaded <see cref="AddressReferencedAsset{T}.Asset"/> value
    /// </summary>
    public static implicit operator BuffDef(AddressReferencedBuffDef addressReferencedAsset)
    {
        return addressReferencedAsset?.Asset;
    }

    /// <summary>
    /// Operator for encapsulating a <see cref="string"/> inside an <see cref="AddressReferencedBuffDef"/>
    /// </summary>
    public static implicit operator AddressReferencedBuffDef(string address)
    {
        return new AddressReferencedBuffDef(address);
    }

    /// <summary>
    /// Operator for encapsulating an <see cref="BuffDef"/> inside an <see cref="AddressReferencedBuffDef"/>
    /// </summary>
    public static implicit operator AddressReferencedBuffDef(BuffDef asset)
    {
        return new AddressReferencedBuffDef(asset);
    }

    /// <summary>
    /// <inheritdoc cref="AddressReferencedAsset{T}.AddressReferencedAsset()"/>
    /// <br>T is <see cref="BuffDef"/></br>
    /// </summary>
    public AddressReferencedBuffDef() : base() { }

    /// <summary>
    /// <inheritdoc cref="AddressReferencedAsset{T}.AddressReferencedAsset(T)"/>
    /// <br>T is <see cref="BuffDef"/></br>
    /// </summary>
    public AddressReferencedBuffDef(BuffDef def) : base(def) { }

    /// <summary>
    /// <inheritdoc cref="AddressReferencedAsset{T}.AddressReferencedAsset(string)"/>
    /// <br>T is <see cref="BuffDef"/></br>
    /// </summary>
    public AddressReferencedBuffDef(string addressOrName) : base(addressOrName) { }
}
