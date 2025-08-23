using RoR2.ExpansionManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace R2API.AddressReferencedAssets;

/// <summary>
/// <inheritdoc cref="AddressReferencedAsset{T}"/>
/// <br>T is <see cref="ExpansionDef"/></br>
/// <br>The <see cref="ExpansionDef"/> can also be loaded via the <see cref="ExpansionCatalog"/>, as such, you should wait until <see cref="AddressReferencedAsset.OnAddressReferencedAssetsLoaded"/> gets raised.</br>
/// </summary>
[Serializable]
public class AddressReferencedExpansionDef : AddressReferencedAsset<ExpansionDef>
{
    public override bool CanLoadFromCatalog => _canLoadFromCatalog;

    [SerializeField, HideInInspector]
    private bool _canLoadFromCatalog = true;

    protected override IEnumerator LoadAsyncCoroutine()
    {
        if(CanLoadFromCatalog)
        {
            ExpansionDef expansionDef = ExpansionCatalog.expansionDefs.FirstOrDefault(ed => ed.name.Equals(Address, StringComparison.OrdinalIgnoreCase));
            if (expansionDef != null)
            {
                Asset = expansionDef;
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
            ExpansionDef expansionDef = ExpansionCatalog.expansionDefs.FirstOrDefault(ed => ed.name.Equals(Address, StringComparison.OrdinalIgnoreCase));
            if (expansionDef != null)
            {
                Asset = expansionDef;
                return;
            }
        }
        await LoadFromAddressAsync();
    }

    protected override void Load()
    {
        if(CanLoadFromCatalog)
        {
            ExpansionDef expansionDef = ExpansionCatalog.expansionDefs.FirstOrDefault(ed => ed.name.Equals(Address, StringComparison.OrdinalIgnoreCase));
            if (expansionDef != null)
            {
                Asset = expansionDef;
                return;
            }
        }
        LoadFromAddress();
    }
    /// <summary>
    /// Operator for casting <see cref="AddressReferencedExpansionDef"/> to a boolean value
    /// <br>Allows you to keep using the unity Syntax for checking if an object exists.</br>
    /// </summary>
    public static implicit operator bool(AddressReferencedExpansionDef addressReferencedAsset)
    {
        return addressReferencedAsset?.Asset;
    }

    /// <summary>
    /// Operator for casting <see cref="AddressReferencedExpansionDef"/> to it's currently loaded <see cref="AddressReferencedAsset{T}.Asset"/> value
    /// </summary>
    public static implicit operator ExpansionDef(AddressReferencedExpansionDef addressReferencedAsset)
    {
        return addressReferencedAsset?.Asset;
    }

    /// <summary>
    /// Operator for encapsulating a <see cref="string"/> inside an <see cref="AddressReferencedExpansionDef"/>
    /// </summary>
    public static implicit operator AddressReferencedExpansionDef(string address)
    {
        return new AddressReferencedExpansionDef(address);
    }

    /// <summary>
    /// Operator for encapsulating an <see cref="ExpansionDef"/> inside an <see cref="AddressReferencedExpansionDef"/>
    /// </summary>
    public static implicit operator AddressReferencedExpansionDef(ExpansionDef asset)
    {
        return new AddressReferencedExpansionDef(asset);
    }

    /// <summary>
    /// <inheritdoc cref="AddressReferencedAsset{T}.AddressReferencedAsset()"/>
    /// <br>T is <see cref="ExpansionDef"/></br>
    /// </summary>
    public AddressReferencedExpansionDef() : base() { }

    /// <summary>
    /// <inheritdoc cref="AddressReferencedAsset{T}.AddressReferencedAsset(T)"/>
    /// <br>T is <see cref="ExpansionDef"/></br>
    /// </summary>
    public AddressReferencedExpansionDef(ExpansionDef def) : base(def) { }

    /// <summary>
    /// <inheritdoc cref="AddressReferencedAsset{T}.AddressReferencedAsset(string)"/>
    /// <br>T is <see cref="ExpansionDef"/></br>
    /// </summary>
    public AddressReferencedExpansionDef(string addressOrName) : base(addressOrName) { }
}
