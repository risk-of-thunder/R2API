namespace R2API.Models;

/// <summary>
/// Type of a parameter value
/// </summary>
public enum ParameterType
{
    /// <summary>
    /// float type
    /// </summary>
    Float = 1,
    /// <summary>
    /// int type
    /// </summary>
    Int = 3,
    /// <summary>
    /// bool type
    /// </summary>
    Bool = 4,
    /// <summary>
    /// Trigger work mostly like bool parameter, but their values are reset to false when used in a Transition 
    /// </summary>
    Trigger = 9
}
