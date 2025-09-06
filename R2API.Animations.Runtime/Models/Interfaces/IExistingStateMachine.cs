using System;
using System.Collections.Generic;
using System.Text;
using R2API.Models;
using UnityEngine;

namespace R2API.Animations.Models.Interfaces;
/// <summary>
/// Changes to a state machine.
/// </summary>
public interface IExistingStateMachine
{
    /// <summary>
    /// The name of the state machine.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// New state that will be added to the layer.
    /// </summary>
    IReadOnlyList<IState> NewStates { get; }

    /// <summary>
    /// Existing states in the layer that will be modified.
    /// </summary>
    IReadOnlyList<IExistingState> States { get ; }

    /// <summary>
    /// New any state transitions that will be added to the layer.
    /// </summary>
    IReadOnlyList<ITransition> NewAnyStateTransitions { get ; }

    /// <summary>
    /// New entry transitions that will be added to the layer.
    /// </summary>
    IReadOnlyList<ISimpleTransition> NewEntryTransitions { get ; }

    /// <summary>
    /// New sub state machines that will be added to the layer.
    /// </summary>
    IReadOnlyList<IStateMachine> NewSubStateMachines { get; }

    /// <summary>
    /// Existing sub state machines that will be modified.
    /// </summary>
    IReadOnlyList<IExistingStateMachine> SubStateMachines { get; }

    /// <summary>
    /// The Behaviour list assigned to this state machine.
    /// </summary>
    IReadOnlyList<StateMachineBehaviour> NewBehaviours { get; }
}
