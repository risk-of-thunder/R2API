using System;
using System.IO;
using UnityEngine;

namespace R2API.Models;

/// <summary>
/// Used to communicate between scripting and the controller. Some parameters can be set in scripting and used by the controller,
/// while other parameters are based on Custom Curves in Animation Clips and can be sampled using the scripting API.
/// </summary>
[Serializable]
public class Parameter
{
    [SerializeField]
    private string name;
    /// <summary>
    /// The name of the parameter.
    /// </summary>
    public string Name { get => name; set => name = value; }

    [SerializeField]
    private ParameterType type;
    /// <summary>
    /// The type of the parameter. 
    /// </summary>
    public ParameterType Type { get => type; set => type = value; }

    [SerializeField]
    private ParameterValue value;
    /// <summary>
    /// The default value for the parameter.
    /// </summary>
    public ParameterValue Value { get => value; set => this.value = value; }

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
