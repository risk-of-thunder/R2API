using RoR2;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace R2API.AddressReferencedAssets;

/// <summary>
/// <inheritdoc cref="AddressReferencedAsset{T}"/>
/// <br>T is <see cref="EquipmentDef"/></br>
/// <br>The <see cref="EquipmentDef"/> can also be loaded via the <see cref="EquipmentCatalog"/>, as such, you should wait until <see cref="AddressReferencedAsset.OnAddressReferencedAssetsLoaded"/> gets raised.</br>
/// </summary>
public class AddressReferencedEquipmentDef : AddressReferencedAsset<EquipmentDef>
{
    public override bool CanLoadFromCatalog => true;
    protected override async Task LoadAsync()
    {
        EquipmentIndex index = EquipmentCatalog.FindEquipmentIndex(Address);
        if (index != EquipmentIndex.None)
        {
            Asset = EquipmentCatalog.GetEquipmentDef(index);
            return;
        }
        await LoadFromAddressAsync();
    }

    protected override void Load()
    {
        EquipmentIndex index = EquipmentCatalog.FindEquipmentIndex(Address);
        if (index != EquipmentIndex.None)
        {
            Asset = EquipmentCatalog.GetEquipmentDef(index);
            return;
        }
        LoadFromAddress();
    }
}
