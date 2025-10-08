using Valkyrie.AutoTranslator.Ai;
using Valkyrie.AutoTranslator.Data;
using Valkyrie.AutoTranslator.Helpers;

namespace Valkyrie.AutoTranslator {
    internal class AutoTranslator {
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
        private readonly string _deepLContextDefault;
        private readonly string _deepLContextActivation;
        private readonly string _deepLApiMode;
        private readonly string _deepSeekApiKey;
        private readonly string _llmPrompt;
        private readonly bool _deepLApiUpdateGlossary;
        private readonly bool _useLlmApi;
        private readonly string _glossaryFilePath;
        private readonly string _translationCacheFilePath;
        private readonly bool _useTranslationCache;
        private TranslationCacheManager _translationCacheManager;
        private CsvTool csvTool;

        public AutoTranslator (
            string inputPath,
            string inputFileName,
            string outputPath,
            string outputFileNameAdditionalPart,
            bool translate,
            string targetLanguageName,
            string sourceLanguageName,
            string targetLanguage,
            string sourceLanguage,
            bool deepLApiUpdateGlossary,
            string deepLApiMode,
            bool useLlmApi,
            bool useTranslationCache,
            string outputDelimiter = null,
            string translatorProvider = TranslatorConstants.ApiNameAzure,
            string deepLApiKey = null,
            string glossaryFilePath = null,
            string translationCacheFilePath = null,
            string deepLFormality = null,
            string deepLContextDefault = null,
            string deepLContextActivation = null,
            string deepSeekApiKey = null,
            string llmPrompt = null) {
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
            _deepLContextDefault = deepLContextDefault;
            _deepLContextActivation = deepLContextActivation;
            _llmPrompt = llmPrompt;
            _deepLApiUpdateGlossary = deepLApiUpdateGlossary;
            _useLlmApi = useLlmApi;
            _glossaryFilePath = glossaryFilePath;
            _translationCacheFilePath = translationCacheFilePath;
            _useTranslationCache = useTranslationCache;

            // Log all properties except for API keys
            AutoTranslatorLogger.Info ($"AutoTranslator initialized with:");
            AutoTranslatorLogger.Info ($"azureCategoryId={_azureCategoryId}");
            AutoTranslatorLogger.Info ($"deepLApiUpdateGlossary={_deepLApiUpdateGlossary}");
            AutoTranslatorLogger.Info ($"deepLFormality={_deepLFormality} (possible values are: leave this empty or \"more\" or \"less\" or \"prefer_more\" or \"prefer_less\")");
            AutoTranslatorLogger.Info ($"glossaryFilePath={_glossaryFilePath}");
            AutoTranslatorLogger.Info ($"inputPath={inputPath}");
            AutoTranslatorLogger.Info ($"inputFileName={inputFileName}");
            AutoTranslatorLogger.Info ($"translationCacheFilePath={translationCacheFilePath}");
            AutoTranslatorLogger.Info ($"outputDelimiter={_csvOutputFileDelimiter}");
            AutoTranslatorLogger.Info ($"outputPath={_outputPath}");
            AutoTranslatorLogger.Info ($"sourceLanguage={_sourceLanguage} (find supported values here: https://developers.deepl.com/docs/getting-started/supported-languages#translation-target-languages)");
            AutoTranslatorLogger.Info ($"sourceLanguageName={_sourceLanguageName}");
            AutoTranslatorLogger.Info ($"targetLanguage={_targetLanguage} (find supported values here: https://developers.deepl.com/docs/getting-started/supported-languages#translation-target-languages)");
            AutoTranslatorLogger.Info ($"targetLanguageName={_targetLanguageName}");
            AutoTranslatorLogger.Info ($"translate={_translate}");
            AutoTranslatorLogger.Info ($"translatorProvider={_translatorProvider}");
            AutoTranslatorLogger.Info ($"useLlmApi={_useLlmApi}");

            AutoTranslatorLogger.Info ($"Properties that will not be logged here=deepSeekApiKey, llmPrompt, deepLApiKey, deepLContextDefault, deepLContextActivation, azureKey");

            // Log all properties except for API keys
            if(_useTranslationCache && translationCacheFilePath == null)
            {
                throw new ArgumentNullException("translationCacheFilePath", "Translation cache file path cannot be null. Please provide a valid path.");
            }

            if (useLlmApi) {
                if (string.IsNullOrEmpty (_deepSeekApiKey) || string.IsNullOrEmpty (_llmPrompt)) {
                    AutoTranslatorLogger.Error ("LLM API usage is enabled, but DeepSeekApiKey or LlmPrompt is null or empty. Please provide valid values.");
                    throw new System.Exception ("LLM API usage is enabled, but DeepSeekApiKey or LlmPrompt is null or empty. Please provide valid values.");
                }
            }
            if (translate) {
                if (string.IsNullOrEmpty (_deepLApiKey)) {
                    AutoTranslatorLogger.Error ("Translation is enabled, but DeepLApiKey is null or empty. Please provide a valid DeepL API key.");
                    throw new System.Exception ("Translation is enabled, but DeepLApiKey is null or empty. Please provide a valid DeepL API key.");
                }
            }

            csvTool = new CsvTool (null, _csvOutputFileDelimiter);

            _translationCacheManager = new TranslationCacheManager(
                csvTool,
                _translationCacheFilePath,
                cacheFileName,
                _csvOutputFileDelimiter
            );

            // Update DeepL glossary if required
            if (_translate) {
                if (_translatorProvider == TranslatorConstants.ApiNameDeepL) {
                    if (_deepLApiUpdateGlossary) {
                        // Path to your glossary CSV
                        if (_glossaryFilePath != null) {
                            List<KeyValuePair<string, string>> glossaryEntries = csvTool.GetLanguagePairSourceAndTragetLanguage (_glossaryFilePath, _sourceLanguageName, _targetLanguageName);
                            _deepLGlossaryId = DeepLTranslator.UpdateGlossaryAsync (_deepLApiKey, _sourceLanguage, _targetLanguage, glossaryEntries).GetAwaiter ().GetResult ();
                        }
                    } else {
                        _deepLGlossaryId = DeepLTranslator.GetGlossary (_deepLApiKey).GetAwaiter ().GetResult ();
                    }

                }
            }
        }

        public void CreateTranslatedFiles () {
            if (_inputFileName.StartsWith ("*")) {
                string extension = Path.GetExtension (_inputFileName);
                if (string.IsNullOrEmpty (extension)) {
                    AutoTranslatorLogger.Error ("Input file name does not contain a valid extension.");
                    return;
                }

                string[] files = Directory.GetFiles (_inputPath, "*" + extension);
                foreach (var file in files) {
                    string fileName = Path.GetFileName (file);
                    CreateTranslatedFile (_inputPath, fileName);
                }
            } else {
                CreateTranslatedFile (_inputPath, _inputFileName);
            }
        }

        private void CreateTranslatedFile (string inputPath, string inputFile) {
            AutoTranslatorLogger.Info ($"Start translating file {inputFile}");
            List<ValkyrieLanguageData> languageData = csvTool.GetFileLanguageData (inputPath, inputFile, false);

            List<ValkyrieLanguageData> list = new List<ValkyrieLanguageData> ();
            foreach (ValkyrieLanguageData languageDataSingle in languageData) {
                TranslateData (list, languageDataSingle);
            }
            GenerateTranslatedFile (list, _outputPath, inputFile, _outputFileNameAdditionalPart, _csvOutputFileDelimiter);

            //Only save cache if any real changes were done
            if (_useTranslationCache && (_translate || _useLlmApi)) {
                _translationCacheManager.SaveCache();
            }

            AutoTranslatorLogger.Info ($"Finished translating file {inputFile}");
        }

        private void GenerateTranslatedFile (List<ValkyrieLanguageData> translatedData, string path, string fileNameOld, string fileNameNewAdditionalPart, string delimiter) {
            CsvTool csvTool = new CsvTool (null, delimiter);
            csvTool.CreateCsvFile (path, fileNameOld, fileNameNewAdditionalPart, null, translatedData, false, delimiter);
        }

        private void TranslateData (List<ValkyrieLanguageData> newData, ValkyrieLanguageData data) {
            data.Value = TranslateText (data.Key, data.Value);
            newData.Add (data);
        }

        private string TranslateText (string key, string value) {
            if (string.IsNullOrWhiteSpace (value)) {
                return value;
            }

            if (value.Equals (_sourceLanguageName)) {
                return _targetLanguageName;
            }

            string[] skipValues = new string[] {
                "quest.authors",
                "quest.authors_short"
            };

            //skip certain keys from translation
            if (skipValues.Any (s => s.Equals (key))) {
                AutoTranslatorLogger.Info ($"Skipping translation for key: {key}");
                return value;
            }

            // Handle surrounding markers like ||| or "
            string prefix = "";
            string suffix = "";
            string coreValue = value;

            if (coreValue.StartsWith ("|||") && coreValue.EndsWith ("|||")) {
                prefix = "|||";
                suffix = "|||";
                coreValue = coreValue.Substring (3, coreValue.Length - 6);
            } else if (AutoTranslatorHelpers.IsEncapsulatedWithQuotes (coreValue)) {
                prefix = coreValue.Substring (0, 1);
                suffix = coreValue.Substring (coreValue.Length - 1, 1);
                coreValue = coreValue.Substring (1, coreValue.Length - 2);
            }

            var sentences = SplitIntoSentences (coreValue);
            var translatedSentences = new List<string> ();

            foreach (var sentence in sentences) {
                var translatedString = TranslateSentence (key, sentence);
                translatedSentences.Add (translatedString);
            }

            // Build the final string with smarter joining to avoid extra spaces around HTML tags.
            var sb = new System.Text.StringBuilder ();
            for (int i = 0; i < translatedSentences.Count; i++) {
                string current = translatedSentences[i];
                sb.Append (current);

                // Add a space if the current part and the next part are both actual text (not tags or just whitespace).
                if (i < translatedSentences.Count - 1) {
                    string next = translatedSentences[i + 1];
                    bool currentIsText = !current.StartsWith ("<") && !current.EndsWith (">");
                    bool nextIsText = !next.StartsWith ("<") && !next.EndsWith (">");
                    if (currentIsText && nextIsText) sb.Append (" ");
                }
            }
            string finalTranslatedValue = sb.ToString ();

            string combinedFinal = prefix + finalTranslatedValue + suffix;
            combinedFinal = AutoTranslatorHelpers.ReplaceDoubleQuotesWithPipes (combinedFinal);
            combinedFinal = AutoTranslatorHelpers.EnsureStartWithThreePipes (combinedFinal, _targetLanguage);
            combinedFinal = AutoTranslatorHelpers.EnsureStartWithPipesAlsoEndsWithPipes (combinedFinal, _targetLanguage);
            combinedFinal = AutoTranslatorHelpers.ReplaceWhiteSpacesBetweenNewlines (combinedFinal);
            return combinedFinal;
        }

        private List<string> SplitIntoSentences (string text) {
            if (string.IsNullOrEmpty (text))
                return new List<string> ();

            // Regex to split text into sentences. It splits after sentence-ending punctuation (. ! ?)
            // followed by whitespace, or after one or more newlines (\n). It also handles splitting
            // around HTML tags and captures delimiters like newlines.
            // This will split the string, keeping the delimiters as separate entries in the resulting array.
            var regex = new System.Text.RegularExpressions.Regex (@"(\s*\\n\s*|(?<=[.!?])\s+|<i>|</i>|<b>|</b>)");
            var sentences = regex.Split (text).Where (s => !string.IsNullOrWhiteSpace (s)).ToList ();

            return sentences;
        }

        private string TranslateSentence (string key, string value) {
            string valueBefore = value;

            // If the "sentence" is just whitespace or newlines, don't translate it.
            if (string.IsNullOrWhiteSpace (value)) {
                return value;
            }

            // Regex to check if the string contains any letters.
            // If not, it's likely just punctuation, numbers, or special characters (like \n)
            // and should not be translated. This also handles HTML tags and escaped newlines.
            string trimmedValue = value.Trim ();

            // Create a test string where known non-translatable tokens are removed.
            string testValue = trimmedValue;
            testValue = System.Text.RegularExpressions.Regex.Replace (testValue, @"\\n", ""); // Remove \n sequences
            testValue = System.Text.RegularExpressions.Regex.Replace (testValue, @"</?[ib]>", ""); // Remove <i>, <b> tags

            // A string is translatable if it still contains letters after removing formatting tokens.
            bool containsRealText = System.Text.RegularExpressions.Regex.IsMatch (testValue, @"[a-zA-Z]");

            if (!containsRealText) {
                AutoTranslatorLogger.Info ($"Skipping translation for formatting or non-alphabetic sentence: {value}");
                return value;
            }

            var curlyBracketWords = AutoTranslatorHelpers.IdentifyWordsInCurlyBrackets (value);

            string translatedValue = value;
            bool errorOccurred = false;

            //use cached value if available
            if (_useTranslationCache && _translationCacheManager.TryGetTranslation(value, out var cachedTranslation))
            {
                translatedValue = cachedTranslation;
                AutoTranslatorLogger.Info($"Using cached value for: {value}");
            }
            //if not cached, user the translator API
            else {
                if (_translate) {
                    value = AutoTranslatorHelpers.AddWhiteSpaceForLineBreaks (value);
                    value = AutoTranslatorHelpers.AddNoTranslationTagForQuotationMarks (value, _translatorProvider);
                    value = AutoTranslatorHelpers.MarkKeepTags (value);

                    if (_translatorProvider == TranslatorConstants.ApiNameDeepL) {
                        AutoTranslatorLogger.Info ($"Start using DeepL translator for sentence: {value}");
                        var tuple = DeepLTranslator.Translate (_deepLApiMode, key, value, _sourceLanguage, _targetLanguage, _deepLApiKey, _deepLGlossaryId, _deepLContextDefault, _deepLContextActivation, _deepLFormality).GetAwaiter ().GetResult ();
                        if (tuple.Item2) // if error occurred, use original text
                        {
                            errorOccurred = true;
                        } else {
                            translatedValue = tuple.Item1;
                        }
                        AutoTranslatorLogger.Success ($"Finished using DeepL translator for sentence: {value}");
                    } else {
                        AutoTranslatorLogger.Error("Only DeepL translator is supported at the moment.");
                    }
                }

                translatedValue = AutoTranslatorHelpers.RemoveKeepTags (translatedValue);
                translatedValue = AutoTranslatorHelpers.RevertNoTranslationTags (translatedValue);

                // Check if the translated value contains more than one word before calling the LLM.
                bool isSingleWord = !translatedValue.Trim ().Contains (" ");

                if (_useLlmApi && !isSingleWord) {
                    AutoTranslatorLogger.Info ($"Start using DeepSeek LLM for sentence: {translatedValue}");
                    var llmResult = DeepSeekApi.ExecutePromptAsync (_deepSeekApiKey, _llmPrompt, key, translatedValue).GetAwaiter ().GetResult ();
                    if (llmResult.Item2) // if error occurred, use original text
                    {
                        errorOccurred = true;
                    }
                    translatedValue = llmResult.Item1;
                    AutoTranslatorLogger.Success ($"Finished using DeepSeek LLM for sentence: {value}");
                } else if (_useLlmApi && isSingleWord) {
                    AutoTranslatorLogger.Info ($"Skipping LLM for single-word sentence: {translatedValue}");
                }
            }

            // Remove <keep> tags after translation
            translatedValue = AutoTranslatorHelpers.FindAndReplacedTranslatedCurlyBracketsWords (translatedValue, curlyBracketWords);
            translatedValue = AutoTranslatorHelpers.ReplaceQuotesWithEnglishspecialCharacterQuotation (translatedValue, _targetLanguage);
            translatedValue = AutoTranslatorHelpers.ReplaceBackslashNotFollowedByNWithLineBreak (translatedValue);
            if (_translatorProvider != TranslatorConstants.ApiNameDeepL) {
                translatedValue = AutoTranslatorHelpers.ReplaceDeepLSpecialGlossaryChar (translatedValue);
            }

            if (_translatorProvider != TranslatorConstants.ApiNameDeepL) {
                translatedValue = AutoTranslatorHelpers.ReplaceLineBreaksWithOldValue (translatedValue);
            }

            if (!errorOccurred) {
                _translationCacheManager.AddTranslation(valueBefore, translatedValue);
            }

            AutoTranslatorLogger.Success ($"Finished all operations for sentence: {value}");
            return translatedValue;
        }
    }
}