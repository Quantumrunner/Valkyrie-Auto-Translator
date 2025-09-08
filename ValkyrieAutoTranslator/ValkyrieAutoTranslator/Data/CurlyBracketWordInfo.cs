namespace Valkyrie.AutoTranslator.Data
{
    internal class CurlyBracketWordInfo
    {
        public int Index { get; set; } // 1-based index of the word in curly brackets
        public string Word { get; set; } // The word inside the curly brackets
        public int StartPosition { get; set; } // Start index in the string
        public int EndPosition { get; set; } // End index in the string (exclusive)
    }
}
