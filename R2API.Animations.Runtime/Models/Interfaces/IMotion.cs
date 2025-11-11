using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace R2API.Animations.Models.Interfaces;
/// <summary>
/// Information about what will be playing in a state
/// </summary>
public interface IMotion
{
    /// <summary>
    /// AnimationClip that will be played in this motion. Leave null if BlendTree is set.
    /// </summary>
    AnimationClip Clip { get; }

    /// <summary>
    /// BlendTree that will be played in this motion. Ignored if Clip is not null.
    /// </summary>
    IBlendTree BlendTree { get; }

}
