using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace R2API.Models;

public class Condition
{
    public ConditionMode ConditionMode { get; set; }
    public string ParamName { get; set; }
    public float Value { get; set; }

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
