using System;
using System.Collections.Generic;
using System.Text;
using R2API.Models;
using UnityEngine;

namespace R2API.Animations.Models.Interfaces;
/// <summary>
/// A graph controlling the interaction of states. Each state references a motion.
/// </summary>
public interface IStateMachine
{
    /// <summary>
    /// The name of the state machine.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The name of a state that will play on state machine entry.
    /// </summary>
    public string DefaultStateName { get; }

    /// <summary>
    /// The path to a DefaultState as all parent StateMachines separated by . i.e. "StateMachine1.StateMachine2".
    /// If left empty it's assumed that the default state is in this state machine.
    /// </summary>
    string DefaultStateMachinePath { get; }

    /// <summary>
    /// New state that will be added to the layer.
    /// </summary>
    IReadOnlyList<IState> States { get; }

    /// <summary>
    /// New any state transitions that will be added to the layer.
    /// </summary>
    IReadOnlyList<ITransition> AnyStateTransitions { get ; }

    /// <summary>
    /// New entry transitions that will be added to the layer.
    /// </summary>
    IReadOnlyList<ISimpleTransition> EntryTransitions { get ; }

    /// <summary>
    /// New sub state machines that will be added to the layer.
    /// </summary>
    IReadOnlyList<IStateMachine> SubStateMachines { get; }

    /// <summary>
    /// The Behaviour list assigned to this state machine.
    /// </summary>
    IReadOnlyList<StateMachineBehaviour> Behaviours { get; }
}
