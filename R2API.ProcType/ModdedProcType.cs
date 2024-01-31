using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace R2API;

/// <summary>
/// A modded equivalent of <see cref="ProcType"/> for use with <see cref="ProcTypeAPI"/>.
/// </summary>
public enum ModdedProcType : int
{
    /// <summary>
    /// Represents an invalid value of <see cref="ModdedProcType"/>.
    /// </summary>
    /// <remarks>All negative values of <see cref="ModdedProcType"/> are considered invalid.</remarks>
    Invalid = -1
}
