using CsvHelper;
using CsvHelper.Configuration;
using System.Text;
using Valkyrie.AutoTranslator.Helpers;

namespace Valkyrie.AutoTranslator
{
    internal class CsvTool
    {
        private readonly string _delimiter = ",";

        public CsvTool(string[] commentTokens, string delimiter)
        {
            _delimiter = delimiter;
        }

        public List<ValkyrieLanguageData> GetFileLanguageData(string path, string fileName, bool useCsvReader)
        {
            string combinedPath = Path.Combine(path, fileName);

            using (StreamReader sr = new StreamReader(combinedPath))
            {
                if(useCsvReader)
                {
                    string headerString = $"Key{_delimiter}Value";
                    string filetext = File.ReadAllText(path);
                    if (!filetext.StartsWith(headerString))
                    {
                        filetext = headerString + "\n" + filetext;
                    }

                    var config = new CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)
                    {
                        Delimiter = _delimiter,
                        BadDataFound = context =>
                        {
                            AutoTranslatorLogger.Error($"Bad data found on row {context.RawRecord}: {context.RawRecord}");
                        },
                    };

                    using (TextReader tr = new StringReader(filetext))
                    {
                        var reader = new CsvReader(tr, config);
                        var good = new List<ValkyrieLanguageData>();
                        var bad = new List<string>();
                        var isRecordBad = false;
                        while (reader.Read())
                        {
                            var record = reader.GetRecord<ValkyrieLanguageData>();
                            if (!isRecordBad)
                            {
                                good.Add(record);
                            }

                            isRecordBad = false;
                        }

                        if(bad.Count > 0)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            AutoTranslatorLogger.Info($"The following lines are incorrect: {bad.ToString()}");
                            AutoTranslatorLogger.Info($"These lines will not be added to the output file!");
                            Console.ResetColor();
                        }
                        
                        return good;
                    }
                }
                else
                {
                    var list = new List<ValkyrieLanguageData>();
                    var lines = File.ReadAllLines(combinedPath);
                    foreach(var line in lines)
                    {
                        string[] actualValue = line.Split(new char[] { ',' }, 2);
                        if(actualValue.Length < 2)
                        {
                            throw new Exception($"Line {line} is not valid. Please ensure the key and the value is split by a comma (e.g. quest.name,The Shadow Rune - Act I - Death On The Wing I)");
                        }
                        var data = new ValkyrieLanguageData { Key = actualValue[0], Value = actualValue[1] };
                        list.Add(data);
                    }

                    return list;
                }
                       
            }
        }

        internal List<ValkyrieTranslationData> GetCsvTranslationData(string path, string delimiter)
        {
            using (StreamReader sr = new StreamReader(path))
            {
                var config = new CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)
                {
                    Delimiter = delimiter
                };
                var reader = new CsvReader(sr, config);
                var records = reader.GetRecords<ValkyrieTranslationData>();

                return records.ToList();
            }
        }

        public void CreateCsvFile(string outputPath, string filenameOld, string fileNameNewAdditionalPart, ICollection<string> headers, ICollection<ValkyrieLanguageData> data, bool quoteAllFields, string delimiter = null)
        {
            if (string.IsNullOrWhiteSpace(delimiter))
            {
                delimiter = _delimiter;
            }

            string filenameWithoutExt = Path.GetFileNameWithoutExtension(filenameOld);
            string extension = Path.GetExtension(filenameOld);
            string combinedFilename = filenameWithoutExt + fileNameNewAdditionalPart + extension;
            string combinedPath = Path.Combine(outputPath, combinedFilename);

            var config = new CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)
            {
                ShouldQuote = args => quoteAllFields,
                Delimiter = delimiter,
            };

            using (var writer = new StreamWriter(combinedPath, false, Encoding.UTF8))
            using (var csv = new CsvWriter(writer, config))
            {

                if (headers != null && headers.Count > 0)
                {
                    foreach (var header in headers)
                    {
                        csv.WriteField(header);
                    }
                    csv.NextRecord();
                }

                int count = 0;
                foreach (var singleData in data)
                {
                    csv.WriteField(singleData.Key);
                    csv.WriteField(singleData.Value);
                    count++;
                    if (count < data.Count)
                    {
                        csv.NextRecord();
                    }
                }
            }
        }

        internal List<KeyValuePair<string, string>> GetLanguagePairSourceAndTargetLanguage(string glossaryFilePath, string sourceLanguageName, string targetLanguageName)
        {
            var result = new List<KeyValuePair<string, string>>();

            var config = new CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                Delimiter = _delimiter,
            };

            using (var reader = new StreamReader(glossaryFilePath))
            using (var csv = new CsvReader(reader, config))
            {
                var records = csv.GetRecords<dynamic>();
                csv.Read();
                csv.ReadHeader();
                string[] headerRowValues = csv.HeaderRecord;
                int sourceIndex = Array.FindIndex(headerRowValues, h => h.Equals(sourceLanguageName, StringComparison.OrdinalIgnoreCase));
                int targetIndex = Array.FindIndex(headerRowValues, h => h.Equals(targetLanguageName, StringComparison.OrdinalIgnoreCase));

                if (sourceIndex == -1 || targetIndex == -1)
                    throw new Exception($"Source or target language column not found. Source: {sourceLanguageName}, Target: {targetLanguageName}");

                foreach (var record in records)
                {
                    var dict = (IDictionary<string, object>)record;
                    var sourceValue = dict.ContainsKey(sourceLanguageName) ? dict[sourceLanguageName]?.ToString() : null;
                    var targetValue = dict.ContainsKey(targetLanguageName) ? dict[targetLanguageName]?.ToString() : null;
                    if (!string.IsNullOrEmpty(sourceValue) && !string.IsNullOrEmpty(targetValue))
                    {
                        result.Add(new KeyValuePair<string, string>(sourceValue, targetValue));
                    }
                }
            }
            return result;
        }
    }
}
