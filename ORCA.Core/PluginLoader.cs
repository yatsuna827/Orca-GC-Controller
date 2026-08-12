using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ORCA.Plugin;

namespace ORCA.Core
{
    public static class PluginLoader
    {
        public static string DefaultDirectory => Path.Combine(AppContext.BaseDirectory, "Plugin");

        public static IEnumerable<(string CommandName, IMacroCommandParser<MacroCommand> Parser)> Load(string pluginDir)
        {
            if (!Directory.Exists(pluginDir)) yield break;

            foreach (var path in Directory.GetFiles(pluginDir).Where(_ => _.ToLower().EndsWith(".dll")))
            {
                var asm = Assembly.LoadFrom(path);

                foreach (var type in asm.GetTypes().Where(_ => !(_.IsGenericType || _.IsAbstract || _.IsInterface)))
                {
                    if (!type.GetConstructors().Any(_ => _.IsPublic && _.GetParameters().Length == 0)) continue;

                    var itf = type.GetInterfaces()
                        .FirstOrDefault(_ => _.IsGenericType && _.GetGenericTypeDefinition() == typeof(IMacroCommandParser<>));
                    if (itf == null) continue;

                    var attr = type.GetCustomAttribute<MacroCommandAttribute>();
                    if (attr is null) continue;

                    yield return (attr.CommandName, (IMacroCommandParser<MacroCommand>)Activator.CreateInstance(type));
                }
            }
        }
    }
}
