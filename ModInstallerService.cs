using System.IO;
using System.IO.Compression;
using SG24MM.Models;

namespace SG24MM.Services
{
    public class ModInstallerService
    {
        public string GetGameFilePath(string gameRoot, ModFile modFile)
        {
            if (Path.IsPathRooted(modFile.GamePath))
            {
                throw new InvalidOperationException(
                    $"Unsafe mod file path: {modFile.GamePath}"
                );
            }

            string fullGameRoot = Path.GetFullPath(gameRoot)
                .TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar
                )
                + Path.DirectorySeparatorChar;

            string fullGameFilePath = Path.GetFullPath(
                Path.Combine(gameRoot, modFile.GamePath)
            );

            if (!fullGameFilePath.StartsWith(
                fullGameRoot,
                StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Unsafe mod file path: {modFile.GamePath}"
                );
            }

            return fullGameFilePath;
        }

        public void InstallMod(
            string gameRoot,
            Mod mod,
            string backupRoot,
            BackupService backupService)
        {
            if (!string.IsNullOrWhiteSpace(mod.ZipPath) &&
                File.Exists(mod.ZipPath))
            {
                InstallFromZip(
                    gameRoot,
                    mod,
                    backupRoot,
                    backupService
                );

                return;
            }

            if (!string.IsNullOrWhiteSpace(mod.InstallPath) &&
                Directory.Exists(mod.InstallPath))
            {
                InstallFromFolder(
                    gameRoot,
                    mod,
                    backupRoot,
                    backupService
                );

                return;
            }

            throw new FileNotFoundException(
                $"Could not find the ZIP or extracted folder for mod '{mod.Name}'."
            );
        }

        private void InstallFromZip(
            string gameRoot,
            Mod mod,
            string backupRoot,
            BackupService backupService)
        {
            using ZipArchive archive =
                ZipFile.OpenRead(mod.ZipPath);

            const string rawMarker =
                "image/x64/generations/raw/";

            foreach (ModFile modFile in mod.Files)
            {
                ZipArchiveEntry? entry =
                    FindZipEntry(
                        archive,
                        modFile.ArchivePath,
                        rawMarker
                    );

                if (entry == null)
                {
                    throw new FileNotFoundException(
                        $"Could not find {modFile.ArchivePath} inside the mod ZIP."
                    );
                }

                string gameFilePath =
                    GetGameFilePath(
                        gameRoot,
                        modFile
                    );

                BackupAndInstall(
                    gameRoot,
                    gameFilePath,
                    modFile.GamePath,
                    backupRoot,
                    backupService,
                    () =>
                    {
                        entry.ExtractToFile(
                            gameFilePath,
                            true
                        );
                    }
                );
            }
        }

        private ZipArchiveEntry? FindZipEntry(
            ZipArchive archive,
            string expectedPath,
            string rawMarker)
        {
            string normalizedExpectedPath =
                NormalizeArchivePath(
                    expectedPath
                );

            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                if (string.IsNullOrWhiteSpace(entry.Name))
                {
                    continue;
                }

                string normalizedEntryPath =
                    NormalizeArchivePath(
                        entry.FullName
                    );

                int rawIndex =
                    normalizedEntryPath.IndexOf(
                        rawMarker,
                        StringComparison.OrdinalIgnoreCase
                    );

                if (rawIndex < 0)
                {
                    continue;
                }

                string gameArchivePath =
                    normalizedEntryPath[
                        rawIndex..
                    ];

                if (string.Equals(
                    gameArchivePath,
                    normalizedExpectedPath,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return entry;
                }
            }

            return null;
        }

        private string NormalizeArchivePath(
            string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            return path
                .Replace('\\', '/')
                .TrimStart('/');
        }

        private void InstallFromFolder(
            string gameRoot,
            Mod mod,
            string backupRoot,
            BackupService backupService)
        {
            string rawFolder =
                FindRawFolder(
                    mod.InstallPath
                );

            if (string.IsNullOrEmpty(rawFolder))
            {
                throw new DirectoryNotFoundException(
                    "Could not find the Sonic Generations 2024 image\\x64\\generations\\raw folder inside the extracted mod folder."
                );
            }

            foreach (ModFile modFile in mod.Files)
            {
                string fileName =
                    Path.GetFileName(
                        modFile.GamePath
                    );

                string? sourceFilePath =
                    FindSourceFile(
                        rawFolder,
                        fileName
                    );

                if (string.IsNullOrEmpty(
                    sourceFilePath))
                {
                    throw new FileNotFoundException(
                        $"Could not find {fileName} inside the extracted mod folder."
                    );
                }

                string gameFilePath =
                    GetGameFilePath(
                        gameRoot,
                        modFile
                    );

                BackupAndInstall(
                    gameRoot,
                    gameFilePath,
                    modFile.GamePath,
                    backupRoot,
                    backupService,
                    () =>
                    {
                        File.Copy(
                            sourceFilePath,
                            gameFilePath,
                            true
                        );
                    }
                );
            }
        }

        private string FindRawFolder(
            string modFolder)
        {
            string directRawFolder =
                Path.Combine(
                    modFolder,
                    "image",
                    "x64",
                    "generations",
                    "raw"
                );

            if (Directory.Exists(
                directRawFolder))
            {
                return directRawFolder;
            }

            string[] rawFolders =
                Directory.GetDirectories(
                    modFolder,
                    "raw",
                    SearchOption.AllDirectories
                );

            foreach (string rawFolder in rawFolders)
            {
                string normalized =
                    rawFolder
                        .Replace('\\', '/')
                        .TrimEnd('/');

                if (normalized.EndsWith(
                    "/image/x64/generations/raw",
                    StringComparison.OrdinalIgnoreCase))
                {
                    return rawFolder;
                }
            }

            return string.Empty;
        }

        private string? FindSourceFile(
            string rawFolder,
            string fileName)
        {
            string expectedFileName =
                fileName.Trim();

            string[] files =
                Directory.GetFiles(
                    rawFolder,
                    "*",
                    SearchOption.AllDirectories
                );

            foreach (string file in files)
            {
                string actualFileName =
                    Path.GetFileName(file);

                if (string.Equals(
                    actualFileName,
                    expectedFileName,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return file;
                }
            }

            return null;
        }

        private void BackupAndInstall(
            string gameRoot,
            string gameFilePath,
            string gamePath,
            string backupRoot,
            BackupService backupService,
            Action installAction)
        {
            string? gameDirectory =
                Path.GetDirectoryName(
                    gameFilePath
                );

            if (!string.IsNullOrEmpty(
                gameDirectory))
            {
                Directory.CreateDirectory(
                    gameDirectory
                );
            }

            if (File.Exists(
                gameFilePath))
            {
                if (!backupService.BackupExists(
                    gameRoot,
                    gameFilePath))
                {
                    string backupPath =
                        backupService.CreateBackup(
                            gameRoot,
                            gameFilePath,
                            backupRoot
                        );

                    if (string.IsNullOrEmpty(
                        backupPath) ||
                        !File.Exists(
                            backupPath))
                    {
                        throw new IOException(
                            $"Could not create a backup of {gamePath}."
                        );
                    }
                }
            }

            installAction();
        }
    }
}