using Terraria.ModLoader;
using Terraria.Localization;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System;
using rail;
using System.Text.RegularExpressions;
using Terraria.ModLoader.Core;
using System.Linq;

namespace ExtraLanguage
{
	internal class LangExtractor
	{
		private readonly GameCulture TargetCulture;

		// Arguments: (Mod mod, string outputPath = null, GameCulture specificCulture = null)
		private static readonly MethodInfo LocalizationLoader_UpdateLocalizationFilesForMod = typeof(LocalizationLoader).GetMethod("UpdateLocalizationFilesForMod", BindingFlags.Static | BindingFlags.NonPublic);

		private static readonly PropertyInfo Mod_File = typeof(Mod).GetProperty("File", BindingFlags.NonPublic | BindingFlags.Instance);

		public static List<string> BlackListMods = new()
		{
			nameof(ExtraLanguage),
			"ModLoader",
		};

		internal static string ExtractLocalization(string modName, string cultureName, string outDir)
		{
			var targetCulture = GameCulture.FromName(cultureName);

			if (targetCulture.Name != cultureName)
			{
				throw new KeyNotFoundException($"Culture '{cultureName}' is not found in game!");
			}

			Utility.DeleteSubdirectories(outDir);
			Directory.CreateDirectory(outDir);

			Regex matchLocaleRegex = new(@$"{cultureName}.*(\.hjson$)");

			var mod = ModLoader.GetMod(modName);
			TmodFile tmodFile = (TmodFile)Mod_File.GetValue(mod);
			List<TmodFile.FileEntry> localeFiles = tmodFile.Where(x => matchLocaleRegex.IsMatch(x.Name)).ToList();

			foreach (var entry in localeFiles)
			{
				var stream = tmodFile.GetStream(entry.Name);
				if (stream == null)
				{
					continue;
				}
				using var reader = new StreamReader(stream);

				var fileText = reader.ReadToEnd();
				Utility.CreateTextFile(Path.Combine(outDir, entry.Name), fileText);
			}
			return outDir;
		}

		internal static string ExtractLocalization(string modName, string cultureName)
		{
			var targetCulture = GameCulture.FromName(cultureName);

			if (targetCulture.Name != cultureName)
			{
				throw new KeyNotFoundException($"Culture '{cultureName}' is not found in game!");
			}

			var outLangDir = Path.Combine(ExtraLanguage.LocalizationDir, cultureName, modName);
			Utility.DeleteSubdirectories(outLangDir);
			Directory.CreateDirectory(outLangDir);

			Regex matchLocaleRegex = new(@$"{cultureName}.*(\.hjson$)");

			var mod = ModLoader.GetMod(modName);

			TmodFile tmodFile = (TmodFile)Mod_File.GetValue(mod);
			List<TmodFile.FileEntry> localeFiles = tmodFile.Where(x => matchLocaleRegex.IsMatch(x.Name)).ToList();

			foreach (var entry in localeFiles)
			{
				var stream = tmodFile.GetStream(entry.Name);
				if (stream == null)
				{
					continue;
				}
				using var reader = new StreamReader(stream);

				var fileText = reader.ReadToEnd();
				Utility.CreateTextFile(Path.Combine(outLangDir, entry.Name), fileText);
			}

			return outLangDir;
		}

		internal static string ExtractLocalization(string cultureName)
		{
			var targetCulture = GameCulture.FromName(cultureName);

			if (targetCulture.Name != cultureName)
			{
				throw new KeyNotFoundException($"Culture '{cultureName}' is not found in game!");
			}

			var outLangDir = Path.Combine(ExtraLanguage.LocalizationDir, cultureName);

			if (Directory.Exists(outLangDir))
			{
				Directory.Delete(outLangDir, recursive: true);
			}

			Directory.CreateDirectory(outLangDir);

			foreach (var mod in ModLoader.Mods)
			{
				if (BlackListMods.Contains(mod.Name))
				{
					continue;
				}

				var outModPath = Path.Combine(outLangDir, mod.Name);
				Directory.CreateDirectory(outModPath);

				Regex matchLocaleRegex = new(@$"{cultureName}.*(\.hjson$)");

				TmodFile tmodFile = (TmodFile)Mod_File.GetValue(mod);
				List<TmodFile.FileEntry> localeFiles = tmodFile.Where(x => matchLocaleRegex.IsMatch(x.Name)).ToList();

				foreach (var entry in localeFiles)
				{
					var stream = tmodFile.GetStream(entry.Name);
					if (stream == null)
					{
						continue;
					}
					using var reader = new StreamReader(stream);

					var fileText = reader.ReadToEnd();
					Utility.CreateTextFile(Path.Combine(outLangDir, entry.Name), fileText);
				}
			}
			return outLangDir;
		}
	}
}