using System;
using System.Collections.Generic;
using R2API.Animations.Models.Interfaces;
using UnityEngine;

namespace R2API.Models;

/// <inheritdoc cref="IExistingState"/>
[Serializable]
public class ExistingState : IExistingState
{
    [SerializeField]
    private string name;
    /// <inheritdoc/>
    public string Name { get => name; set => name = value; }

    [SerializeField]
    private List<Transition> newTransitions = [];
    /// <inheritdoc cref="IExistingState.NewTransitions"/>
    public List<Transition> NewTransitions { get => newTransitions; }
    IReadOnlyList<ITransition> IExistingState.NewTransitions { get => newTransitions; }

    [SerializeField]
    private List<StateMachineBehaviour> newBehaviours = [];
    /// <inheritdoc cref="IExistingState.NewBehaviours"/>
    public List<StateMachineBehaviour> NewBehaviours { get => newBehaviours; }
    IReadOnlyList<StateMachineBehaviour> IExistingState.NewBehaviours { get => newBehaviours; }
}
