using System;
using R2API.Animations.Models.Interfaces;
using UnityEngine;

namespace R2API.Models;

/// <inheritdoc cref="ICondition"/>
[Serializable]
public class Condition : ICondition
{
    [SerializeField]
    private ConditionMode conditionMode;
    /// <inheritdoc/>
    public ConditionMode ConditionMode { get => conditionMode; set => conditionMode = value; }
    [SerializeField]
    private string paramName;
    /// <inheritdoc/>
    public string ParamName { get => paramName; set => paramName = value; }
    [SerializeField]
    private float value;
    /// <inheritdoc/>
    public float Value { get => value; set => this.value = value; }
}
