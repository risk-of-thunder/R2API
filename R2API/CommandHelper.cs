using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using RoR2;
using RoR2.ConVar;
using UnityEngine;
using R2API.Utils;

namespace R2API {
    [R2APISubmodule]
    public class CommandHelper {

        private static Queue<Assembly> assemblies = new Queue<Assembly>();
        private static RoR2.Console console = null;


        /** <summary>
         * Scans the calling assembly for ConCommand attributes and Convar fields and adds these to the console.
         * This method may be called at any time.
         * </summary>
         */
        public static void AddToConsoleWhenReady() {
            Assembly assembly = Assembly.GetCallingAssembly();
            if (assembly == null) {
                return;
            }
            assemblies.Enqueue(assembly);
            HandleCommandsConvars();
        }

        /** <summary>
         * Exactly the same as AddToConsoleWhenReady(): use that method instead.
         * </summary>
         */
        [Obsolete("Use 'AddToConsoleWhenReady()' instead.")]
        public static void RegisterCommands(RoR2.Console _) {
            Assembly assembly = Assembly.GetCallingAssembly();
            if (assembly == null) {
                return;
            }
            assemblies.Enqueue(assembly);
            HandleCommandsConvars();
        }

        [R2APISubmoduleInit(Stage = InitStage.SetHooks)]
        internal static void SetHooks() {
            On.RoR2.Console.InitConVars += ConsoleReady;
        }

        [R2APISubmoduleInit(Stage = InitStage.UnsetHooks)]
        internal static void UnsetHooks() {
            On.RoR2.Console.InitConVars -= ConsoleReady;
        }

        private static void ConsoleReady(On.RoR2.Console.orig_InitConVars orig, RoR2.Console self) {
            orig(self);
            console = self;
            HandleCommandsConvars();
        }

        private static void HandleCommandsConvars() {
            if (console == null) return;

            while (assemblies.Count > 0) {
                Assembly assembly = assemblies.Dequeue();
                RegisterCommands(assembly);
                RegisterConVars(assembly);
            }
        }

        private static void RegisterCommands(Assembly assembly) {
            /*
        This code belongs to Wildbook. 
        https://github.com/wildbook/R2Mods/blob/develop/Utilities/CommandHelper.cs
        Credit goes to Wildbook.         
            */
            var types = assembly?.GetTypes();
            if (types == null) {
                return;
            }

            var catalog = console.GetFieldValue<IDictionary>("concommandCatalog");
            const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            var methods = types.SelectMany(x =>
                x.GetMethods(flags).Where(m => m.GetCustomAttribute<ConCommandAttribute>() != null));

            foreach (var methodInfo in methods) {
                var attributes = methodInfo.GetCustomAttributes<ConCommandAttribute>();
                foreach (var attribute in attributes) {
                    var conCommand = Reflection.GetNestedType<RoR2.Console>("ConCommand").Instantiate();

                    conCommand.SetFieldValue("flags", attribute.flags);
                    conCommand.SetFieldValue("helpText", attribute.helpText);
                    conCommand.SetFieldValue("action", (RoR2.Console.ConCommandDelegate)Delegate.CreateDelegate(typeof(RoR2.Console.ConCommandDelegate), methodInfo));

                    catalog[attribute.commandName.ToLower()] = conCommand;
                }
            }
        }

        private static void RegisterConVars(Assembly assembly) {
            List<BaseConVar> customVars = new List<BaseConVar>();
            foreach (Type type in assembly.GetTypes()) {
                foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)) {
                    if (field.FieldType.IsSubclassOf(typeof(BaseConVar))) {
                        if (field.IsStatic) {
                            console.InvokeMethod("RegisterConVarInternal", (BaseConVar)field.GetValue(null));
                            //console.RegisterConVarInternal((BaseConVar)field.GetValue(null));//This line fails on Release.
                            customVars.Add((BaseConVar)field.GetValue(null));
                        } else if (CustomAttributeExtensions.GetCustomAttribute<CompilerGeneratedAttribute>(type) == null)
                            Debug.LogErrorFormat("ConVar defined as {0} in {1}. {2} could not be registered. ConVars must be static fields.", type.Name, assembly.FullName, field.Name);
                    }
                }
            }
            foreach (BaseConVar baseConVar in customVars) {
                if ((baseConVar.flags & ConVarFlags.Engine) != ConVarFlags.None)
                    baseConVar.defaultValue = baseConVar.GetString();
                else if (baseConVar.defaultValue != null)
                    baseConVar.SetString(baseConVar.defaultValue);
            }
        }
    }
}
//
