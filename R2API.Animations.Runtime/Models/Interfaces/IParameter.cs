using R2API.Models;

namespace R2API.Animations.Models.Interfaces;
/// <summary>
/// Used to communicate between scripting and the controller. Some parameters can be set in scripting and used by the controller,
/// while other parameters are based on Custom Curves in Animation Clips and can be sampled using the scripting API.
/// </summary>
public interface IParameter
{
    /// <summary>
    /// The name of the parameter.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// The type of the parameter. 
    /// </summary>
    ParameterType Type { get; }

    /// <summary>
    /// The default value for the parameter.
    /// </summary>
    ParameterValue Value { get; }
}
