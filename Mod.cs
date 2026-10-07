
namespace SG24MM.Models
{
    public class Mod
    {
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string ZipPath { get; set; } = string.Empty;

        public string InstallPath { get; set; } = string.Empty;

        public bool IsEnabled { get; set; } = false;

        public List<ModFile> Files { get; set; } = new();
    }
}

