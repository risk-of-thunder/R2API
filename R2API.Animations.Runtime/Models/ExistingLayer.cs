using System;
using System.Collections.Generic;
using UnityEngine;

namespace R2API.Models;

/// <summary>
/// Changes to an existing layer in a RuntimeAnimationController
/// </summary>
[Serializable]
public class ExistingLayer
{
    [SerializeField]
    private string name;
    /// <summary>
    /// The name of the layer.
    /// </summary>
    public string Name { get => name; set => name = value; }

    [SerializeField]
    private List<State> newStates = [];
    /// <summary>
    /// New state that will be added to the layer.
    /// </summary>
    public List<State> NewStates { get => newStates; }

    [SerializeField]
    private List<ExistingState> existingStates = [];
    /// <summary>
    /// Existing states in the layer that will be modified.
    /// </summary>
    public List<ExistingState> ExistingStates { get => existingStates; }
}
