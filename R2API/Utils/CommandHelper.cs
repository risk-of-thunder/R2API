using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using RoR2;

namespace R2API.Utils {
    /*
         This code belongs to Wildbook. 
         https://github.com/wildbook/R2Mods/blob/develop/Utilities/CommandHelper.cs
         Credit goes to Wildbook.         
             */

    public class CommandHelper {
        public static void RegisterCommands(RoR2.Console self) {
            var types = Assembly.GetEntryAssembly()?.GetTypes();
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
    }
}
