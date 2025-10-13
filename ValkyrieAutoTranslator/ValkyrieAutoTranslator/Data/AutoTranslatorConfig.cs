namespace ValkyrieAutoTranslator.Data
{
    public class AutoTranslatorConfig
    {
        public SecretsConfig Secrets { get; set; }
        public TranslationConfig Translation { get; set; }
        public LlmConfig Llm { get; set; }
        public CacheConfig Cache { get; set; }
        public FileInputOutputConfig FileInputOutput { get; set; }

        public class SecretsConfig
        {
            public string DeepSeekApiKey { get; set; }
            public string DeepLApiKey { get; set; }
        }

        public class TranslationConfig
        {
            public bool Translate { get; set; }
            public string TranslatorProvider { get; set; }
            public string SourceLanguage { get; set; }
            public string TargetLanguageName { get; set; }
            public string TargetLanguage { get; set; }
            public string SourceLanguageName { get; set; }
            public DeepLConfig DeepL { get; set; }

            public class DeepLConfig
            {
                public string DeepLApiMode { get; set; }
                public bool DeepLApiUpdateGlossary { get; set; }
                public DeepLContextConfig DeepLContext { get; set; }
                public string DeepLFormality { get; set; }
                public bool DeleteExistingGlossaries { get; set; }
                public string DeepLGlossaryFilePath { get; set; }

                public class DeepLContextConfig
                {
                    public string Default { get; set; }
                    public string Activation { get; set; }
                }
            }
        }

        public class LlmConfig
        {
            public string LlmPrompt { get; set; }
            public List<string> LlmKeyWords { get; set; }
            public List<string> LlmKeyWordsDefault { get; set; }
            public List<string> LlmKeyWordsActivation { get; set; }
            public bool UseLlmApi { get; set; }
        }

        public class CacheConfig
        {
            public bool UseTranslationCache { get; set; }
            public bool AddCacheToDictionary { get; set; }
            public string TranslationCacheFilePath { get; set; }
        }

        public class FileInputOutputConfig
        {
            public string InputPath { get; set; }
            public string InputFileName { get; set; }
            public string OutputPath { get; set; }
            public string OutputFileNameAdditionalPart { get; set; }
            public string CsvOutputFileDelimiter { get; set; }
        }
    }
}