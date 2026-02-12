using Newtonsoft.Json.Linq;
using Valkyrie.AutoTranslator.Helpers;

namespace Valkyrie.AutoTranslator
{
    internal static class DeepLTranslator
    {
        internal const char SpecialGlossaryChar = '␣';

        public static async Task<Tuple<string, bool>> Translate(string deepLApiMode,string key, string text, string sourceLang, string targetLang, string apiKey, string glossaryId = null, string deepLContextDefault = null, string deepLContextActivation = null, string deepLFormality = null)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"DeepL-Auth-Key {apiKey}");

                int maxRetries = 5;
                int delayMs = 1000;
                for (int attempt = 0; attempt < maxRetries; attempt++)
                {
                    try
                    {
                        var keyValuePairs = new List<KeyValuePair<string, string>>
                        {
                            new KeyValuePair<string, string>("text", text),
                            new KeyValuePair<string, string>("source_lang", sourceLang.ToUpper()),
                            new KeyValuePair<string, string>("target_lang", targetLang.ToUpper()),
                            new KeyValuePair<string, string>("tag_handling", "xml"),
                            new KeyValuePair<string, string>("ignore_tags", "keep"),
                            new KeyValuePair<string, string>("model_type", "quality_optimized"),
                        };
                        if (!string.IsNullOrEmpty(glossaryId))
                        {
                            keyValuePairs.Add(new KeyValuePair<string, string>("glossary_id", glossaryId));
                        }
                        if (!string.IsNullOrEmpty(deepLFormality))
                        {
                            keyValuePairs.Add(new KeyValuePair<string, string>("formality", deepLFormality));
                        }

                        if (key.StartsWith("Activation") && !string.IsNullOrEmpty(deepLContextActivation))
                        {
                            keyValuePairs.Add(new KeyValuePair<string, string>("context", deepLContextActivation));
                        }
                        else if (!string.IsNullOrEmpty(deepLContextDefault))
                        {
                            keyValuePairs.Add(new KeyValuePair<string, string>("context", deepLContextDefault));
                        }

                        using (var content = new FormUrlEncodedContent(keyValuePairs))
                        {
                            string apiUrl = deepLApiMode == "paid" ? "https://api.deepl.com/v2/translate" : "https://api-free.deepl.com/v2/translate";
                            var response = await client.PostAsync(apiUrl, content);

                            if ((int)response.StatusCode == 429 || (int)response.StatusCode == 503)
                            {
                                await Task.Delay(delayMs);
                                delayMs *= 2;
                                continue;
                            }
                            if ((int)response.StatusCode == 456)
                            {
                                string limitReachedError = "DeepL API limit reached.";
                                AutoTranslatorLogger.Error(limitReachedError);
                                return new Tuple<string, bool>(text, true);
                            }

                            response.EnsureSuccessStatusCode();
                            if (!response.IsSuccessStatusCode)
                            {
                                var errorContent = await response.Content.ReadAsStringAsync();
                                AutoTranslatorLogger.Error($"DeepL API error: {response.StatusCode} - {errorContent}");
                                return new Tuple<string, bool>(text, true);
                            }
                            response.EnsureSuccessStatusCode();
                            var json = await response.Content.ReadAsStringAsync();
                            var result = JObject.Parse(json);
                            var translatedText = result["translations"]?[0]?["text"]?.ToString() ?? text;
                            return new Tuple<string, bool>(translatedText, false);
                        }
                    }
                    catch(Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
                    {
                        if (attempt == maxRetries - 1)
                        {
                            throw;
                        }
                        await Task.Delay(delayMs);
                        delayMs *= 2;
                    }
                    catch (Exception ex)
                    {
                        AutoTranslatorLogger.Error($"Unexpected error during DeepL translation: {ex.Message}");
                        return new Tuple<string, bool>(text, true);
                    }
                }

                string errorMessage = $"DeepL API throttling: Max retry attempts exceeded for text {text}.";
				AutoTranslatorLogger.Error(errorMessage);
				return new Tuple<string, bool>(text, true);
            }
        }

        // Helper to encode leading/trailing whitespace
        private static string EncodeWhitespace(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            // Replace leading and trailing spaces with a visible marker (e.g., ␣)
            int leading = input.TakeWhile(char.IsWhiteSpace).Count();
            int trailing = input.Reverse().TakeWhile(char.IsWhiteSpace).Count();
            string encoded = input.Trim();
            if (leading > 0) encoded = new string(SpecialGlossaryChar, leading) + encoded;
            if (trailing > 0) encoded += new string(SpecialGlossaryChar, trailing);
            return encoded;
        }

        public static async Task<string> UpdateGlossaryAsync(string apiKey, bool deleteExistingGlossaries, string sourceLang, string targetLang, List<KeyValuePair<string, string>> glossaryEntries)
        {
            int glossaryCount = glossaryEntries.Count;
            AutoTranslatorLogger.Info($"Starting DeepL glossary update with glossary of {glossaryCount} entries");

            // Encode whitespace in keys and values
            var encodedEntries = glossaryEntries.Select(e =>
                new KeyValuePair<string, string>(EncodeWhitespace(e.Key), EncodeWhitespace(e.Value))
            ).ToList();
            var glossaryText = string.Join("\n", encodedEntries.Select(e => $"{e.Key}\t{e.Value}"));

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"DeepL-Auth-Key {apiKey}");

                // List and delete all glossaries
                var listResponse = await client.GetAsync("https://api-free.deepl.com/v2/glossaries");
                listResponse.EnsureSuccessStatusCode();
                var listContent = await listResponse.Content.ReadAsStringAsync();
                var glossaries = JObject.Parse(listContent)["glossaries"];
                if (glossaries != null)
                {
                    foreach (var glossary in glossaries)
                    {
                        var glossaryId = glossary["glossary_id"]?.ToString();
                        if (!string.IsNullOrEmpty(glossaryId))
                        {
                            AutoTranslatorLogger.Info($"Deleting existing DeepL glossary {glossaryId}");

                            var deleteResponse = await client.DeleteAsync($"https://api-free.deepl.com/v2/glossaries/{glossaryId}");
                            deleteResponse.EnsureSuccessStatusCode();
                        }
                    }
                }

                // Create new glossary
                AutoTranslatorLogger.Info($"Creating new DeepL glossary");

                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("name", "ValkyrieGlossary"),
                    new KeyValuePair<string, string>("source_lang", sourceLang.ToUpper()),
                    new KeyValuePair<string, string>("target_lang", targetLang.ToUpper()),
                    new KeyValuePair<string, string>("entries", glossaryText),
                    new KeyValuePair<string, string>("entries_format", "tsv"),
                });

                var response = await client.PostAsync("https://api-free.deepl.com/v2/glossaries", content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"DeepL API error: {response.StatusCode} - {errorContent}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var glossaryIdCreated = JObject.Parse(responseContent)["glossary_id"]?.ToString();
                return glossaryIdCreated;
            }
        }

        public static async Task<string> GetGlossary(string deepLApiKey)
        {
            AutoTranslatorLogger.Info("Searching for existing DeepL glossary");
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"DeepL-Auth-Key {deepLApiKey}");

                var response = await client.GetAsync("https://api-free.deepl.com/v2/glossaries");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                var glossaries = JObject.Parse(content)["glossaries"];
                if (glossaries != null && glossaries.HasValues)
                {
                    var firstGlossary = glossaries.First;
                    var glossaryId = firstGlossary?["glossary_id"]?.ToString();
                    return glossaryId ?? string.Empty;
                }
                return string.Empty;
            }
        }
    }
}