using System;
using System.Collections.Generic;
using System.Text;

namespace R2API.Animations.Models.Interfaces;
/// <summary>
/// Changes to a layer.
/// </summary>
public interface IExistingLayer
{
    /// <summary>
    /// Name of the layer.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Changes to a state machine for this layer.
    /// </summary>
    IExistingStateMachine StateMachine { get; }

    /// <summary>
    /// New behaviours if the layer is synced.
    /// </summary>
    IReadOnlyList<ISyncedBehaviour> NewSyncedBehaviours { get; }
}
