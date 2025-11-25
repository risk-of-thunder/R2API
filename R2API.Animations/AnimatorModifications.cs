using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using R2API.Models;
using R2API.Animations.Models.Interfaces;
using UnityEngine;

namespace R2API;

/// <summary>
/// Modifications for a <see cref="RuntimeAnimatorController"/>
/// </summary>
public class AnimatorModifications : IExistingAnimatorController
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

    /// <inheritdoc cref="IExistingAnimatorController.NewParameters"/>
    public List<Parameter> NewParameters { get; } = [];
    IReadOnlyList<IParameter> IExistingAnimatorController.NewParameters { get => NewParameters; }

    public List<ExistingLayer> Layers { get; } = [];
    /// <inheritdoc cref="IExistingAnimatorController.Layers"/>
    IReadOnlyList<IExistingLayer> IExistingAnimatorController.Layers { get => Layers; }

    public List<Layer> NewLayers { get; } = [];
    /// <inheritdoc cref="IExistingAnimatorController.NewLayers"/>
    IReadOnlyList<ILayer> IExistingAnimatorController.NewLayers { get => NewLayers; }

    /// <summary>
    /// Modifications for a <see cref="RuntimeAnimatorController"/>
    /// </summary>
    /// <param name="plugin">BepInPlugin instance</param>
    public AnimatorModifications(BepInPlugin plugin)
    {
        Key = $"{plugin.GUID};{plugin.Version}";
    }

    /// <summary>
    /// Creates an <see cref="AnimatorModifications"/> from <see cref="AnimatorDiff"/> created in Unity.
    /// <para/>
    /// Use <see cref="CreateFromDiff(AnimatorDiff, BepInPlugin)"/> instead
    /// </summary>
    /// <param name="diff"></param>
    /// <param name="plugin"></param>
    /// <param name="clipBundlePath"></param>
    /// <returns></returns>
    [Obsolete]
    public static AnimatorModifications CreateFromDiff(AnimatorDiff diff, BepInPlugin plugin, string clipBundlePath)
    {
        return CreateFromDiff(diff, plugin);
    }

    /// <summary>
    /// Creates an <see cref="AnimatorModifications"/> from <see cref="AnimatorDiff"/> created in Unity.
    /// </summary>
    /// <param name="diff"></param>
    /// <param name="plugin"></param>
    /// <returns></returns>
    public static AnimatorModifications CreateFromDiff(AnimatorDiff diff, BepInPlugin plugin)
    {
        var modifications = new AnimatorModifications(plugin);
        modifications.Layers.AddRange(diff.Layers);
        modifications.NewLayers.AddRange(diff.NewLayers);
        modifications.NewParameters.AddRange(diff.NewParameters);

        foreach (var layer in diff.Layers)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            modifications.NewStates[layer.Name] = layer.NewStates;
            foreach (var state in layer.ExistingStates)
            {
                modifications.NewTransitions[(layer.Name, state.Name)] = state.NewTransitions;
            }
#pragma warning restore CS0618 // Type or member is obsolete
        }

        return modifications;
    }
}
