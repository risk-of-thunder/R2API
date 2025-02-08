using System;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace R2API.Models;

/// <summary>
/// Condition that is used to determine if a transition must be taken.
/// </summary>
[Serializable]
public class Condition
{
    [SerializeField]
    private ConditionMode conditionMode;
    /// <summary>
    /// The mode of the condition.
    /// </summary>
    public ConditionMode ConditionMode { get => conditionMode; set => conditionMode = value; }
    [SerializeField]
    private string paramName;
    /// <summary>
    /// The name of the parameter used in the condition.
    /// </summary>
    public string ParamName { get => paramName; set => paramName = value; }
    [SerializeField]
    private float value;
    /// <summary>
    /// The Parameter's threshold value for the condition to be true.
    /// </summary>
    public float Value { get => value; set => this.value = value; }

    /// <summary>
    /// Writing into a binary writer for caching purposes.
    /// </summary>
    /// <param name="writer"></param>
    public void WriteBinary(BinaryWriter writer)
    {
        writer.Write((int)ConditionMode);
        writer.Write(ParamName ?? "");
        writer.Write(Value.ToString(CultureInfo.InvariantCulture));
    }
}
