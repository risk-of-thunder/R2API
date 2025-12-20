using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace R2API.AddressReferencedAssets;

/// <summary>
/// An Attribute which marks a <see cref="AddressReferencedAsset{T}"/> that it should not be loaded via the ingame catalogs.
/// <para></para>
/// This attribute does nothing at runtime, it's meant to be a Attribute for usage in an editor environment alongside RoR2EditorKit.
/// </summary>
public class NoCatalogLoadAttribute : Attribute
{

}
