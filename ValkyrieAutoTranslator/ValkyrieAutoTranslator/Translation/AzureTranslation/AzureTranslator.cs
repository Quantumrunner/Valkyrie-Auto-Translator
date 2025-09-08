using Newtonsoft.Json;
using System.Text;
using Valkyrie.AutoTranslator.Helpers;

namespace Valkyrie.AutoTranslator.AzureTranslation
{
    internal class AzureTranslator
    {
        private static string host = "https://api.cognitive.microsofttranslator.com";
        private static string path = "/translate?api-version=3.0";

        // Translate to German
        //private static string queryparameter = "&from=en&to=de&textType=html&category={1}";
        private static string queryparameter = "&from={0}&to={1}&textType=html";

        private static string uri;

        public static async Task<string> Translate(string text, HashSet<KeyValuePair<string, string>> translationCache, string sourceLanguage, string targetLanguage, string azureKey, string azureCategoryId)
        {
            queryparameter = string.Format(queryparameter, sourceLanguage, targetLanguage, azureCategoryId);
            uri = host + path + queryparameter;

            object[] body = new object[1]
            {
                new
                {
                    Text = text
                }
            };
            string requestBody = JsonConvert.SerializeObject(body);
            using (HttpClient client = new HttpClient())
            {
                using (HttpRequestMessage request = new HttpRequestMessage())
                {
                    request.Method = HttpMethod.Post;
                    request.RequestUri = new Uri(uri);
                    request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                    request.Headers.Add("Ocp-Apim-Subscription-Key", azureKey);
                    request.Headers.Add("Ocp-Apim-Subscription-Region", "westeurope");
                    string responseBody = await (await client.SendAsync(request)).Content.ReadAsStringAsync();
                    if (responseBody.Contains("{\"error\":"))
                    {
                        AutoTranslatorLogger.Info(responseBody);
                        return text;
                    }
                    AzureTranslationObject objData = JsonConvert.DeserializeObject<List<AzureTranslationObject>>(responseBody).FirstOrDefault();
                    string translatedText = objData.translations.FirstOrDefault().text;
                    translationCache.Add(new KeyValuePair<string, string>(text, translatedText));
                    return translatedText;
                }
            }
        }
    }
}
