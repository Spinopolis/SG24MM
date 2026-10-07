using System.IO;
using System.Text.Json;
using SG24MM.Models;

namespace SG24MM.Services
{
    public class ConfigService
    {
        private readonly string _configPath;

        public ConfigService()
        {
            string appFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SG24MM"
            );

            Directory.CreateDirectory(appFolder);

            _configPath = Path.Combine(appFolder, "config.json");
        }

        public void SaveSettings(AppSettings settings)
        {
            string json = JsonSerializer.Serialize(
                settings,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                }
            );

            File.WriteAllText(_configPath, json);
        }

        public AppSettings LoadSettings()
        {
            if (!File.Exists(_configPath))
            {
                return new AppSettings();
            }

            string json = File.ReadAllText(_configPath);

            return JsonSerializer.Deserialize<AppSettings>(json)
                   ?? new AppSettings();
        }
    }
}