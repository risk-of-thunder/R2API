using System;
using System.IO;
using UnityEngine;

namespace R2API.Models;

/// <summary>
/// Represents a motion in the context of its parent blend tree.
/// </summary>
[Serializable]
public class ChildMotion
{
    [SerializeField]
    private string clipBundlePath;
    /// <summary>
    /// Full path to an AssetBundle that contains AnimationClip for this state.
    /// </summary>
    public string ClipBundlePath { get => clipBundlePath; set => clipBundlePath = value; }

    [SerializeField]
    private AnimationClip clip;
    /// <summary>
    /// AnimationClip that will be played in this motion. Leave null if BlendTree is set.
    /// </summary>
    public AnimationClip Clip { get => clip; set => clip = value; }

    [SerializeField]
    private BlendTree blendTree;
    /// <summary>
    /// BlendTree that will be played in this motion. Ignored if Clip is not null.
    /// </summary>
    public BlendTree BlendTree { get => blendTree; set => blendTree = value; }

    [SerializeField]
    private float threshold;
    /// <summary>
    /// The threshold of the child. Used in 1D blend trees.
    /// </summary>
    public float Threshold { get => threshold; set => threshold = value; }

    [SerializeField]
    private Vector2 position;
    /// <summary>
    /// The position of the child. Used in 2D blend trees.
    /// </summary>
    public Vector2 Position { get => position; set => position = value; }

    [SerializeField]
    private float timeScale;
    /// <summary>
    /// The relative speed of the child.
    /// </summary>
    public float TimeScale { get => timeScale; set => timeScale = value; }

    [SerializeField]
    private float cycleOffset;
    /// <summary>
    /// Normalized time offset of the child.
    /// </summary>
    public float CycleOffset { get => cycleOffset; set => cycleOffset = value; }

    [SerializeField]
    private string directBlendParameter;
    /// <summary>
    /// The parameter used by the child when used in a BlendTree of type BlendTreeType.Direct.
    /// </summary>
    public string DirectBlendParameter { get => directBlendParameter; set => directBlendParameter = value; }

    [SerializeField]
    private bool mirror;
    /// <summary>
    /// Mirror of the child.
    /// </summary>
    public bool Mirror { get => mirror; set => mirror = value; }

    /// <summary>
    /// Writing into a binary writer for caching purposes.
    /// </summary>
    /// <param name="writer"></param>
    public void WriteBinary(BinaryWriter writer)
    {
        writer.Write(ClipBundlePath ?? "");
        if (Clip)
        {
            writer.Write(Clip.name);
        }
        if (BlendTree)
        {
            BlendTree.WriteBinary(writer);
        }
        writer.Write(Threshold);
        writer.Write(Position.x);
        writer.Write(Position.y);
        writer.Write(TimeScale);
        writer.Write(CycleOffset);
        writer.Write(DirectBlendParameter ?? "");
        writer.Write(Mirror);
    }
}
