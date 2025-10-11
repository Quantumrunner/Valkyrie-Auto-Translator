using Microsoft.Extensions.Configuration;

namespace Valkyrie.AutoTranslator
{
    public class Program
    {
        static void Main(string[] args)
        {
            // Build configuration from appsettings.json
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddUserSecrets<Program>(optional: true)
                .Build();

            string deepSeekApiKey = config["secrets:deepSeekApiKey"];
            string deepLApiKey = config["secrets:deepLApiKey"];

            bool translate = bool.Parse(config["translation:translate"] ?? "false");
            string translationProvider = config["translation:translatorProvider"];
            string deepLGlossaryFilePath = config["translation:deepL:deepLGlossaryFilePath"];
            string sourceLanguage = config["translation:sourceLanguage"];
            string targetLanguageName = config["translation:targetLanguageName"];
            string targetLanguage = config["translation:targetLanguage"];
            string sourceLanguageName = config["translation:sourceLanguageName"];
            bool deepLApiUpdateGlossary = bool.Parse(config["translation:deepL:deepLApiUpdateGlossary"] ?? "false");
            var deepLContextDefault = config["translation:deepL:deepLContext:default"];
            var deepLContextActivation = config["translation:deepL:deepLContext:activation"];
            string deepLFormality = config["translation:deepL:deepLFormality"];
            string deepLApiMode = config["translation:deepL:deepLApiMode"];

            string llmPrompt = config["llm:llmPrompt"];
            var llmKeyWords = config.GetSection("llm:llmKeyWords").Get<string[]>()?.ToList() ?? new List<string>();
            var llmKeyWordsDefault = config.GetSection("llm:llmKeyWordsDefault").Get<string[]>()?.ToList() ?? new List<string>();
            var llmKeyWordsActivation = config.GetSection("llm:llmKeyWordsActivation").Get<string[]>()?.ToList() ?? new List<string>();
            bool useLlmApi = bool.Parse(config["llm:useLlmApi"] ?? "false");

            bool useTranslationCache = bool.Parse(config["cache:useTranslationCache"] ?? "false");
            string translationCacheFilePath = config["cache:translationCacheFilePath"];

            string inputPath = config["fileInputOutput:inputPath"];
            string inputFileName = config["fileInputOutput:inputFileName"];
            string outputPath = config["fileInputOutput:outputPath"];
            string outputFileNameAdditionalPart = config["fileInputOutput:outputFileNameAdditionalPart"];
            string csvOutputFileDelimiter = config["fileInputOutput:csvOutputFileDelimiter"];

            bool addCacheToDictionary = bool.Parse(config["cache:addCacheToDictionary"] ?? "false");

            AutoTranslator autoTranslator = new AutoTranslator(
                inputPath, inputFileName, outputPath, outputFileNameAdditionalPart, translate, targetLanguageName, sourceLanguageName, targetLanguage, sourceLanguage,
                deepLApiUpdateGlossary, deepLApiMode, useLlmApi, useTranslationCache, csvOutputFileDelimiter, translationProvider, deepLApiKey, deepLGlossaryFilePath, translationCacheFilePath, deepLFormality, deepLContextDefault, deepLContextActivation, deepSeekApiKey, llmPrompt, llmKeyWordsDefault, llmKeyWordsActivation, addCacheToDictionary
            );
            autoTranslator.CreateTranslatedFiles();
        }
    }
}