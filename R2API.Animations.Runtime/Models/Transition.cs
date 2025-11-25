using System;
using System.Collections.Generic;
using R2API.Animations.Models.Interfaces;
using UnityEngine;

namespace R2API.Models;

/// <inheritdoc cref="ITransition"/>
[Serializable]
public class Transition : ITransition
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
    private float transitionDuration;
    /// <inheritdoc/>
    public float TransitionDuration { get => transitionDuration; set => transitionDuration = value; }

    [SerializeField]
    private float offset;
    /// <inheritdoc/>
    public float Offset { get => offset; set => offset = value; }

    [SerializeField]
    private float exitTime;
    /// <inheritdoc/>
    public float ExitTime { get => exitTime; set => exitTime = value; }

    [SerializeField]
    private bool hasExitTime;
    /// <inheritdoc/>
    public bool HasExitTime { get => hasExitTime; set => hasExitTime = value; }

    [SerializeField]
    private bool hasFixedDuration;
    /// <inheritdoc/>
    public bool HasFixedDuration { get => hasFixedDuration; set => hasFixedDuration = value; }

    [SerializeField]
    private InterruptionSource interruptionSource;
    /// <inheritdoc/>
    public InterruptionSource InterruptionSource { get => interruptionSource; set => interruptionSource = value; }

    [SerializeField]
    private bool orderedInterruption;
    /// <inheritdoc/>
    public bool OrderedInterruption { get => orderedInterruption; set => orderedInterruption = value; }

    [SerializeField]
    private bool canTransitionToSelf;
    /// <inheritdoc/>
    public bool CanTransitionToSelf { get => canTransitionToSelf; set => canTransitionToSelf = value; }

    [SerializeField]
    private bool isExit;
    /// <inheritdoc/>
    public bool IsExit { get => isExit; set => isExit = value; }

    [SerializeField]
    private List<Condition> conditions = [];
    /// <inheritdoc cref="ISimpleTransition.Conditions"/>
    public List<Condition> Conditions { get => conditions; }
    IReadOnlyList<ICondition> ISimpleTransition.Conditions { get => conditions; }
}
