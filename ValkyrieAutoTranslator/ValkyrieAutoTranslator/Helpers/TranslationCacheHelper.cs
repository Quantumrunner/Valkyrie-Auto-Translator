namespace Valkyrie.AutoTranslator.Helpers
{
    internal static class TranslationCacheHelper
    {
        public static HashSet<KeyValuePair<string, string>> LoadTranslationCache(
            CsvTool csvTool,
            string translationCacheFilePath,
            string translationCacheFileName,
            string csvOutputFileDelimiter)
        {
            var translationCache = new HashSet<KeyValuePair<string, string>>();
            if (!string.IsNullOrWhiteSpace(translationCacheFilePath) && !string.IsNullOrWhiteSpace(translationCacheFilePath))
            {
                try
                {
                    // Ensure the directory exists before proceeding
                    if (!Directory.Exists(translationCacheFilePath))
                    {
                        AutoTranslatorLogger.Error($"Translation cache directory does not exist: {translationCacheFilePath}");
                        return translationCache;
                    }                    

                    string combinedPath = Path.Combine(translationCacheFilePath, translationCacheFileName);
                    if (!File.Exists(combinedPath))
                    {
                        // Create an empty file if it does not exist
                        using (File.Create(combinedPath)) { }
                    }

                    var cacheEntriesFromFile = csvTool.GetCsvTranslationData(combinedPath, csvOutputFileDelimiter);
                    foreach (var entry in cacheEntriesFromFile)
                    {
                        if (!string.IsNullOrWhiteSpace(entry.Key) && !string.IsNullOrWhiteSpace(entry.Value))
                        {
                            translationCache.Add(new KeyValuePair<string, string>(entry.Key, entry.Value));
                        }
                    }
                    AutoTranslatorLogger.Info($"Loaded {translationCache.Count} entries from translation cache file: {combinedPath}");
                }
                catch (Exception ex)
                {
                    AutoTranslatorLogger.Error($"Failed to load translation cache from file: {translationCacheFilePath}. Exception: {ex.Message}");
                }
            }
            return translationCache;
        }
        public static void SaveTranslationCache(
            CsvTool csvTool,
            string translationCacheFilePath,
            string translationCacheFileName,
            string csvOutputFileDelimiter,
            IEnumerable<KeyValuePair<string, string>> translationCache)
        {
            if (string.IsNullOrWhiteSpace(translationCacheFilePath) || translationCache == null)
            {
                AutoTranslatorLogger.Error("Translation cache file path or cache is null or empty. Cannot save translation cache.");
                return;
            }
            if (string.IsNullOrWhiteSpace(translationCacheFileName) || translationCache == null)
            {
                AutoTranslatorLogger.Error("Translation cache file name or cache is null or empty. Cannot save translation cache.");
                return;
            }

            try
            {
                var data = new List<ValkyrieLanguageData>();
                foreach (var entry in translationCache)
                {
                    if (!string.IsNullOrWhiteSpace(entry.Key) && !string.IsNullOrWhiteSpace(entry.Value))
                    {
                        data.Add(new ValkyrieLanguageData { Key = entry.Key, Value = entry.Value });
                    }
                }

                List<string> headers = new List<string> { "Key", "Value" };
                csvTool.CreateCsvFile(translationCacheFilePath, translationCacheFileName, string.Empty, headers, data, true, csvOutputFileDelimiter);
                AutoTranslatorLogger.Info($"Saved {data.Count} entries to translation cache file: {translationCacheFilePath}");
            }
            catch (Exception ex)
            {
                AutoTranslatorLogger.Error($"Failed to save translation cache to file: {translationCacheFilePath}. Exception: {ex.Message}");
            }
        }

    }
}
