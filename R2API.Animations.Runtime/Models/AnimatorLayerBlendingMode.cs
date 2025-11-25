namespace R2API.Models;

/// <summary>
/// Specifies how the layer is blended with the previous layers.
/// </summary>
public enum AnimatorLayerBlendingMode
{
    /// <summary>
    /// Animations overrides to the previous layers.
    /// </summary>
    Override,
    /// <summary>
    /// Animations are added to the previous layers.
    /// </summary>
    Additive
}
