using System.Collections.Generic;
using System.IO;
using BepInEx;
using R2API.Models;
using UnityEngine;

namespace R2API;

/// <summary>
/// Modifications for a <see cref="RuntimeAnimatorController"/>
/// </summary>
public class AnimatorModifications
{
    /// <summary>
    /// A key for caching, calculated from plugin info.
    /// </summary>
    public string Key { get; }
    /// <summary>
    /// New states to add. The Key is a layer name to which a state will be added.
    /// </summary>
    public Dictionary<string, State> NewStates { get; } = [];
    /// <summary>
    /// New transitions to add. The key is a (layer name, state name)
    /// </summary>
    public Dictionary<(string, string), Transition> NewTransitions { get; } = [];
    /// <summary>
    /// New parameters to add.
    /// </summary>
    public List<Parameter> NewParameters { get; } = [];

    /// <summary>
    /// Modifications for a <see cref="RuntimeAnimatorController"/>
    /// </summary>
    /// <param name="plugin">BepInPlugin instance</param>
    public AnimatorModifications(BepInPlugin plugin)
    {
        Key = $"{plugin.GUID};{plugin.Version}";
    }

    /// <summary>
    /// Writing modifications into a binary writer for caching purposes.
    /// </summary>
    /// <param name="writer"></param>
    public void WriteBinary(BinaryWriter writer)
    {
        writer.Write(Key);

        foreach (var (layer, state) in NewStates)
        {
            writer.Write(layer);
            state.WriteBinary(writer);
        }

        foreach (var ((layer, state), transition) in NewTransitions)
        {
            writer.Write(layer);
            writer.Write(state);
            transition.WriteBinary(writer);
        }

        foreach (var parameter in NewParameters)
        {
            parameter.WriteBinary(writer);
        }
    }
}
