using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace R2API.Animations.Models.Interfaces;
/// <summary>
/// Represents a motion in the context of its parent blend tree.
/// </summary>
public interface IChildMotion : IMotion
{
    /// <summary>
    /// The threshold of the child. Used in 1D blend trees.
    /// </summary>
    float Threshold { get; }

    /// <summary>
    /// The position of the child. Used in 2D blend trees.
    /// </summary>
    Vector2 Position { get; }

    /// <summary>
    /// The relative speed of the child.
    /// </summary>
    float TimeScale { get; }

    /// <summary>
    /// Normalized time offset of the child.
    /// </summary>
    float CycleOffset { get; }

    /// <summary>
    /// The parameter used by the child when used in a BlendTree of type BlendTreeType.Direct.
    /// </summary>
    string DirectBlendParameter { get; }

    /// <summary>
    /// Mirror of the child.
    /// </summary>
    bool Mirror { get; }
}
