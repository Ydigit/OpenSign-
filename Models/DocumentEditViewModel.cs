namespace OpenSign.Models
{
    public class Placeholder
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // "multiple_choice" ou "free_text"
        public List<string> Options { get; set; } = new List<string>();
    }

    public class DocumentEditViewModel
    {
        public string DocumentText { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string EncryptionMode { get; set; } = string.Empty;
        public List<Placeholder> Placeholders { get; set; } = new List<Placeholder>();
    }
}
