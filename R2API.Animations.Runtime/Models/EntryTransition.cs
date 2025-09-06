using System;
using System.Collections.Generic;
using R2API.Animations.Models.Interfaces;
using UnityEngine;

namespace R2API.Models;

/// <summary>
/// EntryTransitions define when and how the state machine switch from one state to another.
/// EntryTransition always originate from a StateMachine or a StateMachine entry.
/// They do not define timing parameters.
/// </summary>
[Serializable]
public class EntryTransition : ISimpleTransition
{
    [SerializeField]
    private string destinationStateName;
    /// <inheritdoc/>
    public string DestinationStateName { get => destinationStateName; set => destinationStateName = value; }

    [SerializeField]
    private string destinationStateMachineName;
    /// <inheritdoc/>
    public string DestinationStateMachinePath { get => destinationStateMachineName; set => destinationStateMachineName = value; }

    [SerializeField]
    private List<Condition> conditions = [];
    /// <inheritdoc cref="ISimpleTransition.Conditions"/>
    public List<Condition> Conditions { get => conditions; }
    IReadOnlyList<ICondition> ISimpleTransition.Conditions { get => conditions; }
}
