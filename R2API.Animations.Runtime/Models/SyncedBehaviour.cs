using System;
using System.Collections.Generic;
using System.Text;
using R2API.Animations.Models.Interfaces;
using UnityEngine;

namespace R2API.Models;

/// <inheritdoc cref="ISyncedBehaviour"/>
[Serializable]
public class SyncedBehaviour : ISyncedBehaviour
{
    [SerializeField]
    private string stateName;
    /// <inheritdoc/>
    public string StateName { get => stateName; set => stateName = value; }

    [SerializeField]
    private string stateMachinePath;
    /// <inheritdoc/>
    public string StateMachinePath { get => stateMachinePath; set => stateMachinePath = value; }

    [SerializeField]
    private List<StateMachineBehaviour> behaviours = [];
    /// <inheritdoc cref="ISyncedBehaviour.Behaviours"/>
    public List<StateMachineBehaviour> Behaviours { get => behaviours; }
    IReadOnlyList<StateMachineBehaviour> ISyncedBehaviour.Behaviours { get => behaviours; }
}
