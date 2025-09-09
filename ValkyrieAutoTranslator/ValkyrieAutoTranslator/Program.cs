using Microsoft.Extensions.Configuration;

namespace Valkyrie.AutoTranslator
{
    public class Program
    {
        static void Main(string[] args)
        {
            // Build configuration from appsettings.json
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddUserSecrets<Program>() // For sensitive data like API keys
                .Build();

            string azureAuth = configuration["secrets:azureAuthentificationKey"];
            string deepSeekApiKey = configuration["secrets:deepSeekApiKey"];
            string deepLApiKey = configuration["secrets:deepLApiKey"];

            bool translate = bool.Parse(configuration["translation:translate"] ?? "false");
            string translationProvider = configuration["translation:translatorProvider"];
            string deepLGlossaryFilePath = configuration["translation:deepL:deepLGlossaryFilePath"];
            string sourceLanguage = configuration["translation:sourceLanguage"];
            string targetLanguageName = configuration["translation:targetLanguageName"];
            string targetLanguage = configuration["translation:targetLanguage"];
            string sourceLanguageName = configuration["translation:sourceLanguageName"];
            string categoryId = configuration["translation:azure:azureCategoryId"];
            bool deepLApiUpdateGlossary = bool.Parse(configuration["translation:deepL:deepLApiUpdateGlossary"] ?? "false");
            string deepLFormality = configuration["translation:deepL:deepLFormality"];
            string deepLApiMode = configuration["translation:deepL:deepLApiMode"];

            string llmPrompt = configuration["llm:llmPrompt"];
            bool useLlmApi = bool.Parse(configuration["llm:useLlmApi"] ?? "false");

            string translationCacheFilePath = configuration["cache:translationCacheFilePath"];

            string inputPath = configuration["fileInputOutput:inputPath"];
            string inputFileName = configuration["fileInputOutput:inputFileName"];
            string outputPath = configuration["fileInputOutput:outputPath"];
            string outputFileNameAdditionalPart = configuration["fileInputOutput:outputFileNameAdditionalPart"];
            string csvOutputFileDelimiter = configuration["fileInputOutput:csvOutputFileDelimiter"];

            AutoTranslator autoTranslator = new AutoTranslator(
                inputPath, inputFileName, outputPath, outputFileNameAdditionalPart, translate, targetLanguageName, sourceLanguageName, targetLanguage, sourceLanguage,
                categoryId, azureAuth, deepLApiUpdateGlossary, deepLApiMode, useLlmApi, csvOutputFileDelimiter, translationProvider, deepLApiKey, deepLGlossaryFilePath, translationCacheFilePath, deepLFormality, deepSeekApiKey, llmPrompt
            );
            autoTranslator.CreateTranslatedFiles();
        }
    }
}