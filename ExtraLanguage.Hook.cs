using Terraria.ModLoader;
using Terraria.Localization;
using System.Reflection;
using System;
using Terraria;
using System.Collections.Generic;
using MonoMod.Cil;
using System.IO;
using ReLogic.Content.Sources;
using System.Linq;

namespace ExtraLanguage
{
	public partial class ExtraLanguage : Mod
	{
		private void LoadHooks() {
            IL_Main.DrawMenu += HookLanguageSelection;
			LanguageManager.Instance.OnLanguageChanged += HookOnLanguageChanged;
			On_LanguageManager.LoadActiveCultureTranslationsFromSources += HookLoadActiveCultureTranslationsFromSources;
			On_LanguageManager.LoadFilesForCulture += HookLoadFilesForCulture;
		}

        private void UnloadHooks() {
            LanguageManager.Instance.OnLanguageChanged -= HookOnLanguageChanged;
        }

		private void HookLoadFilesForCulture(On_LanguageManager.orig_LoadFilesForCulture orig, LanguageManager self, GameCulture culture)
		{
			orig.Invoke(self, culture);

			Utility.UpdateModdedLocalizedTexts();
		}

		private void HookLoadActiveCultureTranslationsFromSources(On_LanguageManager.orig_LoadActiveCultureTranslationsFromSources orig, LanguageManager self)
        {
            // Workaround for loading language pack properly
            // Detail: https://github.com/tModLoader/tModLoader/pull/2617
			var contentSources = self.GetType().GetField("_contentSources", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(self) as IContentSource[];
			string assetNameStart = string.Concat(str2: self.ActiveCulture.Name, str0: "Localization", str1: Path.DirectorySeparatorChar.ToString()).ToLower();

			IContentSource[] array = contentSources;
			foreach (IContentSource item in array)
			{
				foreach (string item2 in GetAllAssetsStartingWith(item, assetNameStart))
				{

					string extension = Path.GetExtension(item2);

					if (extension != ".json" && extension != ".csv")
					{
						continue;
					}
					
					using Stream stream = item.OpenStream(item2);
					using StreamReader streamReader = new StreamReader(stream);
					string fileText = streamReader.ReadToEnd();

					if (extension == ".json")
					{
						self.LoadLanguageFromFileTextJson(fileText, canCreateCategories: false);
					}
					if (extension == ".csv")
					{
						self.LoadLanguageFromFileTextCsv(fileText);
					}
				}
			}
        }

		private void HookOnLanguageChanged(LanguageManager langMgr)
		{
			var cfg = ModContent.GetInstance<LocalizationConfig>();
			Logger.Info($"Config Game Language: {cfg.LanguageName} => {langMgr.ActiveCulture.CultureInfo.Name}");
			LocalizationConfig.UpdateLanguageName(langMgr.ActiveCulture.CultureInfo.Name);
		}

        private static void HookLanguageSelection(ILContext il)
		{
			try
			{
				// Main.menuMode == 1212
				var iLCursor = new ILCursor(il);
				iLCursor.GotoNext(i => i.MatchLdstr("Language.Polish"));
				iLCursor.GotoNext(i => i.MatchStelemRef());
				iLCursor.Index += 1;

				// 497: Add Language.ChineseTraditional to the list of languages
				foreach (var item in SupportedLanguages)
				{
					iLCursor.Emit(Mono.Cecil.Cil.OpCodes.Ldloc_S, (byte)26);
					iLCursor.Emit(Mono.Cecil.Cil.OpCodes.Ldc_I4_S, (sbyte)item.LegacyId);
					iLCursor.Emit(Mono.Cecil.Cil.OpCodes.Ldstr, item.DisplayName);
					// iLCursor.Emit(Mono.Cecil.Cil.OpCodes.Call, typeof(Language).GetMethod("GetTextValue", new Type[] { typeof(string) }));
					iLCursor.Emit(Mono.Cecil.Cil.OpCodes.Stelem_Ref);
				}

				// 498: Replace numButtons = 10 => numButtons = 10 + SupportedLanguages.Count
				iLCursor.Remove();
				iLCursor.Emit(Mono.Cecil.Cil.OpCodes.Ldc_I4_S, (sbyte)(10 + SupportedLanguages.Count));

				// Main.menuMode == 1213
				iLCursor.GotoNext(i => i.MatchLdcI4(1213));
				iLCursor.GotoNext(i => i.MatchLdstr("Language.Polish"));
				iLCursor.Index += 3;

				// 525: Add Language.ChineseTraditional to the list of languages
				foreach (var item in SupportedLanguages)
				{
					iLCursor.Emit(Mono.Cecil.Cil.OpCodes.Ldloc_S, (byte)26);
					iLCursor.Emit(Mono.Cecil.Cil.OpCodes.Ldc_I4_S, (sbyte)item.LegacyId);
					iLCursor.Emit(Mono.Cecil.Cil.OpCodes.Ldstr, item.DisplayName);
					// iLCursor.Emit(Mono.Cecil.Cil.OpCodes.Call, typeof(Language).GetMethod("GetTextValue", new Type[] { typeof(string) }));
					iLCursor.Emit(Mono.Cecil.Cil.OpCodes.Stelem_Ref);
				}

				// 526: Replace array9[10] = Lang.menu[5].Value with array9[10 + SupportedLanguages.Count]
				iLCursor.GotoNext(i => i.MatchLdcI4(10));
				iLCursor.Remove();
				iLCursor.Emit(Mono.Cecil.Cil.OpCodes.Ldc_I4_S, (sbyte)(10 + SupportedLanguages.Count));

				// 527: Replace numButtons = 11 => numButtons = 11 + SupportedLanguages.Count
				iLCursor.GotoNext(i => i.MatchLdcI4(11));
				iLCursor.Remove();
				iLCursor.Emit(Mono.Cecil.Cil.OpCodes.Ldc_I4_S, (sbyte)(11 + SupportedLanguages.Count));

				// 528: if (this.selectedMenu == 10 || backButtonDown) => if (this.selectedMenu == (10 + SupportedLanguages.Count) || backButtonDown)
				iLCursor.GotoNext(i => i.MatchLdfld("Terraria.Main", "selectedMenu"));
				iLCursor.Index += 1;
				iLCursor.Remove();
				iLCursor.Emit(Mono.Cecil.Cil.OpCodes.Ldc_I4_S, (sbyte)(10 + SupportedLanguages.Count));

				// 544: array4[10] = 10 => array4[10 + SupportedLanguages.Count] = 10
				iLCursor.GotoNext(i => i.MatchLdloc(19) && i.Next.MatchLdcI4(10));
				iLCursor.Index += 1;
				iLCursor.Remove();
				iLCursor.Emit(Mono.Cecil.Cil.OpCodes.Ldc_I4_S, (sbyte)(10 + SupportedLanguages.Count));


				// 550: array7[10] = 0.95f => array7[10 + SupportedLanguages.Count] = 0.95f
				iLCursor.GotoNext(i => i.MatchLdcR4(0.95f));
				iLCursor.Index -= 1;
				iLCursor.Remove();
				iLCursor.Emit(Mono.Cecil.Cil.OpCodes.Ldc_I4_S, (sbyte)(10 + SupportedLanguages.Count));
			}
			catch (Exception e)
			{
				MonoModHooks.DumpIL(ModContent.GetInstance<ExtraLanguage>(), il);
				throw new ILPatchFailureException(ModContent.GetInstance<ExtraLanguage>(), il, e);
			}
		}

		private static IEnumerable<string> GetAllAssetsStartingWith(IContentSource self, string assetNameStart)
		{
			return self.EnumerateAssets().Where(s => s.ToLower().StartsWith(assetNameStart));
		}
	}
}