using System;
using System.IO;
using R2API.Animations.Models.Interfaces;
using UnityEngine;

namespace R2API.Models;

/// <inheritdoc cref="IChildMotion"/>
[Serializable]
public class ChildMotion : IChildMotion
{
    /// <summary>
    /// Full path to an AssetBundle that contains AnimationClip for this state.
    /// </summary>
    [Obsolete("No longer required")]
    public string ClipBundlePath { get => ""; set { } }

    [SerializeField]
    private AnimationClip clip;
    /// <inheritdoc/>
    public AnimationClip Clip { get => clip; set => clip = value; }

    [SerializeField]
    private BlendTree blendTree;
    /// <inheritdoc cref="IMotion.BlendTree"/>
    public BlendTree BlendTree { get => blendTree; set => blendTree = value; }
    IBlendTree IMotion.BlendTree { get => blendTree; }

    [SerializeField]
    private float threshold;
    /// <inheritdoc/>
    public float Threshold { get => threshold; set => threshold = value; }

    [SerializeField]
    private Vector2 position;
    /// <inheritdoc/>
    public Vector2 Position { get => position; set => position = value; }

    [SerializeField]
    private float timeScale;
    /// <inheritdoc/>
    public float TimeScale { get => timeScale; set => timeScale = value; }

    [SerializeField]
    private float cycleOffset;
    /// <inheritdoc/>
    public float CycleOffset { get => cycleOffset; set => cycleOffset = value; }

    [SerializeField]
    private string directBlendParameter;
    /// <inheritdoc/>
    public string DirectBlendParameter { get => directBlendParameter; set => directBlendParameter = value; }

    [SerializeField]
    private bool mirror;
    /// <inheritdoc/>
    public bool Mirror { get => mirror; set => mirror = value; }
}
