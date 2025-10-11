using Valkyrie.AutoTranslator.Helpers;

namespace Valkyrie.AutoTranslator
{
    internal class TranslationCacheManager
    {
        private readonly CsvTool _csvTool;
        private readonly string _cacheFilePath;
        private readonly string _cacheFileName;
        private readonly string _delimiter;
        private HashSet<KeyValuePair<string, string>> _cache;

        public TranslationCacheManager(CsvTool csvTool, string cacheFilePath, string cacheFileName, string delimiter)
        {
            _csvTool = csvTool;
            _cacheFilePath = cacheFilePath;
            _cacheFileName = cacheFileName;
            _delimiter = delimiter;
            LoadCache();
        }

        private void LoadCache()
        {
            if (!string.IsNullOrWhiteSpace(_cacheFilePath))
            {
                _cache = TranslationCacheHelper.LoadTranslationCache(_csvTool, _cacheFilePath, _cacheFileName, _delimiter);
            }
            else
            {
                _cache = new HashSet<KeyValuePair<string, string>>();
            }
        }

        public bool TryGetTranslation(string value, out string translation)
        {
            // Normalize both the input and cache keys for comparison
            string normalizedValue = value?.Trim();
            var cached = _cache.FirstOrDefault(c => 
                string.Equals(c.Key?.Trim(), normalizedValue, System.StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(cached.Value))
            {
                translation = cached.Value;
                return true;
            }
            translation = null;
            return false;
        }

        public void AddTranslation(string key, string value)
        {
            _cache.Add(new KeyValuePair<string, string>(key, value));
        }

        public void SaveCache()
        {
            TranslationCacheHelper.SaveTranslationCache(_csvTool, _cacheFilePath, _cacheFileName, _delimiter, _cache);
        }

        public IEnumerable<KeyValuePair<string, string>> GetAllTranslations()
        {
            return _cache;
        }
    }
}