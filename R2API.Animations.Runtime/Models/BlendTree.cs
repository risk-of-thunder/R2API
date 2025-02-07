using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace R2API.Models;

/// <summary>
/// Blend trees are used to blend continuously animation between their children.
/// They can either be 1D or 2D.
/// </summary>
public class BlendTree : ScriptableObject
{
    [SerializeField]
    private string blendParameter;
    /// <summary>
    /// Parameter that is used to compute the blending weight of the children in 1D blend
    /// trees or on the X axis of a 2D blend tree.
    /// </summary>
    public string BlendParameter { get => blendParameter; set => blendParameter = value; }

    [SerializeField]
    private string blendParameterY;
    /// <summary>
    /// Parameter that is used to compute the blending weight of the children on the
    /// Y axis of a 2D blend tree.
    /// </summary>
    public string BlendParameterY { get => blendParameterY; set => blendParameterY = value; }

    [SerializeField]
    private BlendTreeType blendType;
    /// <summary>
    /// The Blending type can be either 1D or different types of 2D.
    /// </summary>
    public BlendTreeType BlendType { get => blendType; set => blendType = value; }

    [SerializeField]
    private List<ChildMotion> children = [];
    /// <summary>
    /// A list of the blend tree child motions.
    /// </summary>
    public List<ChildMotion> Children { get => children; }

    /// <summary>
    /// Writing into a binary writer for caching purposes.
    /// </summary>
    /// <param name="writer"></param>
    public void WriteBinary(BinaryWriter writer)
    {
        writer.Write(BlendParameter ?? "");
        writer.Write(BlendParameterY ?? "");
        writer.Write((int)BlendType);
        foreach (var child in Children)
        {
            child.WriteBinary(writer);
        }
    }
}
