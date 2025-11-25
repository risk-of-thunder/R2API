using System;
using System.Collections.Generic;
using R2API.Animations.Models.Interfaces;
using UnityEngine;

namespace R2API.Models;

/// <inheritdoc cref="IExistingLayer"/>
[Serializable]
public class ExistingLayer : IExistingLayer
{
    [SerializeField]
    private string name;
    /// <inheritdoc/>
    public string Name { get => name; set => name = value; }

    [SerializeField, HideInInspector]
    private List<State> newStates = [];
    /// <summary>
    /// New state that will be added to the layer.
    /// </summary>
    [Obsolete("Use StateMachine field instead")]
    public List<State> NewStates { get => newStates; }

    [SerializeField, HideInInspector]
    private List<ExistingState> existingStates = [];
    /// <summary>
    /// Existing states in the layer that will be modified.
    /// </summary>
    [Obsolete("Use StateMachine field instead")]
    public List<ExistingState> ExistingStates { get => existingStates; }

    [SerializeField]
    private ExistingStateMachine stateMachine;
    /// <inheritdoc cref="IExistingLayer.StateMachine"/>
    public ExistingStateMachine StateMachine { get => stateMachine; set => stateMachine = value; }
    IExistingStateMachine IExistingLayer.StateMachine { get => stateMachine; }

    [SerializeField]
    private List<SyncedBehaviour> newSyncedBehaviours = [];
    /// <inheritdoc cref="IExistingLayer.NewSyncedBehaviours"/>
    public List<SyncedBehaviour> NewSyncedBehaviours { get => newSyncedBehaviours; }
    IReadOnlyList<ISyncedBehaviour> IExistingLayer.NewSyncedBehaviours { get => newSyncedBehaviours; }
}
