using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace R2API.Animations.Models.Interfaces;
/// <summary>
/// Changes to an existing state.
/// </summary>
public interface IExistingState
{
    /// <summary>
    /// Name of the state.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// New transitions for this state.
    /// </summary>
    IReadOnlyList<ITransition> NewTransitions { get; }

    /// <summary>
    /// New behaviours for this state.
    /// </summary>
    IReadOnlyList<StateMachineBehaviour> NewBehaviours { get; }
}
