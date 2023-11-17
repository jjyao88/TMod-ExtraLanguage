using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ExtraLanguage.Translators;
using Terraria.Localization;
using Newtonsoft.Json.Linq;
using Hjson;
using System.Collections.Generic;
using Terraria.ModLoader;
using System.Linq;

namespace ExtraLanguage.Plugins
{
	public class S2TChineseConvert : BasePlugin
	{
		public new string[] SupportedLanguages => new string[] { "zh-Hant" };
		private readonly ITranslator Translator;
		private static readonly Regex cjkCharRegex = new(@"\p{IsCJKUnifiedIdeographs}");
		private bool ForceRecreateLocalization;

		internal readonly string zhHansDir;
		internal readonly string zhHantDir;

		public override bool WaitForLoad => true;

		public S2TChineseConvert(bool forceRecreateLocalization = false)
		{
			if (GameCulture.FromName("zh-Hant").Name != "zh-Hant")
				throw new Exception($"{nameof(S2TChineseConvert)} is only worked on zh-Hant");

			Translator = new FanhuajiTranslator();
			zhHansDir = Path.Combine(ExtraLanguage.LocalizationDir, "zh-Hans");
			zhHantDir = Path.Combine(ExtraLanguage.LocalizationDir, "zh-Hant");
			ForceRecreateLocalization = forceRecreateLocalization;
		}

		protected override bool CheckCondition()
		{
			if (!cfg.UseFanhuaji)
				return false;

			return base.CheckCondition();
		}

		public static bool IsContainCJK(string text)
		{
			return cjkCharRegex.IsMatch(text);
		}

		public static bool IsModContainCJK(string modName)
		{

			ModLoader.TryGetMod(modName, out Mod result);
			if (result == null)
			{
				return false;
			}

			var tempDir = Utility.GetTempDirectory();
			LangExtractor.ExtractLocalization(modName, "zh-Hans", tempDir);
			foreach (var fileInfo in Utility.GetFullDirList(new DirectoryInfo(tempDir), "*.hjson", 5))
			{
				var cnHjson = File.ReadAllText(fileInfo.FullName);

				if (IsContainCJK(cnHjson))
				{
					return true;
				}
			}
			return false;
		}

		private static Dictionary<string, Version> GenerateModVeriosnList()
		{
			var dict = new Dictionary<string, Version>();
			foreach (Mod mod in ModLoader.Mods)
			{
				dict.Add(mod.Name, mod.Version);
			}
			return dict;
		}

		internal override async Task Load()
		{
			string modJsonPath = Path.Combine(zhHantDir, "mods.json");
			bool createFlag = ForceRecreateLocalization || !File.Exists(modJsonPath);

			if (createFlag)
			{
				if (Directory.Exists(zhHantDir))
				{
					Directory.Delete(zhHantDir, recursive: true);
				}
			}

			Directory.CreateDirectory(zhHansDir);
			Directory.CreateDirectory(zhHantDir);

			// Load old mod version list
			Dictionary<string, Version> oldModVersionDict = new();
			if (File.Exists(modJsonPath))
			{
				try
				{
					var oldModVersionJson = File.ReadAllText(Path.Combine(zhHantDir, "mods.json"));
					oldModVersionDict = JObject.Parse(oldModVersionJson).ToObject<Dictionary<string, Version>>();
				}
				catch
				{
					Logger.Error("Failed to load old mod version list, recreating localization files");
				}
			}

			// Generate current mod version list
			Dictionary<string, Version> modVersionDict = new();
			foreach (Mod mod in ModLoader.Mods)
			{
				if (LangExtractor.BlackListMods.Where(x => x == mod.Name).Any())
				{
					continue;
				}

				// TODO: should put all temp localization files into a single temp directory
				if (IsModContainCJK(mod.Name))
				{
					modVersionDict.Add(mod.Name, mod.Version);
				}
			}

			foreach (var (name, version) in modVersionDict)
			{
				if (!oldModVersionDict.ContainsKey(name) || oldModVersionDict[name] != version)
				{
					if (oldModVersionDict.ContainsKey(name))
					{
						Logger.Info($"Mod {name} version changed from {oldModVersionDict[name]} to {version}, recreating localization files");
					}
					else
					{
						Logger.Info($"Mod {name} is new, creating localization files");
					}

					// Remove old localization files
					Utility.DeleteSubdirectories(Path.Combine(zhHantDir, name));

					var cnLocalDir = LangExtractor.ExtractLocalization(name, "zh-Hans");

					foreach (var fileInfo in Utility.GetFullDirList(new DirectoryInfo(cnLocalDir), "*.hjson", 5))
					{
						int startIdx = Path.GetFullPath(ExtraLanguage.LocalizationDir).Split(Path.DirectorySeparatorChar).Length;
						var cnHjson = File.ReadAllText(fileInfo.FullName);

						var translatedText = await TranslateStrings(cnHjson, "zh-Hans", "zh-Hant");
						var splitPath = fileInfo.FullName.Split(Path.DirectorySeparatorChar);

						for (int i = startIdx; i < splitPath.Length; i++)
						{
							splitPath[i] = splitPath[i].Replace("zh-Hans", "zh-Hant");
						}

						var dstFilePath = Path.Combine(splitPath);
						Utility.CreateTextFile(dstFilePath, translatedText);
					}
					oldModVersionDict[name] = version;
				}
			}

			Utility.CreateTextFile(Path.Combine(zhHantDir, "mods.json"), JObject.FromObject(oldModVersionDict).ToString());

			ExtraLanguage.ModdedKeys["zh-Hant"] = LoadTranslation(zhHantDir);
			Utility.UpdateModdedLocalizedTexts();

			Directory.Delete(zhHansDir, recursive: true);
		}

		internal List<(string, string)> LoadTranslation(string localizeDir)
		{
			var totalKeysAndValues = new List<(string, string)>();

			foreach (var fileInfo in Utility.GetFullDirList(new DirectoryInfo(localizeDir), "*.hjson", 5))
			{
				int startIdx = Path.GetFullPath(localizeDir).Split(Path.DirectorySeparatorChar).Length + 1;
				var splitPath = fileInfo.FullName.Split(Path.DirectorySeparatorChar);
				var shortPath = Path.Combine(splitPath[startIdx..^0]).Replace(Path.DirectorySeparatorChar, '/');

				var (_, prefix) = LocalizationLoader.GetCultureAndPrefixFromPath(shortPath);

				var jsonTxt = HjsonValue.Load(fileInfo.FullName).ToString();
				var keyList = LoadModTranslation(jsonTxt, prefix);

				totalKeysAndValues.AddRange(keyList);
			}

			return totalKeysAndValues;
		}

		private static List<(string, string)> LoadModTranslation(string jsonTxt, string modPrefix)
		{
			var list = new List<(string, string)>();

			foreach (JToken item3 in JObject.Parse(jsonTxt).SelectTokens("$..*"))
			{
				if (item3.HasValues)
				{
					continue;
				}

				JObject jObject = item3 as JObject;
				if (jObject != null && jObject.Count == 0)
				{
					continue;
				}

				string text3 = "";
				JToken item = item3;
				for (JToken parent = item3.Parent; parent != null; parent = parent.Parent)
				{
					JProperty jProperty = parent as JProperty;
					string text4;
					if (jProperty == null)
					{
						JArray jArray = parent as JArray;
						text4 = (jArray == null) ? text3 : (jArray.IndexOf(item) + ((text3 == string.Empty) ? string.Empty : ("." + text3)));
					}
					else
					{
						text4 = jProperty.Name + ((text3 == string.Empty) ? string.Empty : ("." + text3));
					}

					text3 = text4;
					item = parent;
				}

				text3 = text3.Replace(".$parentVal", "");
				if (!string.IsNullOrWhiteSpace(modPrefix))
				{
					text3 = modPrefix + "." + text3;
				}

				list.Add((text3, item3.ToString()));
			}
			return list;
		}

		private async Task<string> TranslateStrings(string srcTxt, string srcLang, string dstLang)
		{
			var result = await Translator.TranslateAsync(srcTxt, srcLang, dstLang);
			return result;
		}
	}
}