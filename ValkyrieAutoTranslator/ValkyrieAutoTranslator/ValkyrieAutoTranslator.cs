using Valkyrie.AutoTranslator.Ai;
using Valkyrie.AutoTranslator.Data;
using Valkyrie.AutoTranslator.Helpers;
using ValkyrieAutoTranslator.Data;

namespace Valkyrie.AutoTranslator
{
    internal class AutoTranslator
    {
        private const string cacheFileName = "ValkyrieTranslationCache.csv";
        private readonly AutoTranslatorConfig _config;
        private string _deepLGlossaryId;
        private TranslationCacheManager _translationCacheManager;
        private CsvTool csvTool;

        public AutoTranslator(AutoTranslatorConfig config)
        {
            _config = config;

            LogPropertiesExceptSecrets();
            ValidateConfiguration();

            csvTool = new CsvTool(null, _config.FileInputOutput.CsvOutputFileDelimiter);

            _translationCacheManager = new TranslationCacheManager(
                csvTool,
                _config.Cache.TranslationCacheFilePath,
                cacheFileName,
                _config.FileInputOutput.CsvOutputFileDelimiter
            );

            string glossaryIdValue = string.Empty;
            glossaryIdValue = CreateDeepLGlossary(glossaryIdValue);
            _deepLGlossaryId = glossaryIdValue;
        }

        private void ValidateConfiguration()
        {
            if (_config.Llm.UseLlmApi)
            {
                if (string.IsNullOrEmpty(_config.Secrets.DeepSeekApiKey) || string.IsNullOrEmpty(_config.Llm.LlmPrompt))
                {
                    AutoTranslatorLogger.Error("LLM API usage is enabled, but DeepSeekApiKey or LlmPrompt is null or empty. Please provide valid values.");
                    throw new System.Exception("LLM API usage is enabled, but DeepSeekApiKey or LlmPrompt is null or empty. Please provide valid values.");
                }
            }
            if (_config.Translation.Translate)
            {
                if (string.IsNullOrEmpty(_config.Secrets.DeepLApiKey))
                {
                    AutoTranslatorLogger.Error("Translation is enabled, but DeepLApiKey is null or empty. Please provide a valid DeepL API key.");
                    throw new System.Exception("Translation is enabled, but DeepLApiKey is null or empty. Please provide a valid DeepL API key.");
                }
            }
        }

        private void LogPropertiesExceptSecrets()
        {
            AutoTranslatorLogger.Info($"AutoTranslator initialized with:");
            AutoTranslatorLogger.Info($"deepLApiUpdateGlossary={_config.Translation.DeepL.DeepLApiUpdateGlossary}");
            AutoTranslatorLogger.Info($"deepLFormality={_config.Translation.DeepL.DeepLFormality}");
            AutoTranslatorLogger.Info($"glossaryFilePath={_config.Translation.DeepL.DeepLGlossaryFilePath}");
            AutoTranslatorLogger.Info($"inputPath={_config.FileInputOutput.InputPath}");
            AutoTranslatorLogger.Info($"inputFileName={_config.FileInputOutput.InputFileName}");
            AutoTranslatorLogger.Info($"translationCacheFilePath={_config.Cache.TranslationCacheFilePath}");
            AutoTranslatorLogger.Info($"outputDelimiter={_config.FileInputOutput.CsvOutputFileDelimiter}");
            AutoTranslatorLogger.Info($"outputPath={_config.FileInputOutput.OutputPath}");
            AutoTranslatorLogger.Info($"sourceLanguage={_config.Translation.SourceLanguage}");
            AutoTranslatorLogger.Info($"sourceLanguageName={_config.Translation.SourceLanguageName}");
            AutoTranslatorLogger.Info($"targetLanguage={_config.Translation.TargetLanguage}");
            AutoTranslatorLogger.Info($"targetLanguageName={_config.Translation.TargetLanguageName}");
            AutoTranslatorLogger.Info($"translate={_config.Translation.Translate}");
            AutoTranslatorLogger.Info($"translatorProvider={_config.Translation.TranslatorProvider}");
            AutoTranslatorLogger.Info($"useLlmApi={_config.Llm.UseLlmApi}");

            AutoTranslatorLogger.Info($"Properties that will not be logged here=deepSeekApiKey, llmPrompt, deepLApiKey, deepLContextDefault, deepLContextActivation");

            if (_config.Cache.UseTranslationCache && _config.Cache.TranslationCacheFilePath == null)
            {
                throw new ArgumentNullException("translationCacheFilePath", "Translation cache file path cannot be null. Please provide a valid path.");
            }
        }

        private string CreateDeepLGlossary(string glossaryIdValue)
        {
            if (_config.Translation.Translate)
            {
                if (_config.Translation.TranslatorProvider == TranslatorConstants.ApiNameDeepL)
                {
                    if (_config.Translation.DeepL.DeepLApiUpdateGlossary)
                    {
                        List<KeyValuePair<string, string>> glossaryEntries = new List<KeyValuePair<string, string>>();
                        if (_config.Translation.DeepL.DeepLGlossaryFilePath != null)
                        {
                            glossaryEntries = csvTool.GetLanguagePairSourceAndTargetLanguage(
                                _config.Translation.DeepL.DeepLGlossaryFilePath,
                                _config.Translation.SourceLanguageName,
                                _config.Translation.TargetLanguageName
                            );
                        }

                        // Only add cache if the flag is true
                        if (_config.Cache.AddCacheToDictionary)
                        {
                            AddTranslationCacheToGlossary(glossaryEntries);
                        }

                        glossaryIdValue = DeepLTranslator.UpdateGlossaryAsync(
                            _config.Secrets.DeepLApiKey,
                            _config.Translation.DeepL.DeleteExistingGlossaries,
                            _config.Translation.SourceLanguage,
                            _config.Translation.TargetLanguage,
                            glossaryEntries
                        ).GetAwaiter().GetResult();
                    }
                    else
                    {
                        glossaryIdValue = DeepLTranslator.GetGlossary(_config.Secrets.DeepLApiKey).GetAwaiter().GetResult();
                    }
                }
            }

            return glossaryIdValue;
        }

        private void AddTranslationCacheToGlossary(List<KeyValuePair<string, string>> glossaryEntries)
        {
            AutoTranslatorLogger.Info("Adding translation cache entries to glossary.");
            // Add entries from translation cache if not already present in glossary
            if (_config.Cache.UseTranslationCache && _translationCacheManager != null)
            {
                var cacheEntries = _translationCacheManager.GetAllTranslations();
                var glossarySet = new List<KeyValuePair<string, string>>(glossaryEntries);

                foreach (var entry in cacheEntries)
                {
                    if (!glossaryEntries.Any(e => e.Key.Equals(entry.Key, StringComparison.OrdinalIgnoreCase)))
                    {
                        glossaryEntries.Add(entry);
                    }
                }
            }
        }

        public void CreateTranslatedFiles()
        {
            if (_config.FileInputOutput.InputFileName.StartsWith("*"))
            {
                string extension = Path.GetExtension(_config.FileInputOutput.InputFileName);
                AutoTranslatorLogger.Info($"Loading all files of type {extension} from path {_config.FileInputOutput.InputPath}.");
                if (string.IsNullOrEmpty(extension))
                {
                    AutoTranslatorLogger.Error("Input file name does not contain a valid extension.");
                    return;
                }

                string[] files = Directory.GetFiles(_config.FileInputOutput.InputPath, "*" + extension);
                foreach (var file in files)
                {
                    string fileName = Path.GetFileName(file);
                    CreateTranslatedFile(_config.FileInputOutput.InputPath, fileName);
                }
            }
            else
            {
                CreateTranslatedFile(_config.FileInputOutput.InputPath, _config.FileInputOutput.InputFileName);
            }
        }

        private void CreateTranslatedFile(string inputPath, string inputFile)
        {
            AutoTranslatorLogger.Info($"Start translating file {inputFile}");
            List<ValkyrieLanguageData> languageData = csvTool.GetFileLanguageData(inputPath, inputFile, false);

            List<ValkyrieLanguageData> list = new List<ValkyrieLanguageData>();
            foreach (ValkyrieLanguageData languageDataSingle in languageData)
            {
                TranslateData(list, languageDataSingle);
            }
            GenerateTranslatedFile(list, _config.FileInputOutput.OutputPath, inputFile, _config.FileInputOutput.OutputFileNameAdditionalPart, _config.FileInputOutput.CsvOutputFileDelimiter);

            //Only save cache if any real changes were done
            if (_config.Cache.UseTranslationCache && (_config.Translation.Translate || _config.Llm.UseLlmApi))
            {
                _translationCacheManager.SaveCache();
            }

            AutoTranslatorLogger.Info($"Finished translating file {inputFile}");
        }

        private void GenerateTranslatedFile(List<ValkyrieLanguageData> translatedData, string path, string fileNameOld, string fileNameNewAdditionalPart, string delimiter)
        {
            CsvTool csvTool = new CsvTool(null, delimiter);
            csvTool.CreateCsvFile(path, fileNameOld, fileNameNewAdditionalPart, null, translatedData, false, delimiter);
        }

        private void TranslateData(List<ValkyrieLanguageData> newData, ValkyrieLanguageData data)
        {
            data.Value = TranslateText(data.Key, data.Value);
            newData.Add(data);
        }

        private string TranslateText(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            if (value.Equals(_config.Translation.SourceLanguageName))
            {
                return _config.Translation.TargetLanguageName;
            }

            string[] skipValues = new string[] {
                "quest.authors",
                "quest.authors_short"
            };

            //skip certain keys from translation
            if (skipValues.Any(s => s.Equals(key)))
            {
                AutoTranslatorLogger.Info($"Skipping translation for key: {key}");
                return value;
            }

            // Handle surrounding markers like ||| or "
            string prefix = "";
            string suffix = "";
            string coreValue = value;

            if (coreValue.StartsWith("|||") && coreValue.EndsWith("|||"))
            {
                prefix = "|||";
                suffix = "|||";
                coreValue = coreValue.Substring(3, coreValue.Length - 6);
            }
            else if (AutoTranslatorHelpers.IsEncapsulatedWithQuotes(coreValue))
            {
                prefix = coreValue.Substring(0, 1);
                suffix = coreValue.Substring(coreValue.Length - 1, 1);
                coreValue = coreValue.Substring(1, coreValue.Length - 2);
            }

            var sentences = SplitIntoSentences(coreValue);
            var translatedSentences = new List<string>();

            foreach (var sentence in sentences)
            {
                var translatedString = TranslateSentence(key, sentence);
                translatedSentences.Add(translatedString);
            }

            // Build the final string with smarter joining to avoid extra spaces around HTML tags.
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < translatedSentences.Count; i++)
            {
                string current = translatedSentences[i];
                sb.Append(current);

                // Add a space if the current part and the next part are both actual text (not tags or just whitespace).
                if (i < translatedSentences.Count - 1)
                {
                    string next = translatedSentences[i + 1];
                    bool currentIsText = !current.StartsWith("<") && !current.EndsWith(">");
                    bool nextIsText = !next.StartsWith("<") && !next.EndsWith(">");
                    if (currentIsText && nextIsText) sb.Append(" ");
                }
            }
            string finalTranslatedValue = sb.ToString();

            string combinedFinal = prefix + finalTranslatedValue + suffix;
            combinedFinal = AutoTranslatorHelpers.ReplaceDoubleQuotesWithPipes(combinedFinal);
            combinedFinal = AutoTranslatorHelpers.EnsureStartWithThreePipes(combinedFinal, _config.Translation.TargetLanguage);
            combinedFinal = AutoTranslatorHelpers.EnsureStartWithPipesAlsoEndsWithPipes(combinedFinal, _config.Translation.TargetLanguage);
            combinedFinal = AutoTranslatorHelpers.ReplaceWhiteSpacesBetweenNewlines(combinedFinal);
            return combinedFinal;
        }

        private List<string> SplitIntoSentences(string text)
        {
            if (string.IsNullOrEmpty(text))
                return new List<string>();

            // Regex to split text into sentences. It splits after sentence-ending punctuation (. ! ?)
            // followed by whitespace, or after one or more newlines (\n). It also handles splitting
            // around HTML tags and captures delimiters like newlines.
            // This will split the string, keeping the delimiters as separate entries in the resulting array.
            var regex = new System.Text.RegularExpressions.Regex(@"(\s*\\n\s*|(?<=[.!?])\s+|<i>|</i>|<b>|</b>)");
            var sentences = regex.Split(text).Where(s => !string.IsNullOrWhiteSpace(s)).ToList();

            return sentences;
        }

        private string TranslateSentence(string key, string value)
        {
            string valueBefore = value;

            // If the "sentence" is just whitespace or newlines, don't translate it.
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            // Regex to check if the string contains any letters.
            // If not, it's likely just punctuation, numbers, or special characters (like \n)
            // and should not be translated. This also handles HTML tags and escaped newlines.
            string trimmedValue = value.Trim();

            // Create a test string where known non-translatable tokens are removed.
            string testValue = trimmedValue;
            testValue = System.Text.RegularExpressions.Regex.Replace(testValue, @"\\n", ""); // Remove \n sequences
            testValue = System.Text.RegularExpressions.Regex.Replace(testValue, @"</?[ib]>", ""); // Remove <i>, <b> tags

            // A string is translatable if it still contains letters after removing formatting tokens.
            bool containsRealText = System.Text.RegularExpressions.Regex.IsMatch(testValue, @"[a-zA-Z]");

            if (!containsRealText)
            {
                AutoTranslatorLogger.Info($"Skipping translation for formatting or non-alphabetic sentence: {value}");
                return value;
            }

            var curlyBracketWords = AutoTranslatorHelpers.IdentifyWordsInCurlyBrackets(value);

            string translatedValue = value;
            bool errorOccurred = false;

            //use cached value if available
            if (_config.Cache.UseTranslationCache && _translationCacheManager.TryGetTranslation(value, out var cachedTranslation))
            {
                translatedValue = cachedTranslation;
                AutoTranslatorLogger.Info($"Using cached value for: {value}");
            }
            //if not cached, user the translator API
            else
            {
                if (_config.Translation.Translate)
                {
                    value = AutoTranslatorHelpers.AddWhiteSpaceForLineBreaks(value);
                    value = AutoTranslatorHelpers.AddNoTranslationTagForQuotationMarks(value, _config.Translation.TranslatorProvider);
                    value = AutoTranslatorHelpers.MarkKeepTags(value);

                    if (_config.Translation.TranslatorProvider == TranslatorConstants.ApiNameDeepL)
                    {
                        AutoTranslatorLogger.Info($"Start using DeepL translator for sentence: {value}");
                        var tuple = DeepLTranslator.Translate(
                            _config.Translation.DeepL.DeepLApiMode,
                            key,
                            value,
                            _config.Translation.SourceLanguage,
                            _config.Translation.TargetLanguage,
                            _config.Secrets.DeepLApiKey,
                            _deepLGlossaryId,
                            _config.Translation.DeepL.DeepLContext.Default,
                            _config.Translation.DeepL.DeepLContext.Activation,
                            _config.Translation.DeepL.DeepLFormality
                        ).GetAwaiter().GetResult();
                        if (tuple.Item2) // if error occurred, use original text
                        {
                            errorOccurred = true;
                        }
                        else
                        {
                            translatedValue = tuple.Item1;
                        }
                        AutoTranslatorLogger.Success($"Finished using DeepL translator for sentence: {value}");
                    }
                    else
                    {
                        AutoTranslatorLogger.Error("Only DeepL translator is supported at the moment.");
                    }
                }

                translatedValue = AutoTranslatorHelpers.RemoveKeepTags(translatedValue);
                translatedValue = AutoTranslatorHelpers.RevertNoTranslationTags(translatedValue);

                // Check if the translated value contains more than one word before calling the LLM.
                bool isSingleWord = !translatedValue.Trim().Contains(" ");

                if (_config.Llm.UseLlmApi && !isSingleWord)
                {
                    AutoTranslatorLogger.Info($"Start using DeepSeek LLM for sentence: {translatedValue}");

                    // Use configured keywords
                    List<string> selectedKeyWords = key.StartsWith("activation", StringComparison.OrdinalIgnoreCase)
                        ? (_config.Llm.LlmKeyWordsActivation ?? new List<string>())
                        : (_config.Llm.LlmKeyWordsDefault ?? new List<string>());

                    bool runLlmQuery = ContainsAnyKeyWords(translatedValue, selectedKeyWords);

                    if (runLlmQuery)
                    {
                        var llmResult = DeepSeekApi.ExecutePromptAsync(_config.Secrets.DeepSeekApiKey, _config.Llm.LlmPrompt, key, translatedValue).GetAwaiter().GetResult();
                        if (llmResult.Item2) // if error occurred, use original text
                        {
                            errorOccurred = true;
                        }
                        translatedValue = llmResult.Item1;
                        AutoTranslatorLogger.Success($"Finished using DeepSeek LLM for sentence: {value}");
                    }
                }
                else if (_config.Llm.UseLlmApi && isSingleWord)
                {
                    AutoTranslatorLogger.Info($"Skipping LLM for single-word sentence: {translatedValue}");
                }
            }

            // Remove <keep> tags after translation
            translatedValue = AutoTranslatorHelpers.FindAndReplacedTranslatedCurlyBracketsWords(translatedValue, curlyBracketWords);
            translatedValue = AutoTranslatorHelpers.ReplaceQuotesWithEnglishspecialCharacterQuotation(translatedValue, _config.Translation.TargetLanguage);
            translatedValue = AutoTranslatorHelpers.ReplaceBackslashNotFollowedByNWithLineBreak(translatedValue);
            if (_config.Translation.TranslatorProvider != TranslatorConstants.ApiNameDeepL)
            {
                translatedValue = AutoTranslatorHelpers.ReplaceDeepLSpecialGlossaryChar(translatedValue);
            }

            if (_config.Translation.TranslatorProvider != TranslatorConstants.ApiNameDeepL)
            {
                translatedValue = AutoTranslatorHelpers.ReplaceLineBreaksWithOldValue(translatedValue);
            }

            if (!errorOccurred)
            {
                _translationCacheManager.AddTranslation(valueBefore, translatedValue);
            }

            AutoTranslatorLogger.Success($"Finished all operations for sentence: {value}");
            return translatedValue;
        }

        private bool ContainsAnyKeyWords(string input, List<string> keywords)
        {
            if (keywords == null || keywords.Count == 0)
                return false;

            foreach (var word in keywords)
            {
                // Use regex to match whole words with whitespace boundaries
                var pattern = $@"\b{System.Text.RegularExpressions.Regex.Escape(word)}\b";
                if (System.Text.RegularExpressions.Regex.IsMatch(input, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                {
                    AutoTranslatorLogger.Info($"LLM keyword matched: {word} in input: {input}");
                    return true;
                }
            }
            return false;
        }
    }
}