using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text.Json.Serialization;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace ExtraLanguage
{

	public class LocalizationConfig : ModConfig
	{
		public override ConfigScope Mode => ConfigScope.ClientSide;

		[Header("$Mods.ExtraLanguage.Configs.LocalizationConfig.DeveloperOnlyHeader")]
		public string LanguageName { 
			get{
				try
				{
					using var file = File.OpenText(Path.Combine(ExtraLanguage.MainDir, "lang.txt"));
					return file.ReadLine().Trim();

				}
				catch (Exception e)
				{
					if (e is FileNotFoundException || e is DirectoryNotFoundException)
						return Language.ActiveCulture.Name;
					throw;
				}
			}
		}

		public int LoadedLocalizationKeys
		{
			get
			{
				if (ExtraLanguage.ModdedKeys == null || !ExtraLanguage.ModdedKeys.ContainsKey(Language.ActiveCulture.Name))
					return 0;
				return ExtraLanguage.ModdedKeys[Language.ActiveCulture.Name].Count;
			}
		}

		#region Fanhuaji
		[Header("$Mods.ExtraLanguage.Configs.LocalizationConfig.TChineseHeader")]
		[DefaultValue(false)]
		[ReloadRequired]
		public bool UseFanhuaji;

		[ReloadRequired]
		public HashSet<string> FanhuajiUserPostItems = new()
		{
			"漢化=中文化",
			"屏幕=螢幕",
			"界面=介面",
			"剪貼板=剪貼簿",
			"默認=預設",
		};
		#endregion


		internal static void UpdateLanguageName(string lang)
		{
			using var file = File.CreateText(Path.Combine(ExtraLanguage.MainDir, "lang.txt"));
			file.WriteLine(lang);
		}
		

		internal static void SaveConfig()
		{
			// in-game ModConfig saving from mod code is not supported yet in tmodloader, and subject to change, so we need to be extra careful.
			// This code only supports client configs, and doesn't call onchanged. It also doesn't support ReloadRequired or anything else.
			MethodInfo saveMethodInfo = typeof(ConfigManager).GetMethod("Save", BindingFlags.Static | BindingFlags.NonPublic);
			if (saveMethodInfo != null)
				saveMethodInfo.Invoke(null, new object[] { ModContent.GetInstance<LocalizationConfig>() });
			else
				ModContent.GetInstance<ExtraLanguage>().Logger.Warn("In-game SaveConfig failed, code update required");
		}
	}
}
