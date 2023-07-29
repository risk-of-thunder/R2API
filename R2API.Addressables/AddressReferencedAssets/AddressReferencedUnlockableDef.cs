using RoR2;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace R2API.AddressReferencedAssets;

/// <summary>
/// <inheritdoc cref="AddressReferencedAsset{T}"/>
/// <br>T is <see cref="UnlockableDef"/></br>
/// <br>The <see cref="UnlockableDef"/> can also be loaded via the <see cref="UnlockableCatalog"/>, as such, you should wait until <see cref="AddressReferencedAsset.OnAddressReferencedAssetsLoaded"/> gets raised.</br>
/// </summary>
[Serializable]
public class AddressReferencedUnlockableDef : AddressReferencedAsset<UnlockableDef>
{
    protected override async Task LoadAsync()
    {
        UnlockableDef unlockable = UnlockableCatalog.GetUnlockableDef(Address);
        if (unlockable)
        {
            Asset = unlockable;
            return;
        }
        await LoadFromAddressAsync();
    }

    protected override void Load()
    {
        UnlockableDef unlockable = UnlockableCatalog.GetUnlockableDef(Address);
        if (unlockable)
        {
            Asset = unlockable;
            return;
        }
        LoadFromAddress();
    }
}
