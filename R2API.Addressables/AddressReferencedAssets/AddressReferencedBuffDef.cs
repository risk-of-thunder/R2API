using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace R2API.AddressReferencedAssets;

/// <summary>
/// <inheritdoc cref="AddressReferencedAsset{T}"/>
/// <br>T is <see cref="BuffDef"/></br>
/// <br>The <see cref="BuffDef"/> can also be loaded via the <see cref="BuffCatalog"/>, as such, you should wait until <see cref="AddressReferencedAsset.OnAddressReferencedAssetsLoaded"/> gets raised.</br>
/// </summary>
[Serializable]
public class AddressReferencedBuffDef : AddressReferencedAsset<BuffDef>
{
    public override bool CanLoadFromCatalog => true;
    protected override async Task LoadAsync()
    {
        BuffIndex index = BuffCatalog.FindBuffIndex(Address);
        if(index != BuffIndex.None)
        {
            Asset = BuffCatalog.GetBuffDef(index);
            return;
        }
        await LoadFromAddressAsync();
    }

    protected override void Load()
    {
        BuffIndex index = BuffCatalog.FindBuffIndex(Address);
        if (index != BuffIndex.None)
        {
            Asset = BuffCatalog.GetBuffDef(index);
            return;
        }
        LoadFromAddress();
    }
}
