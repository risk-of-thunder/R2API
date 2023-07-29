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
}
