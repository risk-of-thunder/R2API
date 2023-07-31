using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace R2API.AddressReferencedAssets;

/// <summary>
/// <inheritdoc cref="AddressReferencedAsset{T}"/>
/// <br>T is <see cref="EliteDef"/></br>
/// <br>The <see cref="EliteDef"/> can also be loaded via the <see cref="EliteCatalog"/>, as such, you should wait until <see cref="AddressReferencedAsset.OnAddressReferencedAssetsLoaded"/> gets raised.</br>
/// </summary>
[Serializable]
public class AddressReferencedEliteDef : AddressReferencedAsset<EliteDef>
{
    public override bool CanLoadFromCatalog => true;
    protected override async Task LoadAsync()
    {
        EliteDef def = EliteCatalog.eliteDefs.FirstOrDefault(x => x.name.Equals(Address, StringComparison.OrdinalIgnoreCase));
        if (def != null)
        {
            Asset = def;
            return;
        }
        await LoadFromAddressAsync();
    }

    protected override void Load()
    {
        EliteDef def = EliteCatalog.eliteDefs.FirstOrDefault(x => x.name.Equals(Address, StringComparison.OrdinalIgnoreCase));
        if (def != null)
        {
            Asset = def;
            return;
        }
        LoadFromAddress();
    }

    /// <summary>
    /// Operator for casting <see cref="AddressReferencedEliteDef"/> to a boolean value
    /// <br>Allows you to keep using the unity Syntax for checking if an object exists.</br>
    /// </summary>
    public static implicit operator bool(AddressReferencedEliteDef addressReferencedAsset)
    {
        return addressReferencedAsset?.Asset;
    }

    /// <summary>
    /// Operator for casting <see cref="AddressReferencedEliteDef"/> to it's currently loaded <see cref="Asset"/> value
    /// </summary>
    public static implicit operator EliteDef(AddressReferencedEliteDef addressReferencedAsset)
    {
        return addressReferencedAsset?.Asset;
    }

    /// <summary>
    /// Operator for encapsulating a <see cref="string"/> inside an <see cref="AddressReferencedEliteDef"/>
    /// </summary>
    public static implicit operator AddressReferencedEliteDef(string address)
    {
        return new AddressReferencedEliteDef(address);
    }

    /// <summary>
    /// Operator for encapsulating an <see cref="EliteDef"/> inside an <see cref="AddressReferencedEliteDef"/>
    /// </summary>
    public static implicit operator AddressReferencedEliteDef(EliteDef asset)
    {
        return new AddressReferencedEliteDef(asset);
    }

    /// <summary>
    /// <inheritdoc cref="AddressReferencedAsset{T}.AddressReferencedAsset()"/>
    /// <br>T is <see cref="EliteDef"/></br>
    /// </summary>
    public AddressReferencedEliteDef() : base() { }

    /// <summary>
    /// <inheritdoc cref="AddressReferencedAsset{T}.AddressReferencedAsset(T)"/>
    /// <br>T is <see cref="EliteDef"/></br>
    /// </summary>
    public AddressReferencedEliteDef(EliteDef def) : base(def) { }

    /// <summary>
    /// <inheritdoc cref="AddressReferencedAsset{T}.AddressReferencedAsset(string)"/>
    /// <br>T is <see cref="EliteDef"/></br>
    /// </summary>
    public AddressReferencedEliteDef(string addressOrName) : base(addressOrName) { }
}
