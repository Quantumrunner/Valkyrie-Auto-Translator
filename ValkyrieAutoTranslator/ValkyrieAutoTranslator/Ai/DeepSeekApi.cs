using System.Net;
using System.Text;

namespace Valkyrie.AutoTranslator.Ai
{
    internal static class DeepSeekApi
    {
        private const int MaxRetries = 3;
        private const int ThrottleDelayMs = 1000;

        public static async Task<Tuple<string, bool>> ExecutePromptAsync(string deepseekApiKey, string llmPrompt, string key, string value)
        {
            using (var httpClient = new HttpClient())
            {
                if (string.IsNullOrWhiteSpace(deepseekApiKey))
                    throw new ArgumentException("API key is required.", nameof(deepseekApiKey));
                if (string.IsNullOrWhiteSpace(llmPrompt))
                    throw new ArgumentException("Prompt is required.", nameof(llmPrompt));


                string combineKeyAndValue = $"Key={key} \nValue={value}";

                string apiUrl = "https://api.deepseek.com/v1/chat/completions";
                var requestBody = new
                {
                    model = "deepseek-chat",
                    messages = new[]
                    {
                    new { role = "system", content = llmPrompt },
                    new { role = "user", content = combineKeyAndValue }
                }
                };
                string jsonBody = Newtonsoft.Json.JsonConvert.SerializeObject(requestBody);

                int retries = 0;
                int maxRetries = MaxRetries;
                int delay = ThrottleDelayMs;

                while (true)
                {
                    try
                    {
                        using (var request = new HttpRequestMessage(HttpMethod.Post, apiUrl))
                        {
                            request.Headers.Add("Authorization", $"Bearer {deepseekApiKey}");
                            request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                            using (var response = await httpClient.SendAsync(request))
                            {
                                if (response.StatusCode == (HttpStatusCode)429)
                                {
                                    // Too Many Requests - throttle and retry
                                    if (retries >= maxRetries)
                                        throw new Exception("Too many requests. Retry limit reached.");
                                    await Task.Delay(delay);
                                    retries++;
                                    delay *= 2;
                                    continue;
                                }
                                response.EnsureSuccessStatusCode();
                                string responseContent = await response.Content.ReadAsStringAsync();
                                dynamic result = Newtonsoft.Json.JsonConvert.DeserializeObject(responseContent);
                                string resultString = result?.choices?[0]?.message?.content?.ToString() ?? string.Empty;
                                return new Tuple<string, bool>(resultString, false);
                            }
                        }
                    }
                    catch (HttpRequestException)
                    {
                        if (retries >= maxRetries)
                            throw;
                        await Task.Delay(delay);
                        retries++;
                        delay *= 2;
                    }
                }
            }

        }
    }
}
