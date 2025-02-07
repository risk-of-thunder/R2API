using System.Collections.Generic;
using UnityEngine;

namespace R2API.Models;

/// <summary>
/// An object containing a difference between 2 RuntimeAnimatorControllers.
/// </summary>
public class AnimatorDiff : ScriptableObject
{
    [SerializeField]
    public List<ExistingLayer> layers = [];
    /// <summary>
    /// Existing layers in source RuntimeAnimatorController that will be modified.
    /// </summary>
    public List<ExistingLayer> Layers { get => layers; }

    [SerializeField]
    private List<Parameter> newParameters = [];
    /// <summary>
    /// New parameters that will be added to RuntimeAnimatorController.
    /// </summary>
    public List<Parameter> NewParameters { get => newParameters; }
}
