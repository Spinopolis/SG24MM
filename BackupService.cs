using System.IO;

namespace SG24MM.Services
{
    public class BackupService
    {
        public string BackupRoot { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SG24MM",
        "Backups"
        );


    public string CreateBackup(
        string gameRoot,
        string gameFilePath,
        string backupRoot)
        {
            if (!File.Exists(gameFilePath))
            {
                return string.Empty;
            }

            string relativePath = Path.GetRelativePath(
                gameRoot,
                gameFilePath
            );

            string backupPath = Path.Combine(
                backupRoot,
                relativePath
            );

            string? backupDirectory = Path.GetDirectoryName(backupPath);

            if (!string.IsNullOrEmpty(backupDirectory))
            {
                Directory.CreateDirectory(backupDirectory);
            }

            File.Copy(
                gameFilePath,
                backupPath,
                true
            );

            return backupPath;
        }

        public bool BackupExists(
            string gameRoot,
            string gameFilePath)
        {
            if (!File.Exists(gameFilePath))
            {
                return false;
            }

            string relativePath = Path.GetRelativePath(
                gameRoot,
                gameFilePath
            );

            string backupPath = Path.Combine(
                BackupRoot,
                relativePath
            );

            return File.Exists(backupPath);
        }

        public bool RestoreFile(
            string gameRoot,
            string gameFilePath)
        {
            string relativePath = Path.GetRelativePath(
                gameRoot,
                gameFilePath
            );

            string backupPath = Path.Combine(
                BackupRoot,
                relativePath
            );

            if (!File.Exists(backupPath))
            {
                return false;
            }

            string? gameDirectory = Path.GetDirectoryName(
                gameFilePath
            );

            if (!string.IsNullOrEmpty(gameDirectory))
            {
                Directory.CreateDirectory(gameDirectory);
            }

            File.Copy(
                backupPath,
                gameFilePath,
                true
            );

            return true;
        }
    }


}
