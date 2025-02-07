using System;
using System.Collections.Generic;
using UnityEngine;

namespace R2API.Models;

/// <summary>
/// Changes to an existing state in a layer.
/// </summary>
[Serializable]
public class ExistingState
{
    [SerializeField]
    private string name;
    /// <summary>
    /// The name of the state.
    /// </summary>
    public string Name { get => name; set => name = value; }

    [SerializeField]
    private List<Transition> newTransitions = [];
    /// <summary>
    /// New transitions that will be added to the state.
    /// </summary>
    public List<Transition> NewTransitions { get => newTransitions; }
}
