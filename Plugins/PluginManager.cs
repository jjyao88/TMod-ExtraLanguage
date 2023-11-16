using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;


namespace ExtraLanguage.Plugins
{
    public class PluginManager : IDisposable {
        private Dictionary<string, BasePlugin> LoadedPlugins = new() { };

        public async Task RegisterPlugin(BasePlugin plugin) {
            try {
                if (LoadedPlugins.ContainsKey(plugin.Name)) {
                    throw new Exception("Plugin is already registered");
                }

                await plugin.Run();
                LoadedPlugins.Add(plugin.Name, plugin);
            } catch (Exception) {
                throw;
            }
        }

        public void UnregisterPlugin(string name) {
            if (!LoadedPlugins.ContainsKey(name)) {
                throw new Exception("Plugin is not registered");
            }

            LoadedPlugins[name].Unload();
            LoadedPlugins.Remove(name);
        }

        public void Dispose()
        {
            foreach (var (_, plugin) in LoadedPlugins)
            {
                plugin.UnloadHooks();
                plugin.Unload();
                LoadedPlugins.Remove(plugin.Name);
            }
        }
    }
    public abstract class BasePlugin
    {
        public virtual bool WaitForLoad { get; protected set; } = false;
        public string[] SupportedLanguages { get; }
        public string Name => GetType().Name;
        public bool IsEnabled { get; private set; } = false;
		protected static log4net.ILog Logger => ModContent.GetInstance<ExtraLanguage>().Logger;
		protected static LocalizationConfig cfg => ModContent.GetInstance<LocalizationConfig>();

		protected virtual bool CheckCondition() {
            if (SupportedLanguages != null) {
                if (SupportedLanguages.Length == 0) {
                    return true;
                }

                if (SupportedLanguages.Where(x => x == Language.ActiveCulture.Name).Any()) {
                    return true;
                }

                return false;
            }
            return true;
        }

        internal async Task Run() {
            if (CheckCondition()) {
                await Load();
                IsEnabled = true;
            }
        }

        internal abstract Task Load();
        internal virtual void LoadHooks() {}
        internal virtual void Unload() {}
        internal virtual void UnloadHooks() { }
    }
}