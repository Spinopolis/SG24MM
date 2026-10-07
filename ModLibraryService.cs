using SG24MM.Models;
using System.IO;
using System.Text.Json;

namespace SG24MM.Services
{
    public class ModLibraryService
    {
        private readonly string _libraryPath;

        public ModLibraryService()
        {
            string appFolder = Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData),
                "SG24MM"
            );

            Directory.CreateDirectory(appFolder);

            _libraryPath = Path.Combine(
                appFolder,
                "mods.json"
            );
        }

        public List<Mod> LoadMods()
        {
            if (!File.Exists(_libraryPath))
            {
                return new List<Mod>();
            }

            string json = File.ReadAllText(_libraryPath);

            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<Mod>();
            }

            return JsonSerializer.Deserialize<List<Mod>>(json)
                   ?? new List<Mod>();
        }

        public void SaveMods(List<Mod> mods)
        {
            string json = JsonSerializer.Serialize(
                mods,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                }
            );

            File.WriteAllText(
                _libraryPath,
                json
            );
        }
    }
}