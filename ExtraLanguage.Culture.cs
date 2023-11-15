using System;
using System.Collections.Generic;

namespace ExtraLanguage
{
	public partial class ExtraLanguage
	{
		// In Terraria.Main, the maxMenuItems is only 16, thus we can't add more than 6 custom languages.
		internal static HashSet<CustomCulture> SupportedLanguages { get; } = new()
		{
			{ new CustomCulture(CultureID.ChineseTraditional, "zh-Hant", "正體中文 (Traditional Chinese)") },
		};
	}

	public enum CultureID
	{
		ChineseTraditional = 10,
	}

	public readonly struct CustomCulture
	{
		public readonly CultureID ID;
		public readonly string CultureName;
		public readonly string DisplayName;

		public CustomCulture(CultureID id, string cultureName, string displayName)
		{
			ID = id;
			CultureName = cultureName;
			DisplayName = displayName;

		}

		public int LegacyId => (int)ID;

		public override string ToString()
		{
			return $"{DisplayName} ({CultureName})";
		}
	}

}
