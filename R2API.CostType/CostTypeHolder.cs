using System;
using RoR2;

namespace R2API;

/// <summary>
/// A holder for reserving a CostTypeIndex before the catalog has initialized.
/// </summary>
public class CostTypeHolder {
    /// <summary>A callback that will be ran once the CostTypeDef gets registered.</summary>
    public Action<CostTypeIndex> OnReserved;
    /// <summary>The CostTypeDef that will be registered</summary>
    public CostTypeDef CostTypeDef;
    /// <summary>The reserved CostTypeIndex. This will be -1 if it has not yet been reserved.</summary>
    public CostTypeIndex CostTypeIndex { 
        get {
            if (!Available) {
                CostTypePlugin.Logger.LogError("Attempted to access to CostTypeIndex of a CostTypeHolder that was reserved before catalog initialization!");
            }

            return _costTypeIndex;
        }
    }
    /// <summary>Whether or not this CostTypeHolder has registered its CostTypeDef yet.</summary>
    public bool Available => _costTypeIndex != (CostTypeIndex)(-1);
    internal CostTypeIndex _costTypeIndex = (CostTypeIndex)(-1);

    /// <summary>
    /// Get the CostTypeIndex stored in this CostTypeHolder
    /// </summary>
    /// <returns>The CostTypeIndex, or -1 if CostTypeHolder.Available is false</returns>
    public static implicit operator CostTypeIndex(CostTypeHolder self) {
        return self.CostTypeIndex;
    }
}