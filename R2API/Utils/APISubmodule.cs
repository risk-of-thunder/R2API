using BepInEx.Logging;
using JetBrains.Annotations;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

// ReSharper disable UnusedMember.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace R2API.Utils {

    [Flags]
    internal enum InitStage {
        SetHooks = 1 << 0,
        Load = 1 << 1,
        Unload = 1 << 2,
        UnsetHooks = 1 << 3,
        LoadCheck = 1 << 4,
    }

    // ReSharper disable once InconsistentNaming
    [MeansImplicitUse]
    [AttributeUsage(AttributeTargets.Class)]
    internal class R2APISubmodule : Attribute {
        public int Build;
    }

    // ReSharper disable once InconsistentNaming
    [MeansImplicitUse]
    [AttributeUsage(AttributeTargets.Method)]
    internal class R2APISubmoduleInit : Attribute {
        public InitStage Stage;
    }

    /// <summary>
    /// Attribute to have at the top of your BaseUnityPlugin class if you want to load a specific R2API Submodule.
    /// Parameter(s) are the nameof the submodules.
    /// e.g: [R2APISubmoduleDependency("SurvivorAPI", "ItemAPI")]
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public class R2APISubmoduleDependency : Attribute {
        public string?[]? SubmoduleNames { get; }

        public R2APISubmoduleDependency(params string[] submoduleName) {
            SubmoduleNames = submoduleName;
        }
    }

    // ReSharper disable once InconsistentNaming
    /// <summary>
    ///
    /// </summary>
    public class APISubmoduleHandler {
        private readonly int _build;
        private readonly ManualLogSource? _logger;
        private HashSet<string> _moduleSet;
        private static HashSet<string> LoadedModules;

        internal APISubmoduleHandler(int build, ManualLogSource? logger = null) {
            _build = build;
            _logger = logger;
        }

        /// <summary>
        /// Return true if the specified submodule is loaded.
        /// </summary>
        /// <param name="submodule">nameof the submodule</param>
        public static bool IsLoaded(string? submodule) => LoadedModules.Contains(submodule);

        internal HashSet<string> LoadRequested(PluginScanner? pluginScanner) {
            _moduleSet = new HashSet<string>();

            void AddModuleToSet(IEnumerable<CustomAttributeArgument> arguments) {
                foreach (var arg in arguments) {
                    if (arg.Value != null) {
                        foreach (var stringElement in (CustomAttributeArgument[])arg.Value) {
                            if (stringElement.Value != null) {
                                _moduleSet.Add((string)stringElement.Value);
                            }
                        }
                    }
                }
            }

            void CallWhenAssembliesAreScanned() {
                var moduleTypes = Assembly.GetExecutingAssembly().GetTypes().Where(APISubmoduleFilter).ToList();

                foreach (var moduleType in moduleTypes) {
                    R2API.Logger.LogInfo($"Enabling R2API Submodule: {moduleType.Name}");
                }

                var faults = new Dictionary<Type, Exception>();
                LoadedModules = new HashSet<string>();

                moduleTypes
                    .ForEachTry(t => InvokeStage(t, InitStage.SetHooks, null), faults);
                moduleTypes.Where(t => !faults.ContainsKey(t))
                    .ForEachTry(t => InvokeStage(t, InitStage.Load, null), faults);

                faults.Keys.ForEachTry(t => {
                    _logger?.Log(LogLevel.Error, $"{t.Name} could not be initialized and has been disabled:\n\n{faults[t]}");
                    InvokeStage(t, InitStage.UnsetHooks, null);
                });

                moduleTypes.Where(t => !faults.ContainsKey(t))
                    .ForEachTry(t => t.SetFieldValue("_loaded", true));
                moduleTypes.Where(t => !faults.ContainsKey(t))
                    .ForEachTry(t => LoadedModules.Add(t.Name));
            }

            var scanRequest = new PluginScanner.AttributeScanRequest(attributeTypeFullName: typeof(R2APISubmoduleDependency).FullName,
                attributeTargets: AttributeTargets.Assembly | AttributeTargets.Class,
                CallWhenAssembliesAreScanned, oneMatchPerAssembly: false,
                foundOnAssemblyAttributes: (assembly, arguments) =>
                    AddModuleToSet(arguments),
                foundOnAssemblyTypes: (type, arguments) =>
                    AddModuleToSet(arguments)
                );

            pluginScanner.AddScanRequest(scanRequest);

            return LoadedModules;
        }

        // ReSharper disable once InconsistentNaming
        private bool APISubmoduleFilter(Type type) {
            var attr = type.GetCustomAttribute<R2APISubmodule>();

            if (attr == null)
                return false;

            if (R2API.DebugMode) {
                return true;
            }

            // Comment this out if you want to try every submodules working (or not) state
            if (!_moduleSet.Contains(type.Name)) {
                var shouldload = new object[1];
                InvokeStage(type, InitStage.LoadCheck, shouldload);
                if (!(shouldload[0] is bool)) {
                    return false;
                }

                if (!(bool)shouldload[0]) {
                    return false;
                }
            }

            if (attr.Build != default && attr.Build != _build)
                _logger?.Log(LogLevel.Debug,
                    $"{type.Name} was built for build {attr.Build}, current build is {_build}.");

            return true;
        }

        private void InvokeStage(Type type, InitStage stage, object[]? parameters) {
            var method = type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                .Where(m => m.GetCustomAttributes(typeof(R2APISubmoduleInit))
                .Any(a => ((R2APISubmoduleInit)a).Stage.HasFlag(stage))).ToList();

            if (method.Count == 0) {
                _logger?.Log(LogLevel.Debug, $"{type.Name} has no static method registered for {stage}");
                return;
            }

            method.ForEach(m => m.Invoke(null, parameters));
        }
    }

    public static class EnumerableExtensions {

        /// <summary>
        /// ForEach but with a try catch in it.
        /// </summary>
        /// <param name="list">the enumerable object</param>
        /// <param name="action">the action to do on it</param>
        /// <param name="exceptions">the exception dictionary that will get filled, null by default if you simply want to silence the errors if any pop.</param>
        /// <typeparam name="T"></typeparam>
        public static void ForEachTry<T>(this IEnumerable<T>? list, Action<T>? action, IDictionary<T, Exception?>? exceptions = null) {
            list.ToList().ForEach(element => {
                try {
                    action(element);
                }
                catch (Exception exception) {
                    exceptions?.Add(element, exception);
                }
            });
        }
    }
}
