using System;
using System.Collections.Generic;
using System.Text;
using R2API.Models;
using UnityEngine;

namespace R2API.Animations.Models.Interfaces;
/// <summary>
/// Information about a new layer
/// </summary>
public interface ILayer
{
    /// <summary>
    /// Name of the layer
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Name of a layer after which this layer will be inserted, leave empty if this needs to be the first layer.
    /// </summary>
    string PreviousLayerName { get; }

    /// <summary>
    /// State machine for the layer
    /// </summary>
    IStateMachine StateMachine { get; }

    /// <summary>
    /// The AvatarMask that is used to mask the animation on the given layer.
    /// </summary>
    AvatarMask AvatarMask { get; }

    /// <summary>
    /// The blending mode used by the layer. It is not taken into account for the first layer.
    /// </summary>
    AnimatorLayerBlendingMode BlendingMode { get; }

    /// <summary>
    /// Name of the Synced Layer
    /// </summary>
    string SyncedLayerName { get; }

    /// <summary>
    /// When active, the layer will have an IK pass when evaluated. It will trigger an OnAnimatorIK callback.
    /// </summary>
    bool IKPass { get; }

    /// <summary>
    /// The default blending weight that the layers has. It is not taken into account for the first layer.
    /// </summary>
    float DefaultWeight { get; }

    /// <summary>
    /// When active, the layer will take control of the duration of the Synced Layer.
    /// </summary>
    bool SyncedLayerAffectsTiming { get; }

    /// <summary>
    /// Motions for Synced Layer
    /// </summary>
    IReadOnlyList<ISyncedMotion> SyncedMotions { get; }

    /// <summary>
    /// Behaviours for Synced Layer
    /// </summary>
    IReadOnlyList<ISyncedBehaviour> SyncedBehaviours { get; }
}
