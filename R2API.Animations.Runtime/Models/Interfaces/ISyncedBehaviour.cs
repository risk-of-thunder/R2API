using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace R2API.Animations.Models.Interfaces;
/// <summary>
/// Information about a behaviour for a state in Synced Layer
/// </summary>
public interface ISyncedBehaviour
{
    /// <summary>
    /// Name of a state from Synced Layer
    /// </summary>
    string StateName { get; }

    /// <summary>
    /// The path to a State as all parent StateMachines separated by . i.e. "StateMachine1.StateMachine2".
    /// If left empty it's assumed that the State is in main StateMachine for the layer.
    /// </summary>
    string StateMachinePath { get; }
    
    /// <summary>
    /// The Behaviour list assigned to this state.
    /// </summary>
    IReadOnlyList<StateMachineBehaviour> Behaviours { get; }
}
