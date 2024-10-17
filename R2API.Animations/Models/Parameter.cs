using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace R2API.Models;

public class Parameter
{
    public string Name { get; set; }
    public ParameterType Type { get; set; }
    public ParameterValue Value { get; set; }

    /// <summary>
    /// Writing into a binary writer for caching purposes.
    /// </summary>
    /// <param name="writer"></param>
    public void WriteBinary(BinaryWriter writer)
    {
        writer.Write(Name ?? "");
        writer.Write((int)Type);
        Value.WriteBinary(writer);
    }
}
