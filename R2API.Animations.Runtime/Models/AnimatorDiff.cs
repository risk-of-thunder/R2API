using System.Collections.Generic;
using R2API.Animations.Models.Interfaces;
using UnityEngine;

namespace R2API.Models;

/// <summary>
/// An object containing a difference between 2 RuntimeAnimatorControllers.
/// </summary>
public class AnimatorDiff : ScriptableObject, IExistingAnimatorController
{
    [SerializeField]
    private List<ExistingLayer> layers = [];
    /// <inheritdoc cref="IExistingAnimatorController.Layers"/>
    public List<ExistingLayer> Layers { get => layers; }
    IReadOnlyList<IExistingLayer> IExistingAnimatorController. Layers { get => layers; }

    [SerializeField]
    private List<Layer> newLayers = [];
    /// <inheritdoc cref="IExistingAnimatorController.NewLayers"/>
    public List<Layer> NewLayers { get => newLayers; }
    IReadOnlyList<ILayer> IExistingAnimatorController.NewLayers { get => newLayers; }

    [SerializeField]
    private List<Parameter> newParameters = [];
    /// <inheritdoc cref="IExistingAnimatorController.NewParameters"/>
    public List<Parameter> NewParameters { get => newParameters; }
    IReadOnlyList<IParameter> IExistingAnimatorController. NewParameters { get => newParameters; }
}
