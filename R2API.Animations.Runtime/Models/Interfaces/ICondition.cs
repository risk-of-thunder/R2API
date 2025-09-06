using System;
using System.Collections.Generic;
using System.Text;
using R2API.Models;

namespace R2API.Animations.Models.Interfaces;
/// <summary>
/// Condition that is used to determine if a transition must be taken.
/// </summary>
public interface ICondition
{
    /// <summary>
    /// The mode of the condition.
    /// </summary>
    ConditionMode ConditionMode { get; }

    /// <summary>
    /// The name of the parameter used in the condition.
    /// </summary>
    string ParamName { get; }

    /// <summary>
    /// The Parameter's threshold value for the condition to be true.
    /// </summary>
    float Value { get; }
}
