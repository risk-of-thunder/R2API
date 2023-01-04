using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Linq;

namespace R2API.Utils;

/// <summary>
/// A Utility class used for injecting dependencies to SystemInitializer attributes.
/// </summary>
public static class SystemInitializerInjector
{
    private static Dictionary<Type, SystemInitializerAttribute> typeToSystemInitializer = new Dictionary<Type, SystemInitializerAttribute>();

    /// <summary>
    /// Injects the dependencies specified in <paramref name="dependenciesToInject"/> to the Type specified in <typeparamref name="T"/>
    /// </summary>
    /// <typeparam name="T">The type to modify it's sytem initializer attribute.</typeparam>
    /// <param name="dependenciesToInject">The dependencies to inject</param>
    public static void InjectDependencies<T>(params Type[] dependenciesToInject)
    {
        ThrowIfSystemInitializerExecuted();

        Type typeToInject = typeof(T);
        foreach(Type type in dependenciesToInject)
        {
            InjectDependencyInternal(typeToInject, type);
        }
    }

    /// <summary>
    /// Injects the dependency specified in <paramref name="dependencyToInject"/> to the Type specified in <typeparamref name="T"/>
    /// </summary>
    /// <typeparam name="T">The type to modify it's sytem initializer attribute.</typeparam>
    /// <param name="dependencyToInject">The dependency to inject</param>
    public static void InjectDependency<T>(Type dependencyToInject)
    {
        ThrowIfSystemInitializerExecuted();

        InjectDependencyInternal(typeof(T), dependencyToInject);
    }

    /// <summary>
    /// Injects the dependencies specified in <paramref name="dependenciesToInject"/> to the Type specified in <paramref name="typeToInject"/>
    /// </summary>
    /// <param name="typeToInject">The type to modify it's sytem initializer attribute.</param>
    /// <param name="dependenciesToInject">The dependencies to inject</param>
    public static void InjectDependencies(Type typeToInject, params Type[] dependenciesToInject)
    {
        ThrowIfSystemInitializerExecuted();

        foreach (Type type in dependenciesToInject)
        {
            InjectDependencyInternal(typeToInject, type);
        }
    }

    /// <summary>
    /// Injects the dependency specified in <paramref name="dependencyToInject"/> to the Type specified in <paramref name="typeToInject"/>
    /// </summary>
    /// <param name="typeToInject">The type to modify it's system initializer attribute</param>
    /// <param name="dependencyToInject">The dependency to inject</param>
    public static void InjectDependency(Type typeToInject, Type dependencyToInject)
    {
        ThrowIfSystemInitializerExecuted();

        InjectDependencyInternal(typeToInject, dependencyToInject);
    }

    private static void InjectDependencyInternal(Type typeToInject, Type dependency)
    {
        if (dependency == null || typeToInject == null)
            return;

        SystemInitializerAttribute attribute = GetSystemInitializerAttribute(typeToInject);

        if(attribute != null && !attribute.dependencies.Contains(dependency))
        {
            R2API.Logger.LogDebug($"Injecting {dependency.FullName} to {typeToInject.FullName}'s {nameof(SystemInitializerAttribute)}'s dependencies.");
            HG.ArrayUtils.ArrayAppend(ref attribute.dependencies, dependency);
        }
    }

    private static SystemInitializerAttribute GetSystemInitializerAttribute(Type typeToSearch)
    {
        if(typeToSystemInitializer.TryGetValue(typeToSearch, out var systemInitAttribute))
        {
            return systemInitAttribute;
        }
        MethodInfo[] candidateMethods = typeToSearch.GetMethods(BindingFlags.Static | BindingFlags.NonPublic);

        SystemInitializerAttribute foundAttribute = null;
        foreach(MethodInfo method in candidateMethods)
        {
            var attributeThatMayOrMayNotExistInMethod = method.GetCustomAttribute<SystemInitializerAttribute>();
            if(attributeThatMayOrMayNotExistInMethod != null)
            {
                foundAttribute = attributeThatMayOrMayNotExistInMethod;
                break;
            }
        }

        if(foundAttribute == null)
        {
            R2API.Logger.LogWarning($"Could not find a SystemInitializerAttribute inside {typeToSearch.AssemblyQualifiedName}");
            return foundAttribute;
        }

        typeToSystemInitializer.Add(typeToSearch, foundAttribute);
        return foundAttribute;
    }

    private static void ThrowIfSystemInitializerExecuted()
    {
        if(RoR2.SystemInitializerAttribute.hasExecuted)
        {
            throw new InvalidOperationException("Cannot inject dependencies when SystemInitializer has already been executed");
        }
    }
}
