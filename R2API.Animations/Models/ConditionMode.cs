using System;
using System.Collections.Generic;
using System.Text;

namespace R2API.Models;

public enum ConditionMode
{
    IsTrue = 1,
    IsFalse = 2,
    IsGreater = 3,
    IsLess = 4,
    //IsExitTime = 5,
    IsEqual = 6,
    IsNotEqual = 7,
}
