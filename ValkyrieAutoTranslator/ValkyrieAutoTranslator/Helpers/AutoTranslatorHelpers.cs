using System.Text.RegularExpressions;
using Valkyrie.AutoTranslator.Data;

namespace Valkyrie.AutoTranslator
{
    internal static class AutoTranslatorHelpers
    {

        private const string lineBreakTagInternalDouble = "ValkyrieTranslationLineBreakTagDouble";
        private const string lineBreakTagInternalSingle = "ValkyrieTranslationLineBreakTagSingle";
        private static string lineBreakReplaceDouble = $"<div class='notranslate'>{lineBreakTagInternalDouble}</div>";
        private static string lineBreakReplaceSingle = $"<div class='notranslate'>{lineBreakTagInternalSingle}</div>";


        internal static string ReplaceLineBreaksWithNoTranslationTag(string value)
        {
            string lineBreakDouble = @"\n\n";
            string lineBreakSingle = @"\n";
            if (value.Contains(lineBreakDouble))
            {
                value = value.Replace(lineBreakDouble, lineBreakReplaceDouble);
            }
            if (value.Contains(lineBreakSingle))
            {
                value = value.Replace(lineBreakSingle, lineBreakReplaceSingle);
            }

            return value;
        }

        internal static string AddNoTranslationTagForWordsWithCurlyBrackets(string value, string translatorProvider)
        {
            var wordsInBrakets = AutoTranslatorHelpers.GetAllWordsInBrackets(value);
            List<string> replacedWords = new List<string>();
            foreach (var word in wordsInBrakets)
            {
                string wordWithBrackets = $"{{{word}}}";
                value = AutoTranslatorHelpers.AddNoTranslationTag(value, wordWithBrackets, wordWithBrackets, ref replacedWords, translatorProvider);
            }

            if (translatorProvider == TranslatorConstants.ApiNameAzure)
            {
                value = AutoTranslatorHelpers.ReplaceLineBreaksWithNoTranslationTag(value);
            }

            return value;
        }

        internal static string AddNoTranslationTagForQuotationMarks(string value, string translatorProvider)
        {
            // Only match double quote characters, not the content inside
            var matches = Regex.Matches(value, "\"");
            List<string> replacedWords = new List<string>();

            foreach (Match match in matches)
            {
                string quote = match.Value;
                // Only replace the quote character itself with the no-translate tag
                value = AutoTranslatorHelpers.AddNoTranslationTag(value, quote, quote, ref replacedWords, translatorProvider);
            }

            return value;
        }


        internal static string ReplaceLineBreaksWithOldValue(string value)
        {
            string lineBreakDouble = @"\n\n";
            string lineBreakSingle = @"\n";
            if (value.Contains(AutoTranslatorHelpers.lineBreakReplaceDouble))
            {
                value = value.Replace(AutoTranslatorHelpers.lineBreakReplaceDouble, lineBreakDouble);
            }
            if (value.Contains(AutoTranslatorHelpers.lineBreakReplaceSingle))
            {
                value = value.Replace(AutoTranslatorHelpers.lineBreakReplaceSingle, lineBreakSingle);
            }

            return value;
        }

        internal static string AddNoTranslationTag(string stringValue, string key, string value, ref List<string> replacedWords, string translatorProvider)
        {
            if (replacedWords.Contains(value))
            {
                return stringValue;
            }

            if (replacedWords != null)
            {
                replacedWords.Add(key);
            }

            string newValue;
            if (translatorProvider == TranslatorConstants.ApiNameDeepL)
            {
                // DeepL ignores content inside <span translate="no">...</span>
                newValue = stringValue.Replace(key, $"<keep>{value}</keep>");
            }
            else
            {
                newValue = stringValue.Replace(key, $"<mstrans:dictionary translation=\"{value}\">{key}</mstrans:dictionary>");
            }

            return newValue;
        }

        internal static List<string> GetAllWordsInBrackets(string value)
        {
            var matches = Regex.Matches(value, @"\{(.+?)\}");

            var results = (from Match m in matches select m.Groups[1].ToString()).ToList();
            return results;
        }

        internal static string RevertNoTranslationTags(string value)
        {
            // Remove DeepL <keep> tags, restoring the original value inside (including quotes)
            value = Regex.Replace(value, @"<keep>(.*?)</keep>", "$1");

            // Remove Azure <mstrans:dictionary> tags, restoring the original value inside
            value = Regex.Replace(value, @"<mstrans:dictionary[^>]*>(.*?)</mstrans:dictionary>", "$1");

            return value;
        }
        /// <summary>
        /// Checks if the input string is encapsulated with matching single or double quotes.
        /// Returns true if the string starts and ends with the same quote character.
        /// </summary>
        /// <param name="value">The string to check.</param>
        /// <returns>True if encapsulated with quotes, otherwise false.</returns>
        internal static bool IsEncapsulatedWithQuotes(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length < 2)
                return false;

            char first = value[0];
            char last = value[value.Length - 1];

            return (first == last) && (first == '"' || first == '\'');
        }

        internal static string ReplaceDoubleQuotesWithPipes(string value)
        {
           if (string.IsNullOrEmpty(value))
                return value;

            // Check if the string starts and ends with quotes
            if (value.StartsWith("\"") && value.EndsWith("\""))
            {
                // Remove the first and last quote, then wrap with pipes
                return "|||" + value.Substring(1, value.Length - 2) + "|||";
            }

            return value;
        }

        internal static string AddWhiteSpaceForLineBreaks(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            // Add a space after any \n that is immediately followed by a non-whitespace character
            // Handles both single and double line breaks
            value = Regex.Replace(value, @"(\\n)([^\s\\])", "$1 $2");

            return value;
        }

        internal static List<CurlyBracketWordInfo> IdentifyWordsInCurlyBrackets(string value)
        {
            var result = new List<CurlyBracketWordInfo>();
            if (string.IsNullOrEmpty(value))
                return result;

            var matches = Regex.Matches(value, @"\{(.+?)\}");
            int count = 1;
            foreach (Match match in matches)
            {
                result.Add(new CurlyBracketWordInfo
                {
                    Index = count,
                    Word = match.Groups[1].Value,
                    StartPosition = match.Index,
                    EndPosition = match.Index + match.Length
                });
                count++;
            }
            return result;
        }

        internal static string FindAndReplacedTranslatedCurlyBracketsWords(string translatedValue, List<CurlyBracketWordInfo> curlyBracketWords)
        {
            if (string.IsNullOrEmpty(translatedValue) || curlyBracketWords == null || curlyBracketWords.Count == 0)
                return translatedValue;

            // Find all current curly bracket words in the translated text
            var matches = Regex.Matches(translatedValue, @"\{(.+?)\}");
            if (matches.Count == 0)
                return translatedValue;

            // Replace each found word with the original word from curlyBracketWords by index
            // We assume that the order of the found matches corresponds to the order in curlyBracketWords
            int replaceCount = Math.Min(matches.Count, curlyBracketWords.Count);
            int offset = 0;
            for (int i = 0; i < replaceCount; i++)
            {
                var match = matches[i];
                var originalWord = curlyBracketWords[i].Word;
                // Replace the found word in the translated text with the original word
                // Since we are replacing in the string, we need to adjust the current position with the offset
                int start = match.Index + offset;
                int length = match.Length;
                string replacement = "{" + originalWord + "}";
                translatedValue = translatedValue.Remove(start, length).Insert(start, replacement);
                offset += replacement.Length - length;
            }

            return translatedValue;
        }

        // Replaces straight double quotes around words with language-appropriate quotation marks.
        /// For English: “word”
        /// For German: „word“
        /// </summary>
        /// <param name="translatedValue">The string to process.</param>
        /// <param name="language">The target language code ("en", "de", etc.).</param>
        /// <returns>The string with replaced quotation marks.</returns>
        internal static string ReplaceQuotesWithEnglishspecialCharacterQuotation(string translatedValue, string language)
        {
            if (string.IsNullOrEmpty(translatedValue))
                return translatedValue;

            string openQuote, closeQuote;

            switch (language.ToLowerInvariant())
            {
                case "de":
                    openQuote = "„";
                    closeQuote = "“";
                    break;
                case "fr":
                    openQuote = "«";
                    closeQuote = "»";
                    break;
                case "es":
                    openQuote = "«";
                    closeQuote = "»";
                    break;
                case "it":
                    openQuote = "«";
                    closeQuote = "»";
                    break;
                case "pl":
                    openQuote = "„";
                    closeQuote = "”";
                    break;
                case "ru":
                    openQuote = "«";
                    closeQuote = "»";
                    break;
                case "pt":
                    openQuote = "«";
                    closeQuote = "»";
                    break;
                case "en":
                default:
                    openQuote = "“";
                    closeQuote = "”";
                    break;
            }

            // Replace pairs of straight double quotes around words/phrases
            // Handles: "word", " phrase ", etc.
            translatedValue = Regex.Replace(
                translatedValue,
                "\"([^\"]+?)\"",
                m => openQuote + m.Groups[1].Value + closeQuote);

            return translatedValue;
        }

        // Add <keep> tags around <i> and <b> tags themselves (not their content) to preserve their position
        internal static string MarkKeepTags(string input)
        {
            // Handle <i>...</i>
            input = System.Text.RegularExpressions.Regex.Replace(
                input,
                @"(<i>)(.*?)(</i>)",
                m => $"<keep>{m.Groups[1].Value}</keep>{m.Groups[2].Value}<keep>{m.Groups[3].Value}</keep>",
                System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // Handle <b>...</b>
            input = System.Text.RegularExpressions.Regex.Replace(
                input,
                @"(<b>)(.*?)(</b>)",
                m => $"<keep>{m.Groups[1].Value}</keep>{m.Groups[2].Value}<keep>{m.Groups[3].Value}</keep>",
                System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            return input;
        }

        // Remove <keep> tags after translation
        internal static string RemoveKeepTags(string input)
        {
            return System.Text.RegularExpressions.Regex.Replace(input, @"<\/?keep>", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        internal static string EnsureStartWithThreePipes(string translatedValue, string targetLanguage)
        {
            if (string.IsNullOrEmpty(translatedValue))
                return translatedValue;

            // Count the number of consecutive pipes at the start
            int pipeCount = 0;
            for (int i = 0; i < translatedValue.Length && translatedValue[i] == '|'; i++)
            {
                pipeCount++;
            }

            if (pipeCount == 0)
            {
                // No starting pipes, return as is
                return translatedValue;
            }
            else if (pipeCount == 3)
            {
                // Already three pipes, return as is
                return translatedValue;
            }
            else
            {
                // Remove existing starting pipes and add exactly three
                string rest = translatedValue.Substring(pipeCount);
                return "|||" + rest;
            }
        }

        internal static string EnsureStartWithPipesAlsoEndsWithPipes(string translatedValue, string targetLanguage)
        {
            if (string.IsNullOrEmpty(translatedValue))
                return translatedValue;

            // Check if the string starts with exactly three pipes
            if (translatedValue.StartsWith("|||"))
            {
                // Remove any trailing whitespace for accurate check
                string trimmed = translatedValue.TrimEnd();

                // Check if it already ends with three pipes
                if (!trimmed.EndsWith("|||"))
                {
                    // Append three pipes at the end (preserving original trailing whitespace)
                    int trailingWhitespaceCount = translatedValue.Length - trimmed.Length;
                    string trailingWhitespace = translatedValue.Substring(trimmed.Length, trailingWhitespaceCount);
                    return trimmed + "|||" + trailingWhitespace;
                }
            }
            return translatedValue;
        }

        internal static string ReplaceBackslashNotFollowedByNWithLineBreak(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            // Replace all '\' not followed by 'n' with '\n'
            return Regex.Replace(value, @"\\(?!n)", @"\n");
        }

        internal static string ReplaceWhiteSpacesBetweenNewlines(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            // This regex finds a newline sequence (\n), followed by one or more whitespace characters,
            // followed by another newline sequence, and replaces it with just the two newline sequences.
            // e.g. \n \n becomes \n\n
            return Regex.Replace(value, @"(\\n)\s+(\\n)", "$1$2");
        }

        internal static string ReplaceDeepLSpecialGlossaryChar(string translatedValue)
        {
            if (string.IsNullOrEmpty(translatedValue))
                return translatedValue;

            // DeepLTranslator uses the special char '\uE000' as a glossary placeholder
            // Replace it with the original curly bracket format: {word}
            // The actual replacement logic may depend on how the placeholder is used,
            // but typically, we just remove or replace '\uE000' with nothing or a space.
            // If you want to replace with a specific string, adjust as needed.

            // Replace all occurrences of the special char with a whitespace
            return translatedValue.Replace($"{DeepLTranslator.SpecialGlossaryChar}", " ");
        }
    }
}