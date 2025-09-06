using System;
using System.Collections.Generic;
using System.Text;
using R2API.Models;

namespace R2API.Animations.Models.Interfaces;
/// <summary>
/// Blend trees are used to blend continuously animation between their children.
/// They can either be 1D or 2D.
/// </summary>
public interface IBlendTree
{
    /// <summary>
    /// Parameter that is used to compute the blending weight of the children in 1D blend
    /// trees or on the X axis of a 2D blend tree.
    /// </summary>
    string BlendParameter { get; }

    /// <summary>
    /// Parameter that is used to compute the blending weight of the children on the
    /// Y axis of a 2D blend tree.
    /// </summary>
    string BlendParameterY { get; }

    /// <summary>
    /// The Blending type can be either 1D or different types of 2D.
    /// </summary>
    BlendTreeType BlendType { get; }

    /// <summary>
    /// A list of the blend tree child motions.
    /// </summary>
    IReadOnlyList<IChildMotion> Children { get; }
}
