using System;
using System.Collections.Generic;
using System.Text;

namespace R2API.Models;

/// <summary>
/// Which AnimatorState transitions can interrupt the Transition.
/// </summary>
public enum InterruptionSource
{
    /// <summary>
    /// The Transition cannot be interrupted. Formerly know as Atomic.
    /// </summary>
    None,
    /// <summary>
    /// The Transition can be interrupted by transitions in the source AnimatorState.
    /// </summary>
    Source,
    /// <summary>
    /// The Transition can be interrupted by transitions in the destination AnimatorState.
    /// </summary>
    Destination,
    /// <summary>
    /// The Transition can be interrupted by transitions in the source or the destination AnimatorState.
    /// </summary>
    SourceThenDestination,
    /// <summary>
    /// The Transition can be interrupted by transitions in the source or the destination AnimatorState.
    /// </summary>
    DestinationThenSource
}
