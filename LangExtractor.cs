using Terraria.ModLoader;
using Terraria.Localization;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System;

namespace ExtraLanguage
{
	internal class LangExtractor
	{
        private readonly GameCulture TargetCulture;

		private static List<string> BlackListMods = new()
		{
            nameof(ExtraLanguage),
            "ModLoader",
        };

		private string OutLangDir;

		internal LangExtractor(string targetLang) {
            TargetCulture = GameCulture.FromName(targetLang);
			OutLangDir = Path.Combine(ExtraLanguage.LocalizationDir, TargetCulture.Name);
		}

		internal void ExtractLocalization() {
            MethodInfo updateLocalizationMethod = typeof(LocalizationLoader).GetMethod("UpdateLocalizationFilesForMod", BindingFlags.Static | BindingFlags.NonPublic);

			if (Directory.Exists(OutLangDir))
			{
				Directory.Delete(OutLangDir, recursive: true);
			}
			Directory.CreateDirectory(OutLangDir);

			foreach (var mod in ModLoader.Mods)
			{
				if (BlackListMods.Contains(mod.Name))
				{
					continue;
				}

				var outModPath = Path.Combine(OutLangDir, mod.Name);
				Directory.CreateDirectory(outModPath);

				try
				{
					updateLocalizationMethod.Invoke(null, new object[] { mod, outModPath, TargetCulture });

				}
				catch (Exception e)
				{
					throw new Exception($"Failed to extract localization for {mod.Name}", e);
				}
			}
		}
	}
}