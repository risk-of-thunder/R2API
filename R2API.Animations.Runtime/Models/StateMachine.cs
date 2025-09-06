using System;
using System.Collections.Generic;
using System.Text;
using R2API.Animations.Models.Interfaces;
using UnityEngine;

namespace R2API.Models;

/// <inheritdoc cref="IStateMachine"/>
public class StateMachine : ScriptableObject, IStateMachine
{
    /// <inheritdoc/>
    public string Name { get => name; set => name = value; }

    [SerializeField]
    private string defaultStateName;
    /// <inheritdoc/>
    public string DefaultStateName { get => defaultStateName; set => defaultStateName = value; }

    [SerializeField]
    private string defaultStateMachinePath;
    /// <inheritdoc/>
    public string DefaultStateMachinePath { get => defaultStateMachinePath; set => defaultStateMachinePath = value; }

    [SerializeField]
    private List<State> states = [];
    /// <inheritdoc cref="IStateMachine.States"/>
    public List<State> States { get => states; }
    IReadOnlyList<IState> IStateMachine.States { get => states; }

    [SerializeField]
    private List<Transition> anyStateTransitions = [];
    /// <inheritdoc cref="IStateMachine.AnyStateTransitions"/>
    public List<Transition> AnyStateTransitions { get => anyStateTransitions; }
    IReadOnlyList<ITransition> IStateMachine.AnyStateTransitions { get => anyStateTransitions; }

    [SerializeField]
    private List<EntryTransition> entryTransitions = [];
    /// <inheritdoc cref="IStateMachine.EntryTransitions"/>
    public List<EntryTransition> EntryTransitions { get => entryTransitions; }
    IReadOnlyList<ISimpleTransition> IStateMachine.EntryTransitions { get => entryTransitions; }

    [SerializeField]
    private List<StateMachine> subStateMachines = [];
    /// <inheritdoc cref="IStateMachine.SubStateMachines"/>
    public List<StateMachine> SubStateMachines { get => subStateMachines; }
    IReadOnlyList<IStateMachine> IStateMachine.SubStateMachines { get => subStateMachines; }

    [SerializeField]
    private List<StateMachineBehaviour> behaviours = [];
    /// <inheritdoc cref="IStateMachine.Behaviours"/>
    public List<StateMachineBehaviour> Behaviours { get => behaviours; }
    IReadOnlyList<StateMachineBehaviour> IStateMachine.Behaviours { get => behaviours; }
}
