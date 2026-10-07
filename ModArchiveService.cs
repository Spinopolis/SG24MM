using System.IO;
using System.IO.Compression;
using SG24MM.Models;

namespace SG24MM.Services
{
    public class ModArchiveService
    {
        public Mod ReadMod(string zipPath)
        {
            Mod mod = new Mod
            {
                ZipPath = zipPath,
                Name = Path.GetFileNameWithoutExtension(zipPath)
            };

            using ZipArchive archive = ZipFile.OpenRead(zipPath);

            const string rawMarker =
                "image/x64/generations/raw/";

            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                // Ignore folders.
                if (string.IsNullOrWhiteSpace(entry.Name))
                {
                    continue;
                }

                string archivePath = entry.FullName
                    .Replace('\\', '/')
                    .TrimStart('/');

                // Find the Sonic Generations 2024 raw folder anywhere
                // inside the ZIP. This supports mods with extra
                // folders before image/x64/generations/raw.
                int rawIndex = archivePath.IndexOf(
                    rawMarker,
                    StringComparison.OrdinalIgnoreCase
                );

                if (rawIndex < 0)
                {
                    continue;
                }

                // Get the path starting at image/x64/generations/raw.
                string gameArchivePath = archivePath[rawIndex..];

                if (string.IsNullOrWhiteSpace(gameArchivePath))
                {
                    continue;
                }

                // Prevent duplicate entries.
                bool alreadyAdded = mod.Files.Any(
                    existing =>
                        string.Equals(
                            existing.ArchivePath,
                            gameArchivePath,
                            StringComparison.OrdinalIgnoreCase
                        )
                );

                if (alreadyAdded)
                {
                    continue;
                }

                ModFile modFile = new ModFile
                {
                    ArchivePath = gameArchivePath,
                    GamePath = gameArchivePath.Replace(
                        '/',
                        Path.DirectorySeparatorChar
                    )
                };

                mod.Files.Add(modFile);
            }

            return mod;
        }

        public Mod ReadExtractedMod(string modFolder)
        {
            if (!Directory.Exists(modFolder))
            {
                throw new DirectoryNotFoundException(
                    $"Mod folder not found: {modFolder}"
                );
            }

            Mod mod = new Mod
            {
                Name = new DirectoryInfo(modFolder).Name,
                InstallPath = modFolder
            };

            string rawFolder = FindRawFolder(modFolder);

            if (string.IsNullOrEmpty(rawFolder))
            {
                throw new DirectoryNotFoundException(
                    "Could not find the Sonic Generations 2024 image\\x64\\generations\\raw folder."
                );
            }

            // Find every actual file inside the raw folder.
            string[] files = Directory.GetFiles(
                rawFolder,
                "*",
                SearchOption.AllDirectories
            );

            foreach (string filePath in files)
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    continue;
                }

                // Get the file path relative to the raw folder.
                string relativePath = Path.GetRelativePath(
                    rawFolder,
                    filePath
                );

                string normalizedPath = relativePath
                    .Replace('\\', '/')
                    .TrimStart('/');

                if (string.IsNullOrWhiteSpace(normalizedPath))
                {
                    continue;
                }

                // Build the actual Sonic Generations game path.
                string gamePath =
                    "image/x64/generations/raw/" +
                    normalizedPath;

                // Prevent duplicate entries.
                bool alreadyAdded = mod.Files.Any(
                    existing =>
                        string.Equals(
                            existing.GamePath,
                            gamePath.Replace(
                                '/',
                                Path.DirectorySeparatorChar
                            ),
                            StringComparison.OrdinalIgnoreCase
                        )
                );

                if (alreadyAdded)
                {
                    continue;
                }

                ModFile modFile = new ModFile
                {
                    ArchivePath = gamePath,
                    GamePath = gamePath.Replace(
                        '/',
                        Path.DirectorySeparatorChar
                    )
                };

                mod.Files.Add(modFile);
            }

            return mod;
        }

        private string FindRawFolder(string modFolder)
        {
            const string rawMarker =
                "/image/x64/generations/raw";

            // Check for the standard structure directly first.
            string directRawFolder = Path.Combine(
                modFolder,
                "image",
                "x64",
                "generations",
                "raw"
            );

            if (Directory.Exists(directRawFolder))
            {
                return directRawFolder;
            }

            // Search through nested folders.
            string[] rawFolders = Directory.GetDirectories(
                modFolder,
                "raw",
                SearchOption.AllDirectories
            );

            foreach (string rawFolder in rawFolders)
            {
                string normalized = rawFolder
                    .Replace('\\', '/')
                    .TrimEnd('/');

                // Support extra folders before image/x64/generations/raw.
                if (normalized.EndsWith(
                    rawMarker,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return rawFolder;
                }
            }

            return string.Empty;
        }
    }
}