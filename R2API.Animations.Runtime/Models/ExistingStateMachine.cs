using System;
using System.Collections.Generic;
using System.Text;
using R2API.Animations.Models.Interfaces;
using UnityEngine;

namespace R2API.Models;

/// <inheritdoc cref="IExistingStateMachine"/>
public class ExistingStateMachine : ScriptableObject, IExistingStateMachine
{
    /// <inheritdoc/>
    public string Name { get => name; set => name = value; }

    [SerializeField]
    private List<State> newStates = [];
    /// <inheritdoc cref="IExistingStateMachine.NewStates"/>
    public List<State> NewStates { get => newStates; }
    IReadOnlyList<IState> IExistingStateMachine.NewStates { get => newStates; }

    [SerializeField]
    private List<ExistingState> states = [];
    /// <inheritdoc cref="IExistingStateMachine.States"/>
    public List<ExistingState> States { get => states; }
    IReadOnlyList<IExistingState> IExistingStateMachine.States { get => states; }

    [SerializeField]
    private List<Transition> newAnyStateTransitions = [];
    /// <inheritdoc cref="IExistingStateMachine.NewAnyStateTransitions"/>
    public List<Transition> NewAnyStateTransitions { get => newAnyStateTransitions; }
    IReadOnlyList<ITransition> IExistingStateMachine.NewAnyStateTransitions { get => newAnyStateTransitions; }

    [SerializeField]
    private List<EntryTransition> newEntryTransitions = [];
    /// <inheritdoc cref="IExistingStateMachine.NewEntryTransitions"/>
    public List<EntryTransition> NewEntryTransitions { get => newEntryTransitions; }
    IReadOnlyList<ISimpleTransition> IExistingStateMachine.NewEntryTransitions { get => newEntryTransitions; }

    [SerializeField]
    private List<ExistingStateMachine> subStateMachines = [];
    /// <inheritdoc cref="IExistingStateMachine.SubStateMachines"/>
    public List<ExistingStateMachine> SubStateMachines { get => subStateMachines; }
    IReadOnlyList<IExistingStateMachine> IExistingStateMachine.SubStateMachines { get => subStateMachines; }

    [SerializeField]
    private List<StateMachine> newSubStateMachines = [];
    /// <inheritdoc cref="IExistingStateMachine.NewSubStateMachines"/>
    public List<StateMachine> NewSubStateMachines { get => newSubStateMachines; }
    IReadOnlyList<IStateMachine> IExistingStateMachine.NewSubStateMachines { get => newSubStateMachines; }

    [SerializeField]
    private List<StateMachineBehaviour> newBehaviours = [];
    /// <inheritdoc cref="IExistingStateMachine.NewBehaviours"/>
    public List<StateMachineBehaviour> NewBehaviours { get => newBehaviours; }
    IReadOnlyList<StateMachineBehaviour> IExistingStateMachine.NewBehaviours { get => newBehaviours; }
}
