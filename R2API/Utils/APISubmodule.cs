using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;

namespace R2API.Utils {

    [Flags]
    public enum InitStage {
        SetHooks = 0x00,
        Init = 0x1,

        UnsetHooks = 0xFF
    }

    // ReSharper disable once InconsistentNaming
    [AttributeUsage(AttributeTargets.Class)]
    public class R2APISubmodule : Attribute {
        public int Build;
    }

    // ReSharper disable once InconsistentNaming
    [AttributeUsage(AttributeTargets.Method)]
    public class R2APISubmoduleInit : Attribute {
        public InitStage Stage;
    }


    // ReSharper disable once InconsistentNaming
    public class APISubmoduleHandler {
        private readonly int _build;
        private readonly ManualLogSource _logger;

        public APISubmoduleHandler(int build, ManualLogSource logger = null) {
            _build = build;
            _logger = logger;
        }

        public void HandleAll(Assembly assembly) {
            var types = assembly.GetTypes().Where(APISubmoduleFilter).ToList();
            var faults = new Dictionary<Type, Exception>();

            types.ForEachTry(t => InvokeStage(t, InitStage.SetHooks), faults);
            types.ForEachTry(t => InvokeStage(t, InitStage.Init), faults);
            types.ForEachTry(t => t.SetFieldValue("IsInit", true), faults);

            faults.Keys.ForEachTry(t => {
                _logger?.Log(LogLevel.Error, $"{t.Name} could not be initialized: {faults[t]}");
                InvokeStage(t, InitStage.UnsetHooks);
            });
        }


        // ReSharper disable once InconsistentNaming
        private bool APISubmoduleFilter(Type type) {
            var attr = (R2APISubmodule) type
                .GetCustomAttributes(typeof(R2APISubmodule), false)
                .FirstOrDefault();

            if (attr == null)
                return false;

            if (attr.Build != default && attr.Build != _build)
                _logger?.Log(LogLevel.Message,
                    $"{type.Name} was built for build {attr.Build}, current build is {_build}.");

            return true;
        }

        private void InvokeStage(Type type, InitStage stage) {
            var method = type.GetMethods().Where(m => m.IsStatic && m.GetCustomAttributes(typeof(R2APISubmoduleInit))
                .Any(a => ((R2APISubmoduleInit) a).Stage == stage)).ToList();

            if (method.Count == 0) {
                _logger?.Log(LogLevel.Debug, $"{type.Name} has static method registered for {stage.ToString()}");
                return;
            }

            method.ForEach(m => m.Invoke(null, null));
        }
    }


    public static class EnumerableExtensions {

        public static void ForEachTry<T>(this IEnumerable<T> list, Action<T> action, IDictionary<T, Exception> blacklist = null) {
            // ReSharper disable once ImplicitlyCapturedClosure
            (blacklist == null ? list : list.Where(e => !blacklist.ContainsKey(e)))
                .ToList()
                .ForEach(element => {
                    try {
                        action(element);
                    }
                    catch (Exception exception) {
                        blacklist?.Add(element, exception);
                    }
                });
        }
    }
}
