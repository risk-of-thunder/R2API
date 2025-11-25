using System;
using System.Collections.Generic;
using R2API.Animations.Models.Interfaces;
using UnityEngine;

namespace R2API.Models;

/// <inheritdoc cref="IState"/>
[Serializable]
public class State : IState
{
    [SerializeField]
    private string name;
    /// <inheritdoc/>
    public string Name { get => name; set => name = value; }

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
    private string tag;
    /// <inheritdoc/>
    public string Tag { get => tag; set => tag = value; }

    [SerializeField]
    private string speedParam;
    /// <inheritdoc/>
    public string SpeedParam { get => speedParam; set => speedParam = value; }

    [SerializeField]
    private string mirrorParam;
    /// <inheritdoc/>
    public string MirrorParam { get => mirrorParam; set => mirrorParam = value; }

    [SerializeField]
    private string cycleOffsetParam;
    /// <inheritdoc/>
    public string CycleOffsetParam { get => cycleOffsetParam; set => cycleOffsetParam = value; }

    [SerializeField]
    private string timeParam;
    /// <inheritdoc/>
    public string TimeParam { get => timeParam; set => timeParam = value; }

    [SerializeField]
    private float speed;
    /// <inheritdoc/>
    public float Speed { get => speed; set => speed = value; }

    [SerializeField]
    private float cycleOffset;
    /// <inheritdoc/>
    public float CycleOffset { get => cycleOffset; set => cycleOffset = value; }

    [SerializeField]
    private bool ikOnFeet;
    /// <inheritdoc/>
    public bool IKOnFeet { get => ikOnFeet; set => ikOnFeet = value; }

    [SerializeField]
    private bool writeDefaultValues;
    /// <inheritdoc/>
    public bool WriteDefaultValues { get => writeDefaultValues; set => writeDefaultValues = value; }

    [SerializeField]
    private bool loop;
    /// <inheritdoc/>
    public bool Loop { get => loop; set => loop = value; }

    [SerializeField]
    private bool mirror;
    /// <inheritdoc/>
    public bool Mirror { get => mirror; set => mirror = value; }

    [SerializeField]
    private List<Transition> transitions = [];
    /// <inheritdoc cref="IState.Transitions"/>
    public List<Transition> Transitions { get => transitions; }
    IReadOnlyList<ITransition> IState.Transitions { get => transitions; }

    [SerializeField]
    private List<StateMachineBehaviour> behaviours = [];
    /// <inheritdoc cref="IState.Behaviours"/>
    public List<StateMachineBehaviour> Behaviours { get => behaviours; }
    IReadOnlyList<StateMachineBehaviour> IState.Behaviours { get => behaviours; }
}
