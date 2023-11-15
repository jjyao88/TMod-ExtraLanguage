using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Terraria.Localization;

namespace ExtraLanguage
{
	public class Utility
	{
		public static void CreateDictionaryIfNotExist(string path)
		{
			if (!Directory.Exists(Path.GetDirectoryName(path)))
			{
				Directory.CreateDirectory(path);
			}
		}

		public static void CreateTextFile(string filePath, string content)
		{
			var dir = Path.GetDirectoryName(filePath);
			if (!Directory.Exists(dir))
			{
				Directory.CreateDirectory(dir);
			}
			File.WriteAllText(filePath, content);
		}

		public static IEnumerable<FileInfo> GetFullDirList(DirectoryInfo dir, string searchPattern, int depth)
		{
			foreach (FileInfo file in dir.GetFiles(searchPattern))
			{
				yield return file;
			}

			if (depth > 0)
			{
				foreach (DirectoryInfo d in dir.GetDirectories())
				{
					foreach (FileInfo f in GetFullDirList(d, searchPattern, depth - 1))
					{
						yield return f;
					}
				}
			}
		}

		internal static void UpdateModdedLocalizedTexts()
		{
			string cultureName = LanguageManager.Instance.ActiveCulture.Name;
			if (!ExtraLanguage.ModdedKeys.ContainsKey(cultureName))
			{
				return;
			}

			MethodInfo LocalizedText_SetValue = typeof(LocalizedText).GetMethod("SetValue", BindingFlags.NonPublic | BindingFlags.Instance);

			foreach (var (key, val) in ExtraLanguage.ModdedKeys[cultureName])
			{
				LocalizedText txt = Language.GetText(key);
				LocalizedText_SetValue.Invoke(txt, new object[] { val });
			}
		}
	}
}
