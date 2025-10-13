using Microsoft.Extensions.Configuration;
using ValkyrieAutoTranslator.Data;

namespace Valkyrie.AutoTranslator
{
    public class Program
    {
        static void Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddUserSecrets<Program>(optional: true)
                .Build();

            var autoTranslatorConfig = config.GetSection(nameof(AutoTranslatorConfig)).Get<AutoTranslatorConfig>() 
                ?? config.Get<AutoTranslatorConfig>();

            AutoTranslator autoTranslator = new AutoTranslator(autoTranslatorConfig);
            autoTranslator.CreateTranslatedFiles();
        }
    }
}