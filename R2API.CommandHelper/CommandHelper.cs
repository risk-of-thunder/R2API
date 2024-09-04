using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using R2API.AutoVersionGen;
using RoR2;
using RoR2.ConVar;

namespace R2API.Utils;

/// <summary>
/// A submodule for scanning static methods of a given assembly
/// so that they are registered as console commands for the in-game console.
/// </summary>
[Obsolete($"Add [assembly: HG.Reflection.SearchableAttribute.OptInAttribute] to your assembly instead")]
#pragma warning disable CS0436 // Type conflicts with imported type
[AutoVersion]
#pragma warning restore CS0436 // Type conflicts with imported type
public partial class CommandHelper
{
    public const string PluginGUID = R2API.PluginGUID + ".commandhelper";
    public const string PluginName = R2API.PluginName + ".CommandHelper";

    private static readonly Queue<Assembly> Assemblies = new Queue<Assembly>();
    private static RoR2.Console _console;

    /// <summary>
    /// Return true if the submodule is loaded.
    /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
    [Obsolete(R2APISubmoduleDependency.PropertyObsolete)]
#pragma warning restore CS0618 // Type or member is obsolete
    public static bool Loaded => true;

    /// <summary>
    /// Scans the calling assembly for ConCommand attributes and Convar fields and adds these to the console.
    /// This method may be called at any time.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public static void AddToConsoleWhenReady()
    {
        Utils.CommandHelper.SetHooks();
        var assembly = Assembly.GetCallingAssembly();
        Assemblies.Enqueue(assembly);
        HandleCommandsConvars();
    }

    private static bool _hooksEnabled = false;

    internal static void SetHooks()
    {
        if (_hooksEnabled)
        {
            return;
        }

        On.RoR2.Console.InitConVarsCoroutine += ConsoleReady;

        _hooksEnabled = true;
    }

    internal static void UnsetHooks()
    {
        On.RoR2.Console.InitConVarsCoroutine -= ConsoleReady;

        _hooksEnabled = false;
    }

    private static System.Collections.IEnumerator ConsoleReady(On.RoR2.Console.orig_InitConVarsCoroutine orig, RoR2.Console self)
    {
        yield return orig(self);

        _console = self;
        HandleCommandsConvars();
    }

    private static void HandleCommandsConvars()
    {
        if (_console == null)
        {
            return;
        }

        while (Assemblies.Count > 0)
        {
            var assembly = Assemblies.Dequeue();
            RegisterCommands(assembly);
            RegisterConVars(assembly);
        }
    }

    private static void RegisterCommands(Assembly assembly)
    {
        if (assembly == null)
        {
            return;
        }
        _ = Reflection.GetTypesSafe(assembly, out var types);

        try
        {
            var catalog = _console.concommandCatalog;
            const BindingFlags consoleCommandsMethodFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            var methods = types.SelectMany(t =>
                t.GetMethods(consoleCommandsMethodFlags).Where(m => m.GetCustomAttribute<ConCommandAttribute>() != null));

            foreach (var methodInfo in methods)
            {
                if (!methodInfo.IsStatic)
                {
                    CommandHelperPlugin.Logger.LogError($"ConCommand defined as {methodInfo.Name} in {assembly.FullName} could not be registered. " +
                        "ConCommands must be static methods.");
                    continue;
                }

                var attributes = methodInfo.GetCustomAttributes<ConCommandAttribute>();
                foreach (var attribute in attributes)
                {
                    var conCommand = new RoR2.Console.ConCommand
                    {
                        flags = attribute.flags,
                        helpText = attribute.helpText,
                        action = (RoR2.Console.ConCommandDelegate)Delegate.CreateDelegate(
                            typeof(RoR2.Console.ConCommandDelegate), methodInfo)
                    };

                    catalog[attribute.commandName.ToLower()] = conCommand;
                }
            }
        }
        catch (Exception e)
        {
            CommandHelperPlugin.Logger.LogError($"{nameof(CommandHelper)} failed to scan the assembly called {assembly.FullName}. Exception : {e}");
        }
    }

    private static void RegisterConVars(Assembly assembly)
    {
        if (assembly == null)
        {
            return;
        }
        _ = Reflection.GetTypesSafe(assembly, out var types);

        try
        {
            var customVars = new List<BaseConVar>();
            foreach (var type in types)
            {
                foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (field.FieldType.IsSubclassOf(typeof(BaseConVar)))
                    {
                        if (field.IsStatic)
                        {
                            _console.RegisterConVarInternal((BaseConVar)field.GetValue(null));
                            customVars.Add((BaseConVar)field.GetValue(null));
                        }
                        else if (type.GetCustomAttribute<CompilerGeneratedAttribute>() == null)
                        {
                            CommandHelperPlugin.Logger.LogError(
                                $"ConVar defined as {type.Name} in {assembly.FullName}. {field.Name} could not be registered. ConVars must be static fields.");
                        }
                    }
                }
            }
            foreach (var baseConVar in customVars)
            {
                if ((baseConVar.flags & ConVarFlags.Engine) != ConVarFlags.None)
                {
                    baseConVar.defaultValue = baseConVar.GetString();
                }
                else if (baseConVar.defaultValue != null)
                {
                    baseConVar.AttemptSetString(baseConVar.defaultValue);
                }
            }
        }
        catch (Exception e)
        {
            CommandHelperPlugin.Logger.LogError($"{nameof(CommandHelper)} failed to scan the assembly called {assembly.FullName}. Exception : {e}");
        }
    }
}
