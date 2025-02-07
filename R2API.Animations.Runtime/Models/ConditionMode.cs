namespace R2API.Models;

/// <summary>
/// The mode of the condition.
/// </summary>
public enum ConditionMode
{
    /// <summary>
    /// The condition is true when the parameter value is true.
    /// </summary>
    IsTrue = 1,
    /// <summary>
    /// The condition is true when the parameter value is false.
    /// </summary>
    IsFalse = 2,
    /// <summary>
    /// The condition is true when parameter value is greater than the threshold.
    /// </summary>
    IsGreater = 3,
    /// <summary>
    /// The condition is true when the parameter value is less than the threshold.
    /// </summary>
    IsLess = 4,
    //IsExitTime = 5,
    /// <summary>
    /// The condition is true when parameter value is equal to the threshold.
    /// </summary>
    IsEqual = 6,
    /// <summary>
    /// The condition is true when the parameter value is not equal to the threshold.
    /// </summary>
    IsNotEqual = 7,
}
