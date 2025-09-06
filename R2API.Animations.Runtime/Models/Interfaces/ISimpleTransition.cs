using System;
using System.Collections.Generic;
using System.Text;

namespace R2API.Animations.Models.Interfaces;
/// <summary>
/// Simplest for of a transition
/// </summary>
public interface ISimpleTransition
{
    /// <summary>
    /// The name of a destination state of the transition.
    /// </summary>
    string DestinationStateName { get; }

    /// <summary>
    /// The path to a DestinationState as all parent StateMachines separated by . i.e. "StateMachine1.StateMachine2".
    /// If left empty it's assumed that the source and destination states are in the same state machine.
    /// </summary>
    string DestinationStateMachinePath { get; }

    /// <summary>
    /// Conditions that need to be met for a transition
    /// to happen.
    /// </summary>
    IReadOnlyList<ICondition> Conditions { get; }
}
