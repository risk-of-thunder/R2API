using System;
using System.Collections.Generic;
using R2API.Animations.Models.Interfaces;
using UnityEngine;

namespace R2API.Models;

/// <inheritdoc cref="ILayer"/>
[Serializable]
public class Layer : ILayer
{
    [SerializeField]
    private string name;
    /// <inheritdoc/>
    public string Name { get => name; set => name = value; }

    [SerializeField]
    private string previousLayerName;
    /// <inheritdoc/>
    public string PreviousLayerName { get => previousLayerName; set => previousLayerName = value; }

    [SerializeField]
    private StateMachine stateMachine;
    /// <inheritdoc cref="ILayer.StateMachine"/>
    public StateMachine StateMachine { get => stateMachine; set => stateMachine = value; }
    IStateMachine ILayer.StateMachine { get => stateMachine; }

    [SerializeField]
    private AvatarMask avatarMask;
    /// <inheritdoc/>
    public AvatarMask AvatarMask { get => avatarMask; set => avatarMask = value; }

    [SerializeField]
    private AnimatorLayerBlendingMode blendingMode;
    /// <inheritdoc/>
    public AnimatorLayerBlendingMode BlendingMode { get => blendingMode; set => blendingMode = value; }

    [SerializeField]
    private string syncedLayerName;
    /// <inheritdoc/>
    public string SyncedLayerName { get => syncedLayerName; set => syncedLayerName = value; }

    [SerializeField]
    private bool iKPass;
    /// <inheritdoc/>
    public bool IKPass { get => iKPass; set => iKPass = value; }

    [SerializeField]
    private float defaultWeight;
    /// <inheritdoc/>
    public float DefaultWeight { get => defaultWeight; set => defaultWeight = value; }

    [SerializeField]
    private bool syncedLayerAffectsTiming;
    /// <inheritdoc/>
    public bool SyncedLayerAffectsTiming { get => syncedLayerAffectsTiming; set => syncedLayerAffectsTiming = value; }

    [SerializeField]
    private List<SyncedMotion> syncedMotions = [];
    /// <inheritdoc cref="ILayer.SyncedMotions"/>
    public List<SyncedMotion> SyncedMotions { get => syncedMotions; }
    IReadOnlyList<ISyncedMotion> ILayer.SyncedMotions { get => syncedMotions; }

    [SerializeField]
    private List<SyncedBehaviour> syncedBehaviours = [];
    /// <inheritdoc cref="ILayer.SyncedBehaviours"/>
    public List<SyncedBehaviour> SyncedBehaviours { get => syncedBehaviours; }
    IReadOnlyList<ISyncedBehaviour> ILayer.SyncedBehaviours { get => syncedBehaviours; }
}
