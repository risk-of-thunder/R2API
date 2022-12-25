using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

namespace R2API.Utils;

/// <summary>
/// Alternative implementation for ConditionalWeakTable that actually works
/// </summary>
/// <typeparam name="TKey"></typeparam>
/// <typeparam name="TValue"></typeparam>
public class FixedConditionalWeakTable<TKey, TValue> : FixedConditionalWeakTableManager.IShrinkable
    where TKey : class
    where TValue : class
{
    private ConstructorInfo cachedConstructor = null;
    private readonly ConcurrentDictionary<WeakReferenceWrapper<TKey>, TValue> valueByKey = new(new WeakReferenceWrapperComparer<TKey>());

    public FixedConditionalWeakTable()
    {
        FixedConditionalWeakTableManager.Add(this);
    }

    /// <summary>
    /// Add a value for the specified key
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public void Add(TKey key, TValue value)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }
        if (!valueByKey.TryAdd(new WeakReferenceWrapper<TKey>(key, false), value))
        {
            throw new ArgumentException($"The key already exists");
        }
    }

    /// <summary>
    /// Removes a key and its value from the table.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public bool Remove(TKey key)
    {
        return valueByKey.TryRemove(new WeakReferenceWrapper<TKey>(key, true), out _);
    }

    /// <summary>
    /// Tries to get the value of the specified key.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public bool TryGetValue(TKey key, out TValue value)
    {
        return valueByKey.TryGetValue(new WeakReferenceWrapper<TKey>(key, true), out value);
    }

    /// <summary>
    /// Gets the value of the specified key, or creates a new one with defaultFunc and adds it to the table
    /// </summary>
    /// <param name="key"></param>
    /// <param name="defaultFunc"></param>
    /// <returns></returns>
    public TValue GetValue(TKey key, Func<TKey, TValue> defaultFunc)
    {
        if (TryGetValue(key, out var value))
        {
            return value;
        }

        value = defaultFunc(key);
        Add(key, value);
        return value;
    }

    /// <summary>
    /// Gets the value of the specified key, or creates a new one with default constructor and adds it to the table
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    /// <exception cref="MissingMethodException"></exception>
    public TValue GetOrCreateValue(TKey key)
    {
        if (TryGetValue(key, out var value))
        {
            return value;
        }

        if (cachedConstructor is null)
        {
            var type = typeof(TValue);
            cachedConstructor = type.GetConstructor(Array.Empty<Type>());
            if (cachedConstructor is null)
            {
                throw new MissingMethodException($"{type.FullName} doesn't have public parameterless constructor");
            }
        }

        value = (TValue)cachedConstructor.Invoke(Array.Empty<object>());
        Add(key, value);
        return value;
    }

    void FixedConditionalWeakTableManager.IShrinkable.Shrink()
    {
        foreach (var item in valueByKey)
        {
            if (!item.Key.weakReference.TryGetTarget(out _))
            {
                valueByKey.TryRemove(new WeakReferenceWrapper<TKey>(item.Key.targetHashCode), out _);
            }
        }
    }

    private readonly struct WeakReferenceWrapper<T> where T : class
    {
        public readonly int targetHashCode;
        public readonly WeakReference<T> weakReference;
        public readonly T target;

        public WeakReferenceWrapper(T target, bool strongReference)
        {
            targetHashCode = target.GetHashCode();
            if (strongReference)
            {
                this.target = target;
                weakReference = null;
            }
            else
            {
                this.target = null;
                weakReference = new WeakReference<T>(target);
            }
        }

        public WeakReferenceWrapper(int targetHashCode)
        {
            this.targetHashCode = targetHashCode;
            target = null;
            weakReference = null;
        }
    }

    private readonly struct WeakReferenceWrapperComparer<T> : IEqualityComparer<WeakReferenceWrapper<T>> where T : class
    {
        public bool Equals(WeakReferenceWrapper<T> first, WeakReferenceWrapper<T> second)
        {
            var firstTarget = first.target;
            var secondTarget = second.target;

            //No target and reference means we are looking for dead items to delete
            if (firstTarget is null && first.weakReference is null)
            {
                return !second.weakReference.TryGetTarget(out _);
            }
            if (secondTarget is null && second.weakReference is null)
            {
                return !first.weakReference.TryGetTarget(out _);
            }

            if (firstTarget is null && !first.weakReference.TryGetTarget(out firstTarget))
            {
                return false;
            }

            if (secondTarget is null && !second.weakReference.TryGetTarget(out secondTarget))
            {
                return false;
            }

            return firstTarget == secondTarget;
        }

        public int GetHashCode(WeakReferenceWrapper<T> obj)
        {
            return obj.targetHashCode;
        }
    }
}

internal static class FixedConditionalWeakTableManager
{
    private const int shrinkAttemptDelay = 2000;

    private static readonly object lockObject = new();
    private static readonly List<WeakReference<IShrinkable>> instances = new();
    private static int lastCollectionCount = 0;

    public static void Add(IShrinkable weakTable)
    {
        lock (lockObject)
        {
            if (instances.Count == 0)
            {
                new Thread(ShrinkThreadLoop).Start();
            }
            instances.Add(new WeakReference<IShrinkable>(weakTable));
        }
    }

    private static void ShrinkThreadLoop()
    {
        while (true)
        {
            //Once in a while if there was garbage collection clean up dead references
            Thread.Sleep(shrinkAttemptDelay);
            var newCollectionCount = GC.CollectionCount(2);
            if (lastCollectionCount == newCollectionCount)
            {
                continue;
            }
            lastCollectionCount = newCollectionCount;

            lock (lockObject)
            {
                for (var i = instances.Count - 1; i >= 0; i--)
                {
                    if (!instances[i].TryGetTarget(out var weakTable))
                    {
                        instances.RemoveAt(i);
                        continue;
                    }

                    weakTable.Shrink();
                }
                if (instances.Count == 0)
                {
                    return;
                }
            }
        }
    }

    internal interface IShrinkable
    {
        void Shrink();
    }
}
