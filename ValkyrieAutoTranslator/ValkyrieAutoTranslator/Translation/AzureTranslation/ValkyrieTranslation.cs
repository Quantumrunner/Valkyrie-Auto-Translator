namespace Valkyrie.AutoTranslator.AzureTranslation
{
    public class ValkyrieTranslation
    {
        public string before { get; set; }
        public string after { get; set; }
    }

    public class ValkyrieTranslationObject
    {
        public int id { get; set; }
        public List<ValkyrieTranslation> valkyrieTranslations { get; set; }
    }
}
