using System;
using UnityEngine;

namespace R2API.Models;

/// <summary>
/// Value of a parameter
/// </summary>
[Serializable]
public struct ParameterValue
{
    [SerializeField]
    private bool boolValue;
    /// <summary>
    /// bool value.
    /// </summary>
    public bool BoolValue { get => boolValue; set => boolValue = value; }

    [SerializeField]
    private float floatValue;
    /// <summary>
    /// float value.
    /// </summary>
    public float FloatValue { get => floatValue; set => floatValue = value; }

    [SerializeField]
    private int intValue;
    /// <summary>
    /// int value.
    /// </summary>
    public int IntValue { get => intValue; set => intValue = value; }

    /// <summary>
    /// Initialize with bool value
    /// </summary>
    /// <param name="value"></param>
    public ParameterValue(bool value)
    {
        BoolValue = value;
    }

    /// <summary>
    /// Initialize with float value
    /// </summary>
    /// <param name="value"></param>
    public ParameterValue(float value)
    {
        FloatValue = value;
    }

    /// <summary>
    /// Initialize with int value
    /// </summary>
    /// <param name="value"></param>
    public ParameterValue(int value)
    {
        IntValue = value;
    }

    public static implicit operator ParameterValue(bool value) => new(value);
    public static implicit operator ParameterValue(float value) => new(value);
    public static implicit operator ParameterValue(int value) => new(value);

    public static implicit operator bool(ParameterValue value) => value.BoolValue;
    public static implicit operator float(ParameterValue value) => value.FloatValue;
    public static implicit operator int(ParameterValue value) => value.IntValue;
}
