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
/// <br>T is <see cref="UnlockableDef"/></br>
/// <br>The <see cref="UnlockableDef"/> can also be loaded via the <see cref="UnlockableCatalog"/>, as such, you should wait until <see cref="AddressReferencedAsset.OnAddressReferencedAssetsLoaded"/> gets raised.</br>
/// </summary>
[Serializable]
public class AddressReferencedUnlockableDef : AddressReferencedAsset<UnlockableDef>
{
    public override bool CanLoadFromCatalog => _canLoadFromCatalog;

    [SerializeField, HideInInspector]
    private bool _canLoadFromCatalog = true;

    protected override IEnumerator LoadAsyncCoroutine()
    {
        if (CanLoadFromCatalog)
        {
            UnlockableDef unlockable = UnlockableCatalog.GetUnlockableDef(Address);
            if (unlockable)
            {
                Asset = unlockable;
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
            UnlockableDef unlockable = UnlockableCatalog.GetUnlockableDef(Address);
            if (unlockable)
            {
                Asset = unlockable;
                return;
            }
        }
        await LoadFromAddressAsync();
    }

    protected override void Load()
    {
        if (CanLoadFromCatalog)
        {
            UnlockableDef unlockable = UnlockableCatalog.GetUnlockableDef(Address);
            if (unlockable)
            {
                Asset = unlockable;
                return;
            }
        }
        LoadFromAddress();
    }

    /// <summary>
    /// Operator for casting <see cref="AddressReferencedUnlockableDef"/> to a boolean value
    /// <br>Allows you to keep using the unity Syntax for checking if an object exists.</br>
    /// </summary>
    public static implicit operator bool(AddressReferencedUnlockableDef addressReferencedAsset)
    {
        return addressReferencedAsset?.Asset;
    }

    /// <summary>
    /// Operator for casting <see cref="AddressReferencedUnlockableDef"/> to it's currently loaded <see cref="AddressReferencedAsset{T}.Asset"/> value
    /// </summary>
    public static implicit operator UnlockableDef(AddressReferencedUnlockableDef addressReferencedAsset)
    {
        return addressReferencedAsset?.Asset;
    }

    /// <summary>
    /// Operator for encapsulating a <see cref="string"/> inside an <see cref="AddressReferencedUnlockableDef"/>
    /// </summary>
    public static implicit operator AddressReferencedUnlockableDef(string address)
    {
        return new AddressReferencedUnlockableDef(address);
    }

    /// <summary>
    /// Operator for encapsulating an <see cref="UnlockableDef"/> inside an <see cref="AddressReferencedUnlockableDef"/>
    /// </summary>
    public static implicit operator AddressReferencedUnlockableDef(UnlockableDef asset)
    {
        return new AddressReferencedUnlockableDef(asset);
    }

    /// <summary>
    /// <inheritdoc cref="AddressReferencedAsset{T}.AddressReferencedAsset()"/>
    /// <br>T is <see cref="UnlockableDef"/></br>
    /// </summary>
    public AddressReferencedUnlockableDef() : base() { }

    /// <summary>
    /// <inheritdoc cref="AddressReferencedAsset{T}.AddressReferencedAsset(T)"/>
    /// <br>T is <see cref="UnlockableDef"/></br>
    /// </summary>
    public AddressReferencedUnlockableDef(UnlockableDef def) : base(def) { }

    /// <summary>
    /// <inheritdoc cref="AddressReferencedAsset{T}.AddressReferencedAsset(string)"/>
    /// <br>T is <see cref="UnlockableDef"/></br>
    /// </summary>
    public AddressReferencedUnlockableDef(string addressOrName) : base(addressOrName) { }
}
