using System.Collections.Generic;
using R2API.Animations.Models.Interfaces;
using UnityEngine;

namespace R2API.Models;

/// <inheritdoc cref="IBlendTree"/>
public class BlendTree : ScriptableObject, IBlendTree
{
    [SerializeField]
    private string blendParameter;
    /// <inheritdoc/>
    public string BlendParameter { get => blendParameter; set => blendParameter = value; }

    [SerializeField]
    private string blendParameterY;
    /// <inheritdoc/>
    public string BlendParameterY { get => blendParameterY; set => blendParameterY = value; }

    [SerializeField]
    private BlendTreeType blendType;
    /// <inheritdoc/>
    public BlendTreeType BlendType { get => blendType; set => blendType = value; }

    [SerializeField]
    private List<ChildMotion> children = [];
    /// <inheritdoc cref="IBlendTree.Children"/>
    public List<ChildMotion> Children { get => children; }
    IReadOnlyList<IChildMotion> IBlendTree.Children { get => children; }
}
