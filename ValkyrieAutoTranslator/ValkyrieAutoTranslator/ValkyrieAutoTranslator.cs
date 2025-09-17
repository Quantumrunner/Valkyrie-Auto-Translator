using Valkyrie.AutoTranslator.Ai;
using Valkyrie.AutoTranslator.AzureTranslation;
using Valkyrie.AutoTranslator.Data;
using Valkyrie.AutoTranslator.Helpers;

namespace Valkyrie.AutoTranslator
{
    internal class AutoTranslator
    {
        private const string cacheFileName = "ValkyrieTranslationCache.csv";
        private readonly string _inputFileName;
        private readonly string _outputPath;
        private readonly string _outputFileNameAdditionalPart;
        private readonly bool _translate;
        private readonly string _csvOutputFileDelimiter;
        private readonly string _targetLanguage;
        private readonly string _sourceLanguageName;
        private readonly string _inputPath;
        private readonly string _targetLanguageName;
        private readonly string _azureCategoryId;
        private readonly string _azureKey;
        private readonly string _sourceLanguage;
        private readonly string _translatorProvider;
        private readonly string _deepLApiKey;
        private readonly string _deepLGlossaryId;
        private readonly string _deepLFormality;
        private readonly string _deepLApiMode;
        private readonly string _deepSeekApiKey;
        private readonly string _llmPrompt;
        private readonly bool _deepLApiUpdateGlossary;
        private readonly bool _useLlmApi;
        private readonly string _glossaryFilePath;
        private readonly string _translationCacheFilePath;
        private HashSet<KeyValuePair<string, string>> translationCache;
        private CsvTool csvTool;

        public AutoTranslator(
            string inputPath,
            string inputFileName,
            string outputPath,
            string outputFileNameAdditionalPart,
            bool translate,
            string targetLanguageName,
            string sourceLanguageName,
            string targetLanguage,
            string sourceLanguage,
            string azureCategoryId,
            string azureKey,
            bool deepLApiUpdateGlossary,
            string deepLApiMode,
            bool useLlmApi,
            string outputDelimiter = null,
            string translatorProvider = TranslatorConstants.ApiNameAzure,
            string deepLApiKey = null,
            string glossaryFilePath = null,
            string translationCacheFilePath = null,
            string deepLFormality = null,
            string deepSeekApiKey = null,
            string llmPrompt = null)
        {
            _azureKey = azureKey;
            _azureCategoryId = azureCategoryId;
            _targetLanguage = targetLanguage;
            _sourceLanguage = sourceLanguage;
            _targetLanguageName = targetLanguageName;
            _sourceLanguageName = sourceLanguageName;
            _inputPath = inputPath;
            _inputFileName = inputFileName;
            _outputPath = outputPath;
            _outputFileNameAdditionalPart = outputFileNameAdditionalPart;
            _translate = translate;
            _csvOutputFileDelimiter = outputDelimiter;
            _translatorProvider = translatorProvider;
            _deepLApiKey = deepLApiKey;
            _deepLFormality = deepLFormality;
            _deepSeekApiKey = deepSeekApiKey;
            _deepLApiMode = deepLApiMode;
            _llmPrompt = llmPrompt;
            _deepLApiUpdateGlossary = deepLApiUpdateGlossary;
            _useLlmApi = useLlmApi;
            _glossaryFilePath = glossaryFilePath;
            _translationCacheFilePath = translationCacheFilePath;

            // Log all properties except for API keys
            AutoTranslatorLogger.Info($"AutoTranslator initialized with:");
            AutoTranslatorLogger.Info($"azureCategoryId={_azureCategoryId}");
            AutoTranslatorLogger.Info($"deepLApiUpdateGlossary={_deepLApiUpdateGlossary}");
            AutoTranslatorLogger.Info($"deepLFormality={_deepLFormality} (possible values are: leave this empty or \"more\" or \"less\" or \"prefer_more\" or \"prefer_less\")");
            AutoTranslatorLogger.Info($"glossaryFilePath={_glossaryFilePath}");
            AutoTranslatorLogger.Info($"inputPath={inputPath}");
            AutoTranslatorLogger.Info($"inputFileName={inputFileName}");
            AutoTranslatorLogger.Info($"translationCacheFilePath={translationCacheFilePath}");
            AutoTranslatorLogger.Info($"outputDelimiter={_csvOutputFileDelimiter}");
            AutoTranslatorLogger.Info($"outputPath={_outputPath}");
            AutoTranslatorLogger.Info($"sourceLanguage={_sourceLanguage} (find supported values here: https://developers.deepl.com/docs/getting-started/supported-languages#translation-target-languages)");
            AutoTranslatorLogger.Info($"sourceLanguageName={_sourceLanguageName}");
            AutoTranslatorLogger.Info($"targetLanguage={_targetLanguage} (find supported values here: https://developers.deepl.com/docs/getting-started/supported-languages#translation-target-languages)");
            AutoTranslatorLogger.Info($"targetLanguageName={_targetLanguageName}");
            AutoTranslatorLogger.Info($"translate={_translate}");
            AutoTranslatorLogger.Info($"translatorProvider={_translatorProvider}");
            AutoTranslatorLogger.Info($"useLlmApi={_useLlmApi}");

            AutoTranslatorLogger.Info($"Properties that will not be logged here=deepSeekApiKey, llmPrompt, deepLApiKey");

            // Log all properties except for API keys
            if (useLlmApi)
            {
                if (string.IsNullOrEmpty(_deepSeekApiKey) || string.IsNullOrEmpty(_llmPrompt))
                {
                    AutoTranslatorLogger.Error("LLM API usage is enabled, but DeepSeekApiKey or LlmPrompt is null or empty. Please provide valid values.");
                    throw new System.Exception("LLM API usage is enabled, but DeepSeekApiKey or LlmPrompt is null or empty. Please provide valid values.");
                }
            }
            if (translate)
            {
                if (string.IsNullOrEmpty(_deepLApiKey))
                {
                    AutoTranslatorLogger.Error("Translation is enabled, but DeepLApiKey is null or empty. Please provide a valid DeepL API key.");
                    throw new System.Exception("Translation is enabled, but DeepLApiKey is null or empty. Please provide a valid DeepL API key.");
                }
            }

            csvTool = new CsvTool(null, _csvOutputFileDelimiter);

            // Update DeepL glossary if required
            if (_translate)
            {
                if (_translatorProvider == TranslatorConstants.ApiNameDeepL)
                {
                    if (_deepLApiUpdateGlossary)
                    {
                        // Path to your glossary CSV
                        if (_glossaryFilePath != null)
                        {
                            List<KeyValuePair<string, string>> glossaryEntries = csvTool.GetLanguagePairSourceAndTragetLanguage(_glossaryFilePath, _sourceLanguageName, _targetLanguageName);
                            _deepLGlossaryId = DeepLTranslator.UpdateGlossaryAsync(_deepLApiKey, _sourceLanguage, _targetLanguage, glossaryEntries).GetAwaiter().GetResult();
                        }
                    }
                    else
                    {
                        _deepLGlossaryId = DeepLTranslator.GetGlossary(_deepLApiKey).GetAwaiter().GetResult();
                    }

                }
            }
        }

        public void CreateTranslatedFiles()
        {
            if (_inputFileName.StartsWith("*"))
            {
                string extension = Path.GetExtension(_inputFileName);
                if (string.IsNullOrEmpty(extension))
                {
                    AutoTranslatorLogger.Error("Input file name does not contain a valid extension.");
                    return;
                }

                string[] files = Directory.GetFiles(_inputPath, "*" + extension);
                foreach (var file in files)
                {
                    string fileName = Path.GetFileName(file);
                    CreateTranslatedFile(_inputPath, fileName);
                }
            }
            else
            {
                CreateTranslatedFile(_inputPath, _inputFileName);
            }
        }

        private void CreateTranslatedFile(string inputPath, string inputFile)
        {
            AutoTranslatorLogger.Info($"Start translating file {inputFile}");
            List<ValkyrieLanguageData> languageData = csvTool.GetFileLanguageData(inputPath, inputFile, false);

            if (!string.IsNullOrWhiteSpace(_translationCacheFilePath))
            {
                translationCache = TranslationCacheHelper.LoadTranslationCache(csvTool, _translationCacheFilePath, cacheFileName, _csvOutputFileDelimiter);
            }
            else
            {
                translationCache = new HashSet<KeyValuePair<string, string>>();
            }

            List<ValkyrieLanguageData> list = new List<ValkyrieLanguageData>();
            foreach (ValkyrieLanguageData languageDataSingle in languageData)
            {
                TranslateData(list, languageDataSingle);
            }
            GenerateTranslatedFile(list, _outputPath, inputFile, _outputFileNameAdditionalPart, _csvOutputFileDelimiter);

            TranslationCacheHelper.SaveTranslationCache(csvTool, _translationCacheFilePath, cacheFileName, _csvOutputFileDelimiter, translationCache);

            AutoTranslatorLogger.Info($"Finished translating file {inputFile}");
        }

        private void GenerateTranslatedFile(List<ValkyrieLanguageData> translatedData, string path, string fileNameOld, string fileNameNewAdditionalPart, string delimiter)
        {
            CsvTool csvTool = new CsvTool(null, delimiter);
            csvTool.CreateCsvFile(path, fileNameOld, fileNameNewAdditionalPart, null, translatedData, false, delimiter);
        }

        private void TranslateData(List<ValkyrieLanguageData> newData, ValkyrieLanguageData data)
        {
            data.Value = TranslateByPreValue(data.Key, data.Value);
            newData.Add(data);
        }

        private string TranslateByPreValue(string key, string value)
        {
            string valueBefore = value;
            //AutoTranslatorLogger.Info($"Before: {valueBefore}");

            if (value.Equals(_sourceLanguageName))
            {
                return _targetLanguageName;
            }

            var curlyBracketWords = AutoTranslatorHelpers.IdentifyWordsInCurlyBrackets(value);

            string translatedValue = value;

            string[] skipValues = new string[]
            {
                "quest.authors",
                "quest.authors_short"
            };

            //skip certain keys from translation
            if (skipValues.Any(s => s.Equals(key)))
            {
                AutoTranslatorLogger.Info($"Skipping translation for key: {key}");
                return value;
            }

            //use cached value if available
            if (translationCache.Any(c => c.Key.Equals(value, System.StringComparison.OrdinalIgnoreCase)))
            {
                translatedValue = translationCache.FirstOrDefault(c => c.Key == value).Value;
                AutoTranslatorLogger.Info($"Using cached value for: {value}");

            }
            //if not cached, user the translator API
            else
            {
                if (_translate)
                {
                    value = AutoTranslatorHelpers.AddWhiteSpaceForLineBreaks(value);
                    value = AutoTranslatorHelpers.AddNoTranslationTagForQuotationMarks(value, _translatorProvider);
                    value = AutoTranslatorHelpers.MarkKeepTags(value);

                    if (_translatorProvider == TranslatorConstants.ApiNameDeepL)
                    {
                        AutoTranslatorLogger.Info($"Start using DeepL translator for key {key}");
                        translatedValue = DeepLTranslator.Translate(_deepLApiMode, value, _sourceLanguage, _targetLanguage, _deepLApiKey, _deepLGlossaryId, _deepLFormality).GetAwaiter().GetResult();
                        AutoTranslatorLogger.Success($"Finished using DeepL translator for {key}");
                    }
                    else
                    {
                        AutoTranslatorLogger.Info($"Start using Azure translator for key {key}");
                        translatedValue = AzureTranslator.Translate(value, translationCache, _sourceLanguage, _targetLanguage, _azureKey, _azureCategoryId).GetAwaiter().GetResult();
                        AutoTranslatorLogger.Success($"Finished using Azure translator for {key}");
                    }
                }

                translatedValue = AutoTranslatorHelpers.RemoveKeepTags(translatedValue);
                translatedValue = AutoTranslatorHelpers.RevertNoTranslationTags(translatedValue);

                if (_useLlmApi)
                {
                    AutoTranslatorLogger.Info($"Start using DeepSeek LLM for key {key}");
                    translatedValue = DeepSeekApi.ExecutePromptAsync(_deepSeekApiKey, _llmPrompt, key, translatedValue).GetAwaiter().GetResult();
                    AutoTranslatorLogger.Success($"Finished using DeepSeek LLM for key {key}");
                }
            }

            // Remove <keep> tags after translation
            translatedValue = AutoTranslatorHelpers.ReplaceDoubleQuotesWithPipes(translatedValue);
            translatedValue = AutoTranslatorHelpers.EnsureStartWithThreePipes(translatedValue, _targetLanguage);
            translatedValue = AutoTranslatorHelpers.EnsureStartWithPipesAlsoEndsWithPipes(translatedValue, _targetLanguage);
            translatedValue = AutoTranslatorHelpers.FindAndReplacedTranslatedCurlyBracketsWords(translatedValue, curlyBracketWords);
            translatedValue = AutoTranslatorHelpers.ReplaceQuotesWithEnglishspecialCharacterQuotation(translatedValue, _targetLanguage);
            translatedValue = AutoTranslatorHelpers.ReplaceBackslashNotFollowedByNWithLineBreak(translatedValue);
            if (_translatorProvider != TranslatorConstants.ApiNameDeepL)
            {
                translatedValue = AutoTranslatorHelpers.ReplaceDeepLSpecialGlossaryChar(translatedValue);
            }


            if (_translatorProvider != TranslatorConstants.ApiNameDeepL)
            {
                translatedValue = AutoTranslatorHelpers.ReplaceLineBreaksWithOldValue(translatedValue);
            }

            translationCache.Add(new KeyValuePair<string, string>(valueBefore, translatedValue));

            AutoTranslatorLogger.Success($"Finished all operations for key: {key}");
            return translatedValue;
        }
    }

}
