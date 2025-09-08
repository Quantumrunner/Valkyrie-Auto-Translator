namespace Valkyrie.AutoTranslator.Helpers
{
    internal static class AutoTranslatorLogger  
    {
        private static string GetTimestamp()
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        }

        internal static void Info(string message)
        {
            Console.WriteLine($"[{GetTimestamp()}] {message}");
        }

        internal static void Success(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[{GetTimestamp()}] Success: {message}");
            Console.ResetColor();
        }


        internal static void Error(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[{GetTimestamp()}] Error: {message}");
            Console.ResetColor();
        }

        internal static void Warning(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[{GetTimestamp()}] Warning: {message}");
            Console.ResetColor();
        }

        internal static void Debug(string message)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[{GetTimestamp()}] Debug: {message}");
            Console.ResetColor();
        }
    }
}
