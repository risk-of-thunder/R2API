using System;
using System.Collections.Generic;

namespace R2API.TestingLibrary;

public static class Assert
{
    public static void True(bool condition, string message = null)
    {
        if (!condition)
        {
            throw new AssertException(message ?? "Assertion failed. Condition is false.");
        }
    }

    public static void Same(object expected, object actual)
    {
        if (!ReferenceEquals(expected, actual))
        {
            throw new AssertException($"Assert.Same failed. Expected and actual do not reference the same object.");
        }
    }

    public static void NotSame(object expected, object actual)
    {
        if (ReferenceEquals(expected, actual))
        {
            throw new AssertException($"Assert.NotSame failed. Expected and actual reference the same object.");
        }
    }

    public static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new AssertException($"Assert.Equal failed. Expected: {expected}. Actual: {actual}.");
        }
    }

    public static void NotEqual<T>(T expected, T actual)
    {
        if (EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new AssertException($"Assert.NotEqual failed. Both values are equal: {expected}.");
        }
    }

    public static void Throws<T>(Action action) where T : Exception
    {
        try
        {
            action();
            throw new AssertException($"Assert.Throws failed. Expected exception of type {typeof(T)}.");
        }
        catch (T)
        {
            // Expected exception was thrown, test passes
        }
        catch (Exception ex)
        {
            throw new AssertException($"Assert.Throws failed. Expected exception of type {typeof(T)}, but got {ex.GetType()}.");
        }
    }

    public static void ThrowsAny<T>(Action action, string message = null) where T : Exception
    {
        message ??= string.Empty;

        try
        {
            action();
            message = string.Format("Any exception was expected but not thrown. {0}", message);
            throw new AssertException(message);
        }
        catch (Exception ex)
        {
            if (ex as T == null)
            {
                message = string.Format(
                        "An exception assignable to {0} was expected, but caught {1}. {2}",
                        typeof(T).Name, ex.GetType().Name, message);
                throw new AssertException(message);
            }
        }
    }

    public static void NotNull(object obj)
    {
        if (obj == null)
        {
            throw new AssertException("Assert.NotNull failed. Object is null.");
        }
    }
}

public class AssertException : Exception
{
    public AssertException(string message) : base(message) { }
}
