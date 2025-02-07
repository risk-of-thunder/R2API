using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace R2API.Models;

/// <summary>
/// Transitions define when and how the state machine switch from one state to another.
/// Transition always originate from an Animator State (or AnyState)
/// and have timing parameters.
/// </summary>
[Serializable]
public class Transition
{
    [SerializeField]
    private string destinationStateName;
    /// <summary>
    /// The name of a destination state of the transition.
    /// </summary>
    public string DestinationStateName { get => destinationStateName; set => destinationStateName = value; }

    [SerializeField]
    private float transitionDuration;
    /// <summary>
    /// The duration of the transition.
    /// </summary>
    public float TransitionDuration { get => transitionDuration; set => transitionDuration = value; }

    [SerializeField]
    private float offset;
    /// <summary>
    /// The time at which the destination state will start.
    /// </summary>
    public float Offset { get => offset; set => offset = value; }

    [SerializeField]
    private float exitTime;
    /// <summary>
    /// If Transition.HasExitTime is true, ExitTime represents the exact
    /// time at which the transition can take effect. This is represented in normalized
    /// time, so for example an exit time of 0.75 means that on the first frame where
    /// 75% of the animation has played, the Exit Time condition will be true. On the
    /// next frame, the condition will be false. For looped animations, transitions with
    /// exit times smaller than 1 will be evaluated every loop, so you can use this to
    /// time your transition with the proper timing in the animation, every loop. Transitions
    /// with exit times greater than one will be evaluated only once, so they can be
    /// used to exit at a specific time, after a fixed number of loops. For example,
    /// a transition with an exit time of 3.5 will be evaluated once, after three and
    /// a half loops.
    /// </summary>
    public float ExitTime { get => exitTime; set => exitTime = value; }

    [SerializeField]
    private bool hasExitTime;
    /// <summary>
    /// When active the transition will have an exit time condition.
    /// </summary>
    public bool HasExitTime { get => hasExitTime; set => hasExitTime = value; }

    [SerializeField]
    private bool hasFixedDuration;
    /// <summary>
    /// Determines whether the duration of the transition is reported in a fixed duration
    /// in seconds or as a normalized time.
    /// </summary>
    public bool HasFixedDuration { get => hasFixedDuration; set => hasFixedDuration = value; }

    [SerializeField]
    private InterruptionSource interruptionSource;
    /// <summary>
    /// Which AnimatorState transitions can interrupt the Transition.
    /// </summary>
    public InterruptionSource InterruptionSource { get => interruptionSource; set => interruptionSource = value; }

    [SerializeField]
    private bool orderedInterruption;
    /// <summary>
    /// The Transition can be interrupted by a transition that has a higher priority.
    /// </summary>
    public bool OrderedInterruption { get => orderedInterruption; set => orderedInterruption = value; }

    [SerializeField]
    private bool canTransitionToSelf;
    /// <summary>
    /// Set to true to allow or disallow transition to self during AnyState transition.
    /// </summary>
    public bool CanTransitionToSelf { get => canTransitionToSelf; set => canTransitionToSelf = value; }

    [SerializeField]
    private List<Condition> conditions = [];
    /// <summary>
    /// Animations.AnimatorCondition conditions that need to be met for a transition
    /// to happen.
    /// </summary>
    public List<Condition> Conditions { get => conditions; }

    /// <summary>
    /// Writing modifications into a binary writer for caching purposes.
    /// </summary>
    /// <param name="writer"></param>
    public void WriteBinary(BinaryWriter writer)
    {
        writer.Write(DestinationStateName ?? "");
        writer.Write(TransitionDuration);
        writer.Write(Offset);
        writer.Write(ExitTime);
        writer.Write(HasExitTime);
        writer.Write(HasFixedDuration);
        foreach (var condition in Conditions)
        {
            condition.WriteBinary(writer);
        }
    }
}
