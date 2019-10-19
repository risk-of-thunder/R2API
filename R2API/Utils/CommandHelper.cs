using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using RoR2;
using RoR2.ConVar;
using UnityEngine;

namespace R2API.Utils {
    /*
         This code belongs to Wildbook. 
         https://github.com/wildbook/R2Mods/blob/develop/Utilities/CommandHelper.cs
         Credit goes to Wildbook.         
             */

    public class CommandHelper {
        public static void RegisterCommands(RoR2.Console self) {
            var types = Assembly.GetCallingAssembly()?.GetTypes();
            if (types == null) {
                return;
            }

            var catalog = self.GetFieldValue<IDictionary>("concommandCatalog");
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

        public static void RegisterConVars(RoR2.Console self) {
            var assembly = Assembly.GetCallingAssembly();
            if(assembly==null) {
                return;
            }

            if (self.allConVars == null) {
                Debug.LogErrorFormat("Can't register the convars from mod {0} before the game does. Try doing it after initConvars!",assembly.FullName);
            }

            List<BaseConVar> customVars = new List<BaseConVar>();
            foreach (Type type in assembly.GetTypes()) {
                foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)) {
                    if (field.FieldType.IsSubclassOf(typeof(BaseConVar))) {
                        if (field.IsStatic) {
                            self.RegisterConVarInternal((BaseConVar)field.GetValue(null));
                            customVars.Add((BaseConVar) field.GetValue(null));
                        }
                        else if (CustomAttributeExtensions.GetCustomAttribute<CompilerGeneratedAttribute>(type) == null)
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
