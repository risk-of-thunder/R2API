using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace R2API.Utils;

/// <summary>
/// A Utility class used for injecting dependencies to SystemInitializer attributes.
/// </summary>
public static class SystemInitializerInjector
{
    static readonly Dictionary<MethodInfo, HashSet<Type>> _dependenciesToInject = new Dictionary<MethodInfo, HashSet<Type>>();

    static bool _appliedHooks = false;
    static void applyHooksIfNeeded()
    {
        if (_appliedHooks)
            return;

        IL.RoR2.SystemInitializerAttribute.Execute += il =>
        {
            ILCursor c = new ILCursor(il);

            if (c.TryGotoNext(x => x.MatchCallOrCallvirt(SymbolExtensions.GetMethodInfo<Queue<SystemInitializerAttribute>>(_ => _.Enqueue(default)))))
            {
                c.Emit(OpCodes.Dup);
                c.EmitDelegate((SystemInitializerAttribute instance) =>
                {
                    if (instance.target is MethodInfo initializerMethod && _dependenciesToInject.TryGetValue(initializerMethod, out HashSet<Type> newDependencies))
                    {
                        newDependencies.RemoveWhere(t => instance.dependencies.Contains(t));
                        if (newDependencies.Count == 0)
                            return;

                        int originalDependenciesLength = instance.dependencies.Length;
                        Array.Resize(ref instance.dependencies, originalDependenciesLength + newDependencies.Count);
                        newDependencies.CopyTo(instance.dependencies, originalDependenciesLength);

#if DEBUG
                        R2API.Logger.LogDebug($"SystemInitializerInjector: Injected {newDependencies.Count} dependencies into {initializerMethod.DeclaringType.FullName}.{initializerMethod.Name}");
#endif
                    }
                });
            }
            else
            {
                R2API.Logger.LogError("SystemInitializerInjector: Failed to find IL patch location");
            }
        };

        _appliedHooks = true;
    }

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
        if (typeToInject is null)
            throw new ArgumentNullException(nameof(typeToInject));

        if (dependency is null)
            throw new ArgumentNullException(nameof(dependency));

        foreach (MethodInfo initializerMethod in GetAllSystemInitializerMethods(typeToInject))
        {
            InjectDependencyInternal(initializerMethod, dependency);
        }
    }

    private static IEnumerable<MethodInfo> GetAllSystemInitializerMethods(Type type)
    {
        return type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                   .Where(m => m.GetCustomAttribute<SystemInitializerAttribute>() != null);
    }

    /// <summary>
    /// Injects <paramref name="dependency"/> as a dependency into the SystemInitializer method <paramref name="initializerMethod"/>
    /// </summary>
    /// <param name="initializerMethod">The initializer method to inject the dependency into</param>
    /// <param name="dependency">The dependency type to inject</param>
    public static void InjectDependency(MethodInfo initializerMethod, Type dependency)
    {
        ThrowIfSystemInitializerExecuted();

        InjectDependencyInternal(initializerMethod, dependency);
    }

    /// <summary>
    /// Injects <paramref name="dependencies"/> as dependencies into the SystemInitializer method <paramref name="initializerMethod"/>
    /// </summary>
    /// <param name="initializerMethod">The initializer method to inject the dependencies into</param>
    /// <param name="dependencies">The dependency types to inject</param>
    public static void InjectDependencies(MethodInfo initializerMethod, params Type[] dependencies)
    {
        ThrowIfSystemInitializerExecuted();

        foreach (Type dependency in dependencies)
        {
            InjectDependencyInternal(initializerMethod, dependency);
        }
    }

    private static void InjectDependencyInternal(MethodInfo initializerMethod, Type dependency)
    {
        if (dependency is null)
            throw new ArgumentNullException(nameof(dependency));

        if (initializerMethod is null)
            throw new ArgumentNullException(nameof(initializerMethod));

        if (initializerMethod.GetCustomAttribute<SystemInitializerAttribute>() == null)
        {
            R2API.Logger.LogWarning($"Not injecting SystemInitializer dependency {dependency.FullName} into {initializerMethod.DeclaringType.FullName}.{initializerMethod.Name}: Method is missing {nameof(SystemInitializerAttribute)}");
            return;
        }

        if (!_dependenciesToInject.TryGetValue(initializerMethod, out HashSet<Type> injectedDependencies))
            _dependenciesToInject.Add(initializerMethod, injectedDependencies = new HashSet<Type>());

        if (injectedDependencies.Add(dependency))
        {
            applyHooksIfNeeded();
            R2API.Logger.LogDebug($"Injecting SystemInitializer dependency {dependency.FullName} into {initializerMethod.DeclaringType.FullName}.{initializerMethod.Name}");
        }
    }

    private static void ThrowIfSystemInitializerExecuted()
    {
        if(RoR2.SystemInitializerAttribute.hasExecuted)
        {
            throw new InvalidOperationException("Cannot inject dependencies when SystemInitializer has already been executed");
        }
    }
}
