using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Terraria.ModLoader;

namespace ExtraLanguage.Translators
{
    public class FanhuajiTranslator : ITranslator
    {
        public struct APIResponse
        {
            public struct Data
            {
                public string converter;
                public string text;
            }

            public Data data;
            public int code;
            public string msg;
            public float execTime;
        }

        public async Task<string> TranslateAsync(string text, string srcLang, string dstLang)
        {
            var cfg = ModContent.GetInstance<LocalizationConfig>();

            var content = new MultipartFormDataContent
            {
                { new StringContent(text), "text" },
                { new StringContent("Taiwan"), "converter" },
            };

            if (cfg != null && cfg.FanhuajiUserPostItems.Count > 0)
            {
                content.Add(new StringContent(String.Join('\n', cfg.FanhuajiUserPostItems)), "userPostReplace");
            }

            using var resp = await ITranslator.httpClient.PostAsync(
                "https://api.zhconvert.org/convert",
                content
            );

            if (!resp.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to translate: {resp.StatusCode}");
            }

            var apiResp = JsonConvert.DeserializeObject<APIResponse>(await resp.Content.ReadAsStringAsync());

            return apiResp.data.text;
        }
    }
}
