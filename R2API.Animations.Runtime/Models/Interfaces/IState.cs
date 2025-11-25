using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace R2API.Animations.Models.Interfaces;
/// <summary>
/// States are the basic building blocks of a state machine. Each state contains
/// a Motion (AnimationClip or BlendTree) which will play while the character is
/// in that state. When an event in the game triggers a state transition, the character
/// will be left in a new state whose animation sequence will then take over.
/// </summary>
public interface IState : IMotion
{
    /// <summary>
    /// Name of the state.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// A tag can be used to identify a state.
    /// </summary>
    string Tag { get; }

    /// <summary>
    /// The animator controller parameter that drives the speed value. Leave null if you want to use constant value.
    /// </summary>
    string SpeedParam { get; }

    /// <summary>
    /// The animator controller parameter that drives the mirror value. Leave null if you want to use constant value.
    /// </summary>
    string MirrorParam { get; }

    /// <summary>
    /// The animator controller parameter that drives the cycle offset value. Leave null if you want to use constant value.
    /// </summary>
    string CycleOffsetParam { get; }

    /// <summary>
    /// The animator controller parameter that drives the time value. Leave null if you want to use constant value.
    /// </summary>
    string TimeParam { get; }

    /// <summary>
    /// The default speed of the motion.
    /// </summary>
    float Speed { get; }

    /// <summary>
    /// Offset at which the animation loop starts. Useful for synchronizing looped animations.
    /// Units is normalized time.
    /// </summary>
    float CycleOffset { get; }

    /// <summary>
    /// Should Foot IK be respected for this state.
    /// </summary>
    bool IKOnFeet { get; }

    /// <summary>
    /// Whether or not the AnimatorStates writes back the default values for properties
    /// that are not animated by its Motion.
    /// </summary>
    bool WriteDefaultValues { get; }

    /// <summary>
    /// AnimationClip is looped.
    /// </summary>
    bool Loop { get; }

    /// <summary>
    /// Should the state be mirrored.
    /// </summary>
    bool Mirror { get; }

    /// <summary>
    /// The transitions that are going out of the state.
    /// </summary>
    IReadOnlyList<ITransition> Transitions { get; }

    /// <summary>
    /// The Behaviour list assigned to this state.
    /// </summary>
    IReadOnlyList<StateMachineBehaviour> Behaviours { get; }

}
