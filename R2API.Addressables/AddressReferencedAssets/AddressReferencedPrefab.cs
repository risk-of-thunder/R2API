using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace R2API.AddressReferencedAssets;

/// <summary>
/// <inheritdoc cref="AddressReferencedAsset{T}"/>
/// <br>T is <see cref="GameObject"/></br>
/// </summary>
[Serializable]
public class AddressReferencedPrefab : AddressReferencedAsset<GameObject>
{
}
