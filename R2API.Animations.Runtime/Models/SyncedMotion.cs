using System;
using System.Collections.Generic;
using System.Text;
using R2API.Animations.Models.Interfaces;
using UnityEngine;

namespace R2API.Models;

/// <inheritdoc cref="ISyncedMotion"/>
[Serializable]
public class SyncedMotion : ISyncedMotion
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
    private AnimationClip clip;
    /// <inheritdoc/>
    public AnimationClip Clip { get => clip; set => clip = value; }

    [SerializeField]
    private BlendTree blendTree;
    /// <inheritdoc cref="IMotion.BlendTree"/>
    public BlendTree BlendTree { get => blendTree; set => blendTree = value; }
    IBlendTree IMotion.BlendTree { get => blendTree; }
}
