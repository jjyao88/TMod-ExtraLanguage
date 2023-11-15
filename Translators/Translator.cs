using System.Net.Http;
using System.Threading.Tasks;


namespace ExtraLanguage.Translators
{
    public interface ITranslator
    {
		protected readonly static HttpClient httpClient = new(handler: new HttpClientHandler()
        {
            UseCookies = false,
        });

        public Task<string> TranslateAsync(string text, string srcLang, string dstLang);
    }
}