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

namespace ExtraLanguage.Plugins
{
    public class S2TChineseConvert : BasePlugin
    {
        public new string[] SupportedLanguages => new string[] { "zh-Hant" };
        private LangExtractor langExtractor = new("zh-Hans");
        private readonly ITranslator Translator;
        private static readonly Regex cjkCharRegex = new(@"\p{IsCJKUnifiedIdeographs}");
        private bool ForceRecreateLocalization;

        internal readonly string zhHansDir;
		internal readonly string zhHantDir;

		public override bool WaitForLoad => true;

        public S2TChineseConvert(bool forceRecreateLocalization = false) {
            if (GameCulture.FromName("zh-Hant").Name != "zh-Hant")
                throw new Exception($"{nameof(S2TChineseConvert)} is only worked on zh-Hant");

            Translator = new FanhuajiTranslator();
            zhHansDir = Path.Combine(ExtraLanguage.LocalizationDir, "zh-Hans");
			zhHantDir = Path.Combine(ExtraLanguage.LocalizationDir, "zh-Hant");
            ForceRecreateLocalization = forceRecreateLocalization;
        }

        public static bool IsContainCJK(string text)
        {
			return cjkCharRegex.IsMatch(text);
		}

        internal override async Task Load()
        {
			if (ForceRecreateLocalization)
			{
				langExtractor.ExtractLocalization();

				foreach (var fileInfo in Utility.GetFullDirList(new DirectoryInfo(zhHansDir), "*.hjson", 5))
				{
					int startIdx = Path.GetFullPath(zhHansDir).Split(Path.DirectorySeparatorChar).Length - 1;
					var cnHjson = File.ReadAllText(fileInfo.FullName);

					if (!IsContainCJK(cnHjson))
					{
						continue;
					}

					var translatedText = await TranslateStrings(cnHjson, "zh-Hans", "zh-Hant");
					var splitPath = fileInfo.FullName.Split(Path.DirectorySeparatorChar);

					for (int i = startIdx; i < splitPath.Length; i++)
					{
						splitPath[i] = splitPath[i].Replace("zh-Hans", "zh-Hant");
					}

					var dstFilePath = Path.Combine(splitPath);
					Utility.CreateTextFile(dstFilePath, translatedText);
				}
			}

            ExtraLanguage.ModdedKeys["zh-Hant"] = LoadTranslation(zhHantDir);
			Utility.UpdateModdedLocalizedTexts();
		}

        internal List<(string,string)> LoadTranslation(string localizeDir)
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

        private async Task<string> TranslateStrings(string srcTxt, string srcLang, string dstLang) {
            var result = await Translator.TranslateAsync(srcTxt, srcLang, dstLang);
            return result;
		}
    }
}