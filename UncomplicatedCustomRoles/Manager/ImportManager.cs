using Exiled.API.Interfaces;
using Exiled.Loader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UncomplicatedCustomRoles.API.Attributes;
using UncomplicatedCustomRoles.API.Features;
using UncomplicatedCustomRoles.API.Interfaces;

namespace UncomplicatedCustomRoles.Manager
{
    internal class ImportManager
    {
        public static HashSet<IPlugin<IConfig>> ActivePlugins { get; } = new HashSet<IPlugin<IConfig>>();
        private static bool _alreadyLoaded = false;

        public static void Init()
        {
            if (_alreadyLoaded)
                return;

            ActivePlugins.Clear();
            Task.Run(Actor);
        }

        private static void Actor()
        {
            try
            {
                if (Plugin.Instance == null)
                {
                    if (Plugin.Instance.Config.EnableBasicLogs) {
                        LogManager.Error("Plugin.Instance is NULL! Aborting import.");
                    }
                    
                    return;
                }

                _alreadyLoaded = true;
                List<IPlugin<IConfig>> pluginsToProcess = Loader.Plugins?.ToList() ?? new List<IPlugin<IConfig>>();

                if (Plugin.Instance.Config.EnableBasicLogs) {
                    LogManager.Info("Starting import of CustomRoles from other plugins...");
                    LogManager.Debug($"Total Plugins Found: {Loader.Plugins?.Count() ?? 0}");
                    LogManager.Debug($"Plugins to Process: {pluginsToProcess.Count}");
                }

                foreach (IPlugin<IConfig> plugin in pluginsToProcess)
                {
                    if (plugin == null)
                    {
                        if (Plugin.Instance.Config.EnableBasicLogs) {
                            LogManager.Warn("Null plugin found, skipping...");
                        }
                        
                        continue;
                    }

                    if (Plugin.Instance.Config.EnableBasicLogs) {
                        LogManager.Debug($"Processing plugin: {plugin.Name}");
                    }

                    try
                    {
                        System.Reflection.Assembly assembly = plugin.Assembly;
                        if (assembly == null)
                        {
                            if (Plugin.Instance.Config.EnableBasicLogs) {
                                LogManager.Error($"Assembly for plugin {plugin.Name} is null.");
                            }
                            
                            continue;
                        }

                        Type[] types = assembly.GetTypes();
                        foreach (Type type in types)
                        {
                            object[] attribs = type.GetCustomAttributes(typeof(PluginCustomRole), false);
                            if (attribs.Length > 0 && typeof(ICustomRole).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                            {
                                if (Plugin.Instance.Config.EnableBasicLogs) {
                                    LogManager.Info($"Found role: {type.FullName}");
                                }
                                    
                                try
                                {
                                    object instance = Activator.CreateInstance(type);
                                    if (instance is ICustomRole role)
                                    {
                                        if (Plugin.Instance.Config.EnableBasicLogs) {
                                            LogManager.Info($"Imported role: {role.Name} (ID: {role.Id}) from {plugin.Name}");
                                        }
                                        
                                        CustomRole.Register(role);
                                    }
                                    else
                                    {
                                        if (Plugin.Instance.Config.EnableBasicLogs) {
                                            LogManager.Error($"Error: Instance of {type.FullName} is null or not of type ICustomRole.");
                                        }
                                    }
                                }
                                catch (Exception instEx)
                                {
                                    if (Plugin.Instance.Config.EnableBasicLogs) {
                                        LogManager.Error($"Error creating instance of {type.FullName}: {instEx.Message}");
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception pluginEx)
                    {
                        if (Plugin.Instance.Config.EnableBasicLogs) {
                            LogManager.Error($"Error processing plugin {plugin.Name}: {pluginEx.Message}");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                if (Plugin.Instance.Config.EnableBasicLogs) {
                    LogManager.Error($"{e.Message}, {e.StackTrace}");
                }
            }
        }
    }
}
