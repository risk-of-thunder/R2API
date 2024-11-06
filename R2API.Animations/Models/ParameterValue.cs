using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace R2API.Models;

[StructLayout(LayoutKind.Explicit)]
public struct ParameterValue
{
    [field: FieldOffset(0)]
    public bool BoolValue { get; set; }
    [field: FieldOffset(0)]
    public float FloatValue { get; set; }
    [field: FieldOffset(0)]
    public int IntValue { get; set; }

    public ParameterValue(bool value)
    {
        BoolValue = value;
    }

    public ParameterValue(float value)
    {
        FloatValue = value;
    }

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

    /// <summary>
    /// Writing into a binary writer for caching purposes.
    /// </summary>
    /// <param name="writer"></param>
    public readonly void WriteBinary(BinaryWriter writer)
    {
        writer.Write(IntValue);
    }
}
