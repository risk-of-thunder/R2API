using System;
using R2API.Animations.Models.Interfaces;
using UnityEngine;

namespace R2API.Models;

/// <inheritdoc cref="IParameter"/>
[Serializable]
public class Parameter : IParameter
{
    [SerializeField]
    private string name;
    /// <inheritdoc/>
    public string Name { get => name; set => name = value; }

    [SerializeField]
    private ParameterType type;
    /// <inheritdoc/>
    public ParameterType Type { get => type; set => type = value; }

    [SerializeField]
    private ParameterValue value;
    /// <inheritdoc/>
    public ParameterValue Value { get => value; set => this.value = value; }
}
