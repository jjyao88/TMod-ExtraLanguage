using Terraria.ModLoader;
using Terraria.Localization;
using System.Reflection;
using System;
using Terraria;
using System.Collections.Generic;
using System.IO;
using ExtraLanguage.Plugins;
using System.Threading.Tasks;
using System.Linq;

namespace ExtraLanguage
{
	public partial class ExtraLanguage : Mod
	{
		internal static string MainDir = Path.Combine(Main.SavePath, nameof(ExtraLanguage));
		internal static string LocalizationDir = Path.Combine(MainDir, "Localization");
		internal static Dictionary<string, List<(string, string)>> ModdedKeys;
		private static PluginManager PluginManager;
		private readonly static string[] IncompatibleModNames = new string[] {
			"ChineseLocalization" // Steam Workshop ID: 2800050107
		};


		public override void Load()
		{
            foreach (Mod mod in ModLoader.Mods)
            {
				if (IncompatibleModNames.Contains(mod.Name))
				{
					throw new Exception($"Incompatible mod detected: {mod.Name}. Please unload it first before enabling this mod!");
				}
			}

			ModdedKeys = new Dictionary<string, List<(string, string)>>();
			PluginManager = new PluginManager();

			foreach (CustomCulture lang in SupportedLanguages)
			{
				ModdedKeys.Add(lang.CultureName, new List<(string, string)>());
			}

			Directory.CreateDirectory(MainDir);
			Directory.CreateDirectory(LocalizationDir);		

			var namedCulturesFieldInfo = typeof(GameCulture).GetField("_NamedCultures", BindingFlags.Static | BindingFlags.NonPublic);
			var namedCultures = (Dictionary<GameCulture.CultureName, GameCulture>)namedCulturesFieldInfo.GetValue(null);

			foreach (CustomCulture lang in SupportedLanguages)
			{
				var culture = GameCulture.FromName(lang.CultureName);

				if (!Directory.Exists(Path.Combine(LocalizationDir, lang.CultureName)))
				{
					Directory.CreateDirectory(Path.Combine(LocalizationDir, lang.CultureName));
				}

				// When the culture doesn't exist, it will be returned English culture instead.
				if (culture.LegacyId == 1)
				{
					culture = new GameCulture(lang.CultureName, lang.LegacyId);
				}
				else
				{
					Logger.Debug($"{lang.DisplayName} already exists, skipping");
				}
				namedCultures.Add((GameCulture.CultureName)lang.LegacyId, culture);
			}
			namedCulturesFieldInfo.SetValue(null, namedCultures);

			Logger.Info("Loaded Extra Language Support!");
			LoadHooks();
		}

		public override void Unload()
		{
			UnloadHooks();

			var dict = new Dictionary<GameCulture.CultureName, GameCulture>();
			for (int i = 0; i < 10; i++)
			{
				dict.Add((GameCulture.CultureName)i, GameCulture.FromLegacyId(i));
			}

			var namedCulturesFieldInfo = typeof(GameCulture).GetField("_NamedCultures", BindingFlags.Static | BindingFlags.NonPublic);
			namedCulturesFieldInfo.SetValue(null, dict);

			PluginManager = null;
			ModdedKeys = null;
		}

		public override void PostSetupContent()
		{
			var cfg = ModContent.GetInstance<LocalizationConfig>();

			if (string.IsNullOrEmpty(cfg.LanguageName))
			{
				LocalizationConfig.UpdateLanguageName(Language.ActiveCulture.Name);
			}

			if (Language.ActiveCulture.Name != GameCulture.FromName(cfg.LanguageName).Name)
			{
				LanguageManager.Instance.SetLanguage(cfg.LanguageName);
			}

			List<BasePlugin> plugins = new();

			if (cfg.UseFanhuaji && Language.ActiveCulture.Name == "zh-Hant")
			{
				plugins.Add(new S2TChineseConvert(forceRecreateLocalization: true));
			}

			foreach (var plugin in plugins)
			{
				var task = PluginManager.RegisterPlugin(plugin);
				if (plugin.WaitForLoad)
				{
					task.Wait();
				}
				Logger.Info($"Plugin '{plugin.Name}' is loaded");
			}

			Main.QueueMainThreadAction(Main.AssetSourceController.Refresh);
		}
	}
}