using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    public Dictionary<string, List<State>> NewStates { get; } = [];
    /// <summary>
    /// New transitions to add. The key is a (layer name, state name)
    /// </summary>
    public Dictionary<(string, string), List<Transition>> NewTransitions { get; } = [];
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

        foreach (var (layer, states) in NewStates)
        {
            writer.Write(layer);
            foreach (var state in states)
            {
                state.WriteBinary(writer);
            }
        }

        foreach (var ((layer, state), transitions) in NewTransitions)
        {
            writer.Write(layer);
            writer.Write(state);
            foreach (var transition in transitions)
            {
                transition.WriteBinary(writer);
            }
        }

        foreach (var parameter in NewParameters)
        {
            parameter.WriteBinary(writer);
        }
    }

    /// <summary>
    /// Creates an <see cref="AnimatorModifications"/> from <see cref="AnimatorDiff"/> created in Unity.
    /// </summary>
    /// <param name="diff"></param>
    /// <param name="plugin"></param>
    /// <param name="clipBundlePath"></param>
    /// <returns></returns>
    public static AnimatorModifications CreateFromDiff(AnimatorDiff diff, BepInPlugin plugin, string clipBundlePath)
    {
        var modifications = new AnimatorModifications(plugin);

        foreach (var layer in diff.Layers)
        {
            modifications.NewStates[layer.Name] = layer.NewStates;

            foreach (var state in layer.NewStates)
            {
                if (state.Clip)
                {
                    state.ClipBundlePath = clipBundlePath;
                }
                else if (state.BlendTree)
                {
                    FillBlendTreeClipBundlePath(state.BlendTree, clipBundlePath);
                }
            }

            foreach (var state in layer.ExistingStates)
            {
                modifications.NewTransitions[(layer.Name, state.Name)] = state.NewTransitions;
            }
        }

        modifications.NewParameters.AddRange(diff.NewParameters);

        return modifications;
    }

    private static void FillBlendTreeClipBundlePath(BlendTree blendTree, string clipBundlePath)
    {
        foreach (var child in blendTree.Children)
        {
            if (child.Clip)
            {
                child.ClipBundlePath = clipBundlePath;
            }
            else if (child.BlendTree)
            {
                FillBlendTreeClipBundlePath(child.BlendTree, clipBundlePath);
            }
        }
    }
}
