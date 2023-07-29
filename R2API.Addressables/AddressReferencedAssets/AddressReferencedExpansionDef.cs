using RoR2.ExpansionManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace R2API.AddressReferencedAssets;

/// <summary>
/// <inheritdoc cref="AddressReferencedAsset{T}"/>
/// <br>T is <see cref="ExpansionDef"/></br>
/// <br>The <see cref="ExpansionDef"/> can also be loaded via the <see cref="ExpansionCatalog"/>, as such, you should wait until <see cref="AddressReferencedAsset.OnAddressReferencedAssetsLoaded"/> gets raised.</br>
/// </summary>
[Serializable]
public class AddressReferencedExpansionDef : AddressReferencedAsset<ExpansionDef>
{
    public override bool CanLoadFromCatalog => true;

    protected override async Task LoadAsync()
    {
        ExpansionDef expansionDef = ExpansionCatalog.expansionDefs.FirstOrDefault(ed => ed.name.Equals(Address, StringComparison.OrdinalIgnoreCase));
        if (expansionDef != null)
        {
            Asset = expansionDef;
        }
        await LoadFromAddressAsync();
    }

    protected override void Load()
    {
        ExpansionDef expansionDef = ExpansionCatalog.expansionDefs.FirstOrDefault(ed => ed.name.Equals(Address, StringComparison.OrdinalIgnoreCase));
        if (expansionDef != null)
        {
            Asset = expansionDef;
        }
        LoadFromAddress();
    }
}
