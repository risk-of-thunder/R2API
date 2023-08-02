using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace R2API.AddressReferencedAssets;

/// <summary>
/// <inheritdoc cref="AddressReferencedAsset{T}"/>
/// <br>T is <see cref="ItemDef"/></br>
/// <br>The <see cref="ItemDef"/> can also be loaded via the <see cref="ItemCatalog"/>, as such, you should wait until <see cref="AddressReferencedAsset.OnAddressReferencedAssetsLoaded"/> gets raised.</br>
/// </summary>
[Serializable]
public class AddressReferencedItemDef : AddressReferencedAsset<ItemDef>
{
    public override bool CanLoadFromCatalog => true;
    protected override async Task LoadAsync()
    {
        ItemIndex index = ItemCatalog.FindItemIndex(Address);
        if (index != ItemIndex.None)
        {
            Asset = ItemCatalog.GetItemDef(index);
            return;
        }
        await LoadFromAddressAsync();
    }

    protected override void Load()
    {
        ItemIndex index = ItemCatalog.FindItemIndex(Address);
        if (index != ItemIndex.None)
        {
            Asset = ItemCatalog.GetItemDef(index);
            return;
        }
        LoadFromAddress();
    }
    /// <summary>
    /// Operator for casting <see cref="AddressReferencedItemDef"/> to a boolean value
    /// <br>Allows you to keep using the unity Syntax for checking if an object exists.</br>
    /// </summary>
    public static implicit operator bool(AddressReferencedItemDef addressReferencedAsset)
    {
        return addressReferencedAsset?.Asset;
    }

    /// <summary>
    /// Operator for casting <see cref="AddressReferencedItemDef"/> to it's currently loaded <see cref="Asset"/> value
    /// </summary>
    public static implicit operator ItemDef(AddressReferencedItemDef addressReferencedAsset)
    {
        return addressReferencedAsset?.Asset;
    }

    /// <summary>
    /// Operator for encapsulating a <see cref="string"/> inside an <see cref="AddressReferencedItemDef"/>
    /// </summary>
    public static implicit operator AddressReferencedItemDef(string address)
    {
        return new AddressReferencedItemDef(address);
    }

    /// <summary>
    /// Operator for encapsulating an <see cref="ItemDef"/> inside an <see cref="AddressReferencedItemDef"/>
    /// </summary>
    public static implicit operator AddressReferencedItemDef(ItemDef asset)
    {
        return new AddressReferencedItemDef(asset);
    }

    /// <summary>
    /// <inheritdoc cref="AddressReferencedAsset{T}.AddressReferencedAsset()"/>
    /// <br>T is <see cref="ItemDef"/></br>
    /// </summary>
    public AddressReferencedItemDef() : base() { }

    /// <summary>
    /// <inheritdoc cref="AddressReferencedAsset{T}.AddressReferencedAsset(T)"/>
    /// <br>T is <see cref="ItemDef"/></br>
    /// </summary>
    public AddressReferencedItemDef(ItemDef def) : base(def) { }

    /// <summary>
    /// <inheritdoc cref="AddressReferencedAsset{T}.AddressReferencedAsset(string)"/>
    /// <br>T is <see cref="ItemDef"/></br>
    /// </summary>
    public AddressReferencedItemDef(string addressOrName) : base(addressOrName) { }
}
