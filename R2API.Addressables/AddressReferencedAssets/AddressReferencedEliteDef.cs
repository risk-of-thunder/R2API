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
}
