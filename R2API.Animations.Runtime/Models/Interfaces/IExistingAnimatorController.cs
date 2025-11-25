using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace R2API.Animations.Models.Interfaces;
/// <summary>
/// Changes to a <see cref="RuntimeAnimatorController"/>.
/// </summary>
public interface IExistingAnimatorController
{
    /// <summary>
    /// Existing layers in source RuntimeAnimatorController that will be modified.
    /// </summary>
    IReadOnlyList<IExistingLayer> Layers { get; }

    /// <summary>
    /// New layers that will be added to RuntimeAnimatorController.
    /// </summary>
    IReadOnlyList<ILayer> NewLayers { get; }

    /// <summary>
    /// New parameters that will be added to RuntimeAnimatorController.
    /// </summary>
    IReadOnlyList<IParameter> NewParameters { get; }
}
