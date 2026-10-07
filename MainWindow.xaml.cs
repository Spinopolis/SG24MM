using SG24MM.Models;
using SG24MM.Services;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using Forms = System.Windows.Forms;

namespace SG24MM
{
    public partial class MainWindow : Window
    {
        private readonly ConfigService _configService;
        private readonly ModArchiveService _modArchiveService;
        private readonly BackupService _backupService;
        private readonly ModInstallerService _modInstallerService;
        private readonly ModLibraryService _modLibraryService;
        private AppSettings _settings;

        private readonly List<Mod> _mods = new List<Mod>();

        private string _searchText = string.Empty;

        public MainWindow()
        {
            InitializeComponent();

            _configService = new ConfigService();
            _modArchiveService = new ModArchiveService();
            _backupService = new BackupService();
            _modInstallerService = new ModInstallerService();
            _modLibraryService = new ModLibraryService();
            _settings = _configService.LoadSettings();

            _mods.AddRange(_modLibraryService.LoadMods());

            UpdateModList();

            if (!string.IsNullOrEmpty(_settings.GamePath))
            {
                GamePathTextBox.Text = _settings.GamePath;
            }
        }

        private void BrowseGameFolder_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new Forms.FolderBrowserDialog();

            dialog.Description =
                "Select your Sonic Generations 2024 game folder";

            if (dialog.ShowDialog() == Forms.DialogResult.OK)
            {
                string selectedPath = dialog.SelectedPath;

                string gameExecutable = Path.Combine(
                    selectedPath,
                    "SONIC_X_SHADOW_GENERATIONS.exe"
                );

                if (!File.Exists(gameExecutable))
                {
                    System.Windows.MessageBox.Show(
                        "The selected folder does not contain SONIC_X_SHADOW_GENERATIONS.exe.",
                        "Invalid Game Folder",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );

                    return;
                }

                _settings.GamePath = selectedPath;
                _configService.SaveSettings(_settings);

                GamePathTextBox.Text = selectedPath;

                System.Windows.MessageBox.Show(
                    "Sonic Generations 2024 was detected and saved!",
                    "Game Detected",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
        }

        private void ImportMod_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new Forms.OpenFileDialog();

            dialog.Title = "Select a Sonic Generations 2024 mod";
            dialog.Filter = "ZIP files (*.zip)|*.zip";

            if (dialog.ShowDialog() == Forms.DialogResult.OK)
            {
                try
                {
                    Mod mod = _modArchiveService.ReadMod(dialog.FileName);

                    bool duplicateMod = _mods.Any(existingMod =>
                        string.Equals(
                            existingMod.ZipPath,
                            mod.ZipPath,
                            StringComparison.OrdinalIgnoreCase
                        )
                    );

                    if (duplicateMod)
                    {
                        System.Windows.MessageBox.Show(
                            "This mod has already been imported into SG24MM.",
                            "Duplicate Mod",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information
                        );

                        return;
                    }

                    string? description =
                        PromptForDescription(mod.Name);

                    if (description == null)
                    {
                        return;
                    }

                    mod.Description = description;

                    _mods.Add(mod);
                    _modLibraryService.SaveMods(_mods);

                    UpdateModList();

                    System.Windows.MessageBox.Show(
                        $"Mod: {mod.Name}\n\nFiles found: {mod.Files.Count}",
                        "Mod Imported",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(
                        $"Could not read the mod archive.\n\n{ex.Message}",
                        "Import Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                }
            }
        }

        private string? PromptForDescription(string modName)
        {
            System.Windows.Window descriptionWindow =
                new System.Windows.Window
                {
                    Title = "Mod Description",
                    Width = 500,
                    Height = 300,
                    WindowStartupLocation =
                        System.Windows.WindowStartupLocation.CenterScreen,
                    ResizeMode =
                        System.Windows.ResizeMode.NoResize
                };

            System.Windows.Controls.Grid grid =
                new System.Windows.Controls.Grid
                {
                    Margin = new Thickness(15)
                };

            grid.RowDefinitions.Add(
                new System.Windows.Controls.RowDefinition
                {
                    Height = System.Windows.GridLength.Auto
                }
            );

            grid.RowDefinitions.Add(
                new System.Windows.Controls.RowDefinition
                {
                    Height = new System.Windows.GridLength(
                        1,
                        System.Windows.GridUnitType.Star
                    )
                }
            );

            grid.RowDefinitions.Add(
                new System.Windows.Controls.RowDefinition
                {
                    Height = System.Windows.GridLength.Auto
                }
            );

            System.Windows.Controls.TextBlock promptText =
                new System.Windows.Controls.TextBlock
                {
                    Text =
                        $"Enter a description for \"{modName}\":",
                    FontSize = 16,
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 0, 0, 10)
                };

            System.Windows.Controls.TextBox descriptionBox =
                new System.Windows.Controls.TextBox
                {
                    AcceptsReturn = true,
                    TextWrapping =
                        System.Windows.TextWrapping.Wrap,
                    VerticalScrollBarVisibility =
                        System.Windows.Controls.ScrollBarVisibility.Auto,
                    FontSize = 14
                };

            System.Windows.Controls.StackPanel buttonPanel =
                new System.Windows.Controls.StackPanel
                {
                    Orientation =
                        System.Windows.Controls.Orientation.Horizontal,
                    HorizontalAlignment =
                        System.Windows.HorizontalAlignment.Right,
                    Margin = new Thickness(0, 10, 0, 0)
                };

            System.Windows.Controls.Button cancelButton =
                new System.Windows.Controls.Button
                {
                    Content = "Cancel",
                    Width = 90,
                    Height = 30,
                    Margin = new Thickness(0, 0, 10, 0)
                };

            System.Windows.Controls.Button saveButton =
                new System.Windows.Controls.Button
                {
                    Content = "Save",
                    Width = 90,
                    Height = 30
                };

            string? result = null;

            cancelButton.Click += (sender, e) =>
            {
                descriptionWindow.DialogResult = false;
                descriptionWindow.Close();
            };

            saveButton.Click += (sender, e) =>
            {
                result = descriptionBox.Text.Trim();

                descriptionWindow.DialogResult = true;
                descriptionWindow.Close();
            };

            buttonPanel.Children.Add(cancelButton);
            buttonPanel.Children.Add(saveButton);

            System.Windows.Controls.Grid.SetRow(
                promptText,
                0
            );

            System.Windows.Controls.Grid.SetRow(
                descriptionBox,
                1
            );

            System.Windows.Controls.Grid.SetRow(
                buttonPanel,
                2
            );

            grid.Children.Add(promptText);
            grid.Children.Add(descriptionBox);
            grid.Children.Add(buttonPanel);

            descriptionWindow.Content = grid;

            descriptionWindow.ShowDialog();

            return result;
        }

        private void SetExtractedFolder(Mod mod)
        {
            using var dialog = new Forms.FolderBrowserDialog();

            dialog.Description =
                "Select the extracted folder containing the mod's image folder";

            if (dialog.ShowDialog() != Forms.DialogResult.OK)
            {
                return;
            }

            try
            {
                Mod extractedMod =
                    _modArchiveService.ReadExtractedMod(
                        dialog.SelectedPath
                    );

                if (extractedMod.Files.Count == 0)
                {
                    System.Windows.MessageBox.Show(
                        "No Sonic Generations 2024 mod files were found in this folder.",
                        "No Mod Files",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );

                    return;
                }

                mod.InstallPath = dialog.SelectedPath;

                mod.Files = extractedMod.Files;

                _modLibraryService.SaveMods(_mods);

                UpdateModList();

                System.Windows.MessageBox.Show(
                    $"Extracted folder connected successfully.\n\n" +
                    $"Mod: {mod.Name}\n" +
                    $"Files found: {mod.Files.Count}",
                    "Extracted Folder Connected",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"The extracted mod folder could not be read.\n\n{ex.Message}",
                    "Folder Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void UpdateModList()
        {
            ModListPanel.Children.Clear();

            IEnumerable<Mod> modsToDisplay = _mods;

            if (!string.IsNullOrWhiteSpace(_searchText))
            {
                modsToDisplay = _mods.Where(mod =>
                    mod.Name.Contains(
                        _searchText,
                        StringComparison.OrdinalIgnoreCase
                    ) ||
                    mod.Description.Contains(
                        _searchText,
                        StringComparison.OrdinalIgnoreCase
                    )
                );
            }

            if (!modsToDisplay.Any())
            {
                EmptyModsText.Text =
                    string.IsNullOrWhiteSpace(_searchText)
                        ? "No mods imported"
                        : "No matching mods found";

                EmptyModsText.Visibility =
                    Visibility.Visible;

                return;
            }

            EmptyModsText.Visibility = Visibility.Collapsed;

            foreach (Mod mod in modsToDisplay)
            {
                System.Windows.Controls.Border modCard =
                    new System.Windows.Controls.Border
                    {
                        Background =
                            System.Windows.Media.Brushes.White,
                        Padding = new Thickness(20),
                        Margin = new Thickness(0, 0, 0, 15),
                        CornerRadius = new CornerRadius(8)
                    };

                System.Windows.Controls.StackPanel cardContent =
                    new System.Windows.Controls.StackPanel();

                System.Windows.Controls.TextBlock nameText =
                    new System.Windows.Controls.TextBlock
                    {
                        Text = mod.Name,
                        FontSize = 20,
                        FontWeight = FontWeights.SemiBold
                    };

                System.Windows.Controls.TextBlock filesText =
                    new System.Windows.Controls.TextBlock
                    {
                        Text = $"{mod.Files.Count} files",
                        Foreground =
                            System.Windows.Media.Brushes.Gray,
                        Margin = new Thickness(0, 8, 0, 10)
                    };

                System.Windows.Controls.TextBlock statusText =
                    new System.Windows.Controls.TextBlock
                    {
                        Text = mod.IsEnabled
                            ? "● ACTIVE"
                            : "● INACTIVE",

                        Foreground = mod.IsEnabled
                            ? System.Windows.Media.Brushes.Green
                            : System.Windows.Media.Brushes.Gray,

                        FontWeight = FontWeights.Bold,
                        Margin = new Thickness(0, 0, 0, 10)
                    };

                cardContent.Children.Add(statusText);

                System.Windows.Controls.Button descriptionButton =
                    new System.Windows.Controls.Button
                    {
                        Content = "▼ Show Description",
                        Width = 160,
                        Height = 30,
                        HorizontalAlignment =
                            System.Windows.HorizontalAlignment.Left,
                        Margin = new Thickness(0, 0, 0, 10)
                    };

                System.Windows.Controls.TextBlock descriptionText =
                    new System.Windows.Controls.TextBlock
                    {
                        Text = string.IsNullOrWhiteSpace(mod.Description)
                            ? "No description has been added for this mod yet."
                            : mod.Description,

                        FontSize = 14,

                        Foreground =
                            System.Windows.Media.Brushes.DimGray,

                        TextWrapping =
                            System.Windows.TextWrapping.Wrap,

                        Margin =
                            new Thickness(10, 0, 0, 10),

                        Visibility =
                            Visibility.Collapsed
                    };

                descriptionButton.Click += (sender, e) =>
                {
                    if (descriptionText.Visibility ==
                        Visibility.Collapsed)
                    {
                        descriptionText.Visibility =
                            Visibility.Visible;

                        descriptionButton.Content =
                            "▲ Hide Description";
                    }
                    else
                    {
                        descriptionText.Visibility =
                            Visibility.Collapsed;

                        descriptionButton.Content =
                            "▼ Show Description";
                    }
                };

                cardContent.Children.Add(
                    descriptionButton
                );

                cardContent.Children.Add(
                    descriptionText
                );

                bool hasConflict = false;

                foreach (Mod otherMod in _mods)
                {
                    if (otherMod == mod)
                    {
                        continue;
                    }

                    foreach (ModFile file in mod.Files)
                    {
                        foreach (ModFile otherFile in otherMod.Files)
                        {
                            if (string.Equals(
                                file.GamePath,
                                otherFile.GamePath,
                                StringComparison.OrdinalIgnoreCase))
                            {
                                hasConflict = true;
                                break;
                            }
                        }

                        if (hasConflict)
                        {
                            break;
                        }
                    }

                    if (hasConflict)
                    {
                        break;
                    }
                }

                if (hasConflict)
                {
                    List<string> conflictingModNames =
                        new List<string>();

                    foreach (Mod otherMod in _mods)
                    {
                        if (otherMod == mod)
                        {
                            continue;
                        }

                        bool conflictsWithMod = false;

                        foreach (ModFile file in mod.Files)
                        {
                            foreach (ModFile otherFile in otherMod.Files)
                            {
                                if (string.Equals(
                                    file.GamePath,
                                    otherFile.GamePath,
                                    StringComparison.OrdinalIgnoreCase))
                                {
                                    conflictsWithMod = true;
                                    break;
                                }
                            }

                            if (conflictsWithMod)
                            {
                                break;
                            }
                        }

                        if (conflictsWithMod)
                        {
                            conflictingModNames.Add(
                                otherMod.Name
                            );
                        }
                    }

                    string conflictingModsText =
                        string.Join(
                            "\n• ",
                            conflictingModNames
                        );

                    System.Windows.Controls.TextBlock conflictText =
                        new System.Windows.Controls.TextBlock
                        {
                            Text = "⚠ Conflict detected",
                            Foreground =
                                System.Windows.Media.Brushes.Red,
                            FontWeight = FontWeights.Bold,
                            Margin = new Thickness(0, 0, 0, 10)
                        };

                    System.Windows.Controls.ToolTip conflictToolTip =
                        new System.Windows.Controls.ToolTip
                        {
                            Content =
                                new System.Windows.Controls.TextBlock
                                {
                                    Text =
                                        $"Mod Conflict\n\n" +
                                        $"This mod shares one or more game files with another installed mod. " +
                                        $"Enabling both mods may cause one mod to overwrite files from the other.\n\n" +
                                        $"Conflicting mod(s):\n" +
                                        $"• {conflictingModsText}\n\n" +
                                        $"For the best results, only enable one of these conflicting mods at a time.",
                                    TextWrapping =
                                        System.Windows.TextWrapping.Wrap,
                                    MaxWidth = 450
                                },
                            Placement =
                                System.Windows.Controls.Primitives.PlacementMode.Mouse,
                            MaxWidth = 470
                        };

                    conflictText.ToolTip = conflictToolTip;

                    cardContent.Children.Add(
                        conflictText
                    );
                }

                System.Windows.Controls.Button enableButton =
                    new System.Windows.Controls.Button
                    {
                        Content = mod.IsEnabled
                            ? "Disable"
                            : "Enable",

                        Width = 120,
                        Height = 30,

                        HorizontalAlignment =
                            System.Windows.HorizontalAlignment.Left,

                        Margin = new Thickness(0, 0, 0, 10)
                    };

                enableButton.Click += (sender, e) =>
                {
                    if (mod.IsEnabled)
                    {
                        if (string.IsNullOrWhiteSpace(
                                _settings.GamePath) ||
                            !Directory.Exists(
                                _settings.GamePath))
                        {
                            System.Windows.MessageBox.Show(
                                "The game folder could not be found.",
                                "Game Folder Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning
                            );

                            return;
                        }

                        try
                        {
                            foreach (ModFile file in mod.Files)
                            {
                                string gameFilePath =
                                    _modInstallerService
                                        .GetGameFilePath(
                                            _settings.GamePath,
                                            file
                                        );

                                bool restored =
                                    _backupService.RestoreFile(
                                        _settings.GamePath,
                                        gameFilePath
                                    );

                                if (!restored &&
                                    File.Exists(gameFilePath))
                                {
                                    File.Delete(gameFilePath);
                                }
                            }

                            mod.IsEnabled = false;

                            _modLibraryService.SaveMods(
                                _mods
                            );

                            UpdateModList();

                            System.Windows.MessageBox.Show(
                                $"\"{mod.Name}\" was successfully disabled and the original files were restored.",
                                "Mod Disabled",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information
                            );
                        }
                        catch (Exception ex)
                        {
                            System.Windows.MessageBox.Show(
                                $"The mod could not be disabled.\n\n{ex.Message}",
                                "Disable Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error
                            );
                        }

                        return;
                    }

                    if (string.IsNullOrWhiteSpace(
                            _settings.GamePath) ||
                        !Directory.Exists(
                            _settings.GamePath))
                    {
                        System.Windows.MessageBox.Show(
                            "Please select your Sonic X Shadow Generations game folder first.",
                            "Game Folder Required",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning
                        );

                        return;
                    }

                    string gameExecutable = Path.Combine(
                        _settings.GamePath,
                        "SONIC_X_SHADOW_GENERATIONS.exe"
                    );

                    if (!File.Exists(gameExecutable))
                    {
                        System.Windows.MessageBox.Show(
                            "The selected folder is not a valid Sonic X Shadow Generations game folder.",
                            "Invalid Game Folder",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning
                        );

                        return;
                    }

                    if (mod.Files.Count == 0)
                    {
                        System.Windows.MessageBox.Show(
                            "This mod does not contain any files.",
                            "Empty Mod",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning
                        );

                        return;
                    }

                    foreach (Mod otherMod in _mods)
                    {
                        if (otherMod == mod ||
                            !otherMod.IsEnabled)
                        {
                            continue;
                        }

                        foreach (ModFile file in mod.Files)
                        {
                            foreach (ModFile otherFile in otherMod.Files)
                            {
                                if (string.Equals(
                                    file.GamePath,
                                    otherFile.GamePath,
                                    StringComparison.OrdinalIgnoreCase))
                                {
                                    System.Windows.MessageBox.Show(
                                        $"This mod conflicts with the active mod \"{otherMod.Name}\".\n\nConflicting file:\n{file.GamePath}",
                                        "Mod Conflict",
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Warning
                                    );

                                    return;
                                }
                            }
                        }
                    }

                    try
                    {
                        bool allFilesAlreadyMatch =
                            AreModFilesAlreadyInstalled(
                                mod,
                                out int alreadyMatchingFiles,
                                out int filesChecked,
                                out string safetyMessage
                            );

                        if (allFilesAlreadyMatch &&
                            filesChecked > 0)
                        {
                            System.Windows.MessageBox.Show(
                                $"SG24MM detected that this mod is already installed in the game folder.\n\n" +
                                $"Mod: {mod.Name}\n" +
                                $"Matching files: {alreadyMatchingFiles} / {filesChecked}\n\n" +
                                "SG24MM will NOT enable or reinstall the mod because doing so could create backups from already-modded files.\n\n" +
                                "The game files must be restored to their original state before SG24MM can safely install this mod.",
                                "Mod Already Installed",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning
                            );

                            return;
                        }

                        if (filesChecked > 0 &&
                            alreadyMatchingFiles > 0 &&
                            alreadyMatchingFiles < filesChecked)
                        {
                            System.Windows.MessageBox.Show(
                                $"SG24MM detected that some files from this mod already match the game files.\n\n" +
                                $"Matching files: {alreadyMatchingFiles} / {filesChecked}\n\n" +
                                "The mod appears to be partially installed or the game files may have been changed.\n\n" +
                                "SG24MM will not install the mod until the current game files are in a safe state.",
                                "Unsafe Installation State",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning
                            );

                            return;
                        }

                        if (!string.IsNullOrWhiteSpace(safetyMessage))
                        {
                            System.Windows.MessageBox.Show(
                                safetyMessage,
                                "Installation Safety Check",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show(
                            $"SG24MM could not complete the installation safety check.\n\n" +
                            $"The mod will NOT be installed.\n\n" +
                            $"{ex.Message}",
                            "Safety Check Failed",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error
                        );

                        return;
                    }

                    try
                    {
                        _modInstallerService.InstallMod(
                            _settings.GamePath,
                            mod,
                            _backupService.BackupRoot,
                            _backupService
                        );

                        mod.IsEnabled = true;

                        _modLibraryService.SaveMods(
                            _mods
                        );

                        UpdateModList();

                        System.Windows.MessageBox.Show(
                            $"\"{mod.Name}\" was successfully installed and enabled.",
                            "Mod Enabled",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information
                        );
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show(
                            $"The mod could not be installed.\n\n{ex.Message}",
                            "Installation Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error
                        );
                    }
                };

                System.Windows.Controls.Button showFilesButton =
                    new System.Windows.Controls.Button
                    {
                        Content = "▼ Show Files",
                        Width = 120,
                        Height = 30,
                        HorizontalAlignment =
                            System.Windows.HorizontalAlignment.Left
                    };

                System.Windows.Controls.StackPanel fileListPanel =
                    new System.Windows.Controls.StackPanel
                    {
                        Visibility = Visibility.Collapsed,
                        Margin = new Thickness(0, 10, 0, 0)
                    };

                foreach (ModFile file in mod.Files)
                {
                    System.Windows.Controls.TextBlock fileText =
                        new System.Windows.Controls.TextBlock
                        {
                            Text = file.GamePath,
                            FontSize = 13,
                            Foreground =
                                System.Windows.Media.Brushes.DimGray,
                            Margin =
                                new Thickness(10, 3, 0, 3),
                            TextWrapping =
                                TextWrapping.Wrap
                        };

                    fileListPanel.Children.Add(fileText);
                }

                showFilesButton.Click += (sender, e) =>
                {
                    if (fileListPanel.Visibility ==
                        Visibility.Collapsed)
                    {
                        fileListPanel.Visibility =
                            Visibility.Visible;

                        showFilesButton.Content =
                            "▲ Hide Files";
                    }
                    else
                    {
                        fileListPanel.Visibility =
                            Visibility.Collapsed;

                        showFilesButton.Content =
                            "▼ Show Files";
                    }
                };

                System.Windows.Controls.Button extractedFolderButton =
                    new System.Windows.Controls.Button
                    {
                        Content = "Set Mod Folder",
                        Width = 120,
                        Height = 30,
                        HorizontalAlignment =
                            System.Windows.HorizontalAlignment.Left,
                        Margin = new Thickness(0, 10, 0, 0)
                    };

                extractedFolderButton.Click += (sender, e) =>
                {
                    SetExtractedFolder(mod);
                };

                System.Windows.Controls.Button verifyButton =
                    new System.Windows.Controls.Button
                    {
                        Content = "Verify Files",
                        Width = 120,
                        Height = 30,
                        HorizontalAlignment =
                            System.Windows.HorizontalAlignment.Left,
                        Margin = new Thickness(0, 10, 0, 0)
                    };

                verifyButton.Click += (sender, e) =>
                {
                    VerifyModFiles(mod);
                };

                System.Windows.Controls.Button removeButton =
                    new System.Windows.Controls.Button
                    {
                        Content = "Remove Mod",
                        Width = 120,
                        Height = 30,
                        HorizontalAlignment =
                            System.Windows.HorizontalAlignment.Left,
                        Margin = new Thickness(0, 10, 0, 0)
                    };

                removeButton.Click += (sender, e) =>
                {
                    if (mod.IsEnabled)
                    {
                        System.Windows.MessageBox.Show(
                            "This mod is currently active.\n\nDisable the mod before removing it from SG24MM.",
                            "Mod Is Active",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning
                        );

                        return;
                    }

                    MessageBoxResult result =
                        System.Windows.MessageBox.Show(
                            $"Are you sure you want to remove \"{mod.Name}\" from SG24MM?",
                            "Remove Mod",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question
                        );

                    if (result != MessageBoxResult.Yes)
                    {
                        return;
                    }

                    _mods.Remove(mod);

                    _modLibraryService.SaveMods(
                        _mods
                    );

                    UpdateModList();

                    System.Windows.MessageBox.Show(
                        $"\"{mod.Name}\" was removed from SG24MM.",
                        "Mod Removed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                };

                cardContent.Children.Add(nameText);
                cardContent.Children.Add(filesText);
                cardContent.Children.Add(enableButton);

                if (_settings.ShowFiles)
                {
                    cardContent.Children.Add(showFilesButton);
                    cardContent.Children.Add(fileListPanel);
                }

                if (_settings.ShowModFolder)
                {
                    cardContent.Children.Add(extractedFolderButton);
                }

                if (_settings.ShowVerifyFiles)
                {
                    cardContent.Children.Add(verifyButton);
                }

                cardContent.Children.Add(removeButton);

                modCard.Child = cardContent;

                ModListPanel.Children.Add(modCard);
            }
        }

        private void SearchMods_TextChanged(
            object sender,
            TextChangedEventArgs e)
        {
            _searchText =
                SearchTextBox.Text.Trim();

            UpdateModList();
        }

        private bool AreModFilesAlreadyInstalled(
            Mod mod,
            out int matchingFiles,
            out int filesChecked,
            out string message)
        {
            matchingFiles = 0;
            filesChecked = 0;
            message = string.Empty;

            if (mod.Files.Count == 0)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(mod.ZipPath) &&
                File.Exists(mod.ZipPath))
            {
                using ZipArchive archive =
                    ZipFile.OpenRead(mod.ZipPath);

                foreach (ModFile modFile in mod.Files)
                {
                    ZipArchiveEntry? entry =
                        FindZipEntryForVerification(
                            archive,
                            modFile.ArchivePath
                        );

                    if (entry == null)
                    {
                        continue;
                    }

                    string gameFilePath =
                        _modInstallerService.GetGameFilePath(
                            _settings.GamePath,
                            modFile
                        );

                    if (!File.Exists(gameFilePath))
                    {
                        continue;
                    }

                    filesChecked++;

                    string sourceHash;

                    using (Stream sourceStream =
                           entry.Open())
                    using (SHA256 sha256 =
                           SHA256.Create())
                    {
                        byte[] hash =
                            sha256.ComputeHash(
                                sourceStream
                            );

                        sourceHash =
                            Convert.ToHexString(hash);
                    }

                    string gameHash =
                        CalculateFileHash(
                            gameFilePath
                        );

                    if (string.Equals(
                        sourceHash,
                        gameHash,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        matchingFiles++;
                    }
                }

                return filesChecked > 0 &&
                       matchingFiles == filesChecked;
            }

            if (!string.IsNullOrWhiteSpace(mod.InstallPath) &&
                Directory.Exists(mod.InstallPath))
            {
                string rawFolder =
                    FindRawFolderForVerification(
                        mod.InstallPath
                    );

                if (string.IsNullOrEmpty(rawFolder))
                {
                    message =
                        "SG24MM could not find the image\\x64\\generations\\raw folder inside the extracted mod folder.";

                    return false;
                }

                foreach (ModFile modFile in mod.Files)
                {
                    string? sourceFilePath =
                        FindModSourceFile(
                            rawFolder,
                            modFile
                        );

                    if (string.IsNullOrEmpty(sourceFilePath))
                    {
                        continue;
                    }

                    string gameFilePath =
                        _modInstallerService.GetGameFilePath(
                            _settings.GamePath,
                            modFile
                        );

                    if (!File.Exists(gameFilePath))
                    {
                        continue;
                    }

                    filesChecked++;

                    string sourceHash =
                        CalculateFileHash(
                            sourceFilePath
                        );

                    string gameHash =
                        CalculateFileHash(
                            gameFilePath
                        );

                    if (string.Equals(
                        sourceHash,
                        gameHash,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        matchingFiles++;
                    }
                }

                if (filesChecked == 0)
                {
                    message =
                        $"SG24MM found the extracted mod folder, but none of the {mod.Files.Count} mod files could be compared against the game files.\n\n" +
                        $"Raw folder found:\n{rawFolder}";

                    return false;
                }

                return matchingFiles == filesChecked;
            }

            message =
                "SG24MM could not find either the original ZIP or the extracted mod folder.";

            return false;
        }

        private string NormalizeFileName(
            string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return string.Empty;
            }

            string normalized =
                fileName
                    .Trim()
                    .Replace('/', '\\');

            int lastSeparator =
                normalized.LastIndexOf('\\');

            if (lastSeparator >= 0)
            {
                normalized =
                    normalized.Substring(
                        lastSeparator + 1
                    );
            }

            return normalized.Trim();
        }

        private string? FindModSourceFile(
            string rawFolder,
            ModFile modFile)
        {
            if (!Directory.Exists(rawFolder))
            {
                return null;
            }

            string expectedFileName =
                NormalizeFileName(
                    Path.GetFileName(
                        modFile.GamePath
                    )
                );

            if (string.IsNullOrWhiteSpace(
                    expectedFileName))
            {
                return null;
            }

            string[] actualFiles =
                Directory.GetFiles(
                    rawFolder,
                    "*",
                    SearchOption.AllDirectories
                );

            foreach (string actualFile in actualFiles)
            {
                string actualFileName =
                    NormalizeFileName(
                        Path.GetFileName(actualFile)
                    );

                if (string.Equals(
                    actualFileName,
                    expectedFileName,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return actualFile;
                }
            }

            string debugMessage =
                "FILENAME MATCH DEBUG\n\n" +
                $"Expected filename:\n[{expectedFileName}]\n\n" +
                $"Expected length: {expectedFileName.Length}\n\n" +
                "Expected character codes:\n";

            foreach (char c in expectedFileName)
            {
                debugMessage +=
                    $"'{c}' = U+{((int)c):X4}\n";
            }

            debugMessage +=
                "\nACTUAL FILES:\n\n";

            foreach (string actualFile in actualFiles)
            {
                string actualFileName =
                    NormalizeFileName(
                        Path.GetFileName(actualFile)
                    );

                debugMessage +=
                    $"[{actualFileName}]\n" +
                    $"Length: {actualFileName.Length}\n" +
                    "Characters: ";

                foreach (char c in actualFileName)
                {
                    debugMessage +=
                        $"U+{((int)c):X4} ";
                }

                debugMessage +=
                    $"\nPath: {actualFile}\n\n";
            }

            System.Windows.Window debugWindow =
                new System.Windows.Window
                {
                    Title = "Filename Match Debug",
                    Width = 900,
                    Height = 700,
                    WindowStartupLocation =
                        System.Windows.WindowStartupLocation.CenterScreen
                };

            System.Windows.Controls.TextBox debugTextBox =
                new System.Windows.Controls.TextBox
                {
                    Text = debugMessage,
                    IsReadOnly = true,
                    AcceptsReturn = true,
                    AcceptsTab = true,
                    TextWrapping =
                        System.Windows.TextWrapping.NoWrap,
                    VerticalScrollBarVisibility =
                        System.Windows.Controls.ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility =
                        System.Windows.Controls.ScrollBarVisibility.Auto,
                    Margin = new System.Windows.Thickness(10),
                    FontFamily =
                        new System.Windows.Media.FontFamily(
                            "Consolas"
                        ),
                    FontSize = 14
                };

            debugWindow.Content = debugTextBox;

            debugWindow.ShowDialog();

            return null;
        }

        private string CalculateFileHash(
            string filePath)
        {
            using FileStream stream =
                File.OpenRead(filePath);

            using SHA256 sha256 =
                SHA256.Create();

            byte[] hash =
                sha256.ComputeHash(
                    stream
                );

            return Convert.ToHexString(hash);
        }

        private void VerifyModFiles(Mod mod)
        {
            if (string.IsNullOrWhiteSpace(
                    _settings.GamePath) ||
                !Directory.Exists(
                    _settings.GamePath))
            {
                System.Windows.MessageBox.Show(
                    "Please select your Sonic X Shadow Generations game folder first.",
                    "Game Folder Required",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );

                return;
            }

            int matches = 0;
            int mismatches = 0;
            int missing = 0;

            List<string> results = new List<string>();

            try
            {
                if (!string.IsNullOrWhiteSpace(mod.ZipPath) &&
                    File.Exists(mod.ZipPath))
                {
                    using ZipArchive archive =
                        ZipFile.OpenRead(mod.ZipPath);

                    foreach (ModFile modFile in mod.Files)
                    {
                        ZipArchiveEntry? entry =
                            FindZipEntryForVerification(
                                archive,
                                modFile.ArchivePath
                            );

                        if (entry == null)
                        {
                            missing++;

                            results.Add(
                                $"✗ MISSING FROM ZIP: {modFile.GamePath}"
                            );

                            continue;
                        }

                        string gameFilePath =
                            _modInstallerService.GetGameFilePath(
                                _settings.GamePath,
                                modFile
                            );

                        if (!File.Exists(gameFilePath))
                        {
                            missing++;

                            results.Add(
                                $"✗ MISSING FROM GAME: {modFile.GamePath}"
                            );

                            continue;
                        }

                        string zipHash;

                        using (Stream zipStream = entry.Open())
                        using (SHA256 sha256 =
                               SHA256.Create())
                        {
                            byte[] hash =
                                sha256.ComputeHash(
                                    zipStream
                                );

                            zipHash =
                                Convert.ToHexString(hash);
                        }

                        string gameHash =
                            CalculateFileHash(
                                gameFilePath
                            );

                        if (string.Equals(
                            zipHash,
                            gameHash,
                            StringComparison.OrdinalIgnoreCase))
                        {
                            matches++;

                            results.Add(
                                $"✓ MATCH: {modFile.GamePath}"
                            );
                        }
                        else
                        {
                            mismatches++;

                            results.Add(
                                $"✗ MISMATCH: {modFile.GamePath}"
                            );

                            results.Add(
                                $"Source Hash: {zipHash}"
                            );

                            results.Add(
                                $"Game Hash: {gameHash}"
                            );
                        }
                    }
                }
                else if (!string.IsNullOrWhiteSpace(
                             mod.InstallPath) &&
                         Directory.Exists(
                             mod.InstallPath))
                {
                    string rawFolder =
                        FindRawFolderForVerification(
                            mod.InstallPath
                        );

                    results.Add(
                        "========== DIAGNOSTIC =========="
                    );

                    results.Add(
                        $"Mod: {mod.Name}"
                    );

                    results.Add(
                        $"Stored InstallPath:\n{mod.InstallPath}"
                    );

                    results.Add(
                        $"InstallPath Exists: {Directory.Exists(mod.InstallPath)}"
                    );

                    results.Add(
                        $"Raw Folder Found:\n{rawFolder}"
                    );

                    results.Add(
                        $"Raw Folder Exists: {Directory.Exists(rawFolder)}"
                    );

                    results.Add(
                        $"Stored Mod Files: {mod.Files.Count}"
                    );

                    results.Add(
                        "================================"
                    );

                    if (string.IsNullOrEmpty(rawFolder))
                    {
                        results.Add(
                            "ERROR: Could not find the raw folder."
                        );

                        ShowScrollableVerificationResults(
                            mod,
                            matches,
                            mismatches,
                            missing,
                            results
                        );

                        return;
                    }

                    string[] actualFiles =
                        Directory.GetFiles(
                            rawFolder,
                            "*",
                            SearchOption.AllDirectories
                        );

                    results.Add(
                        $"Actual files found inside raw folder: {actualFiles.Length}"
                    );

                    results.Add("");

                    results.Add(
                        "========== ACTUAL FILES =========="
                    );

                    foreach (string actualFile in actualFiles)
                    {
                        string actualFileName =
                            Path.GetFileName(actualFile);

                        results.Add(
                            $"[{actualFileName}]"
                        );
                    }

                    results.Add(
                        "=================================="
                    );

                    foreach (ModFile modFile in mod.Files)
                    {
                        string fileName =
                            NormalizeFileName(
                                Path.GetFileName(
                                    modFile.GamePath
                                )
                            );

                        results.Add("");
                        results.Add(
                            "=================================="
                        );

                        results.Add(
                            $"CHECKING: {fileName}"
                        );

                        results.Add(
                            $"GamePath: {modFile.GamePath}"
                        );

                        results.Add(
                            $"ArchivePath: {modFile.ArchivePath}"
                        );

                        results.Add(
                            $"Expected filename: [{fileName}]"
                        );

                        results.Add(
                            $"Expected filename length: {fileName.Length}"
                        );

                        string? sourceFilePath =
                            FindModSourceFile(
                                rawFolder,
                                modFile
                            );

                        if (string.IsNullOrEmpty(sourceFilePath))
                        {
                            missing++;

                            results.Add(
                                $"✗ NOT FOUND: {fileName}"
                            );

                            results.Add(
                                "No exact filename match was found in the raw folder."
                            );

                            results.Add(
                                "Actual filenames available:"
                            );

                            foreach (string actualFile in actualFiles)
                            {
                                results.Add(
                                    $"[{Path.GetFileName(actualFile)}]"
                                );
                            }

                            continue;
                        }

                        results.Add(
                            $"SOURCE FOUND: {sourceFilePath}"
                        );

                        string gameFilePath =
                            _modInstallerService.GetGameFilePath(
                                _settings.GamePath,
                                modFile
                            );

                        results.Add(
                            "GAME FILE PATH BEING CHECKED:"
                        );

                        results.Add(
                            $"[{gameFilePath}]"
                        );

                        bool gameFileExists =
                            File.Exists(gameFilePath);

                        results.Add(
                            $"Windows File.Exists result: {gameFileExists}"
                        );

                        results.Add(
                            $"Windows File.Exists path length: {gameFilePath.Length}"
                        );

                        if (!gameFileExists)
                        {
                            missing++;

                            results.Add(
                                $"✗ MISSING FROM GAME: {modFile.GamePath}"
                            );

                            continue;
                        }

                        results.Add(
                            $"✓ FILE EXISTS: {gameFilePath}"
                        );

                        string sourceHash =
                            CalculateFileHash(
                                sourceFilePath
                            );

                        string gameHash =
                            CalculateFileHash(
                                gameFilePath
                            );

                        results.Add(
                            $"Source Hash: {sourceHash}"
                        );

                        results.Add(
                            $"Game Hash: {gameHash}"
                        );

                        if (string.Equals(
                            sourceHash,
                            gameHash,
                            StringComparison.OrdinalIgnoreCase))
                        {
                            matches++;

                            results.Add(
                                $"✓ MATCH: {modFile.GamePath}"
                            );
                        }
                        else
                        {
                            mismatches++;

                            results.Add(
                                $"✗ MISMATCH: {modFile.GamePath}"
                            );
                        }
                    }
                }
                else
                {
                    results.Add(
                        "Neither the original ZIP nor the extracted mod folder could be found."
                    );

                    results.Add("");
                    results.Add("Stored ZIP Path:");
                    results.Add(mod.ZipPath);
                    results.Add("");
                    results.Add("Stored Install Path:");
                    results.Add(mod.InstallPath);
                }

                ShowScrollableVerificationResults(
                    mod,
                    matches,
                    mismatches,
                    missing,
                    results
                );
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"The mod could not be verified.\n\n{ex.Message}",
                    "Verification Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private ZipArchiveEntry? FindZipEntryForVerification(
            ZipArchive archive,
            string expectedPath)
        {
            const string rawMarker =
                "image/x64/generations/raw/";

            string normalizedExpectedPath =
                expectedPath
                    .Replace('\\', '/')
                    .TrimStart('/');

            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                if (string.IsNullOrWhiteSpace(entry.Name))
                {
                    continue;
                }

                string normalizedEntryPath =
                    entry.FullName
                        .Replace('\\', '/')
                        .TrimStart('/');

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

        private void ShowScrollableVerificationResults(
            Mod mod,
            int matches,
            int mismatches,
            int missing,
            List<string> results)
        {
            string summary =
                $"Mod: {mod.Name}\n\n" +
                $"Matches: {matches}\n" +
                $"Mismatches: {mismatches}\n" +
                $"Missing: {missing}\n" +
                $"Total mod files: {mod.Files.Count}\n\n" +
                string.Join("\n", results);

            System.Windows.Window resultsWindow =
                new System.Windows.Window
                {
                    Title = "Mod File Verification - Diagnostic",
                    Width = 1100,
                    Height = 800,
                    MinWidth = 700,
                    MinHeight = 500,
                    WindowStartupLocation =
                        System.Windows.WindowStartupLocation.CenterScreen
                };

            System.Windows.Controls.Grid resultsGrid =
                new System.Windows.Controls.Grid();

            resultsGrid.RowDefinitions.Add(
                new System.Windows.Controls.RowDefinition
                {
                    Height = new System.Windows.GridLength(
                        1,
                        System.Windows.GridUnitType.Star
                    )
                }
            );

            resultsGrid.RowDefinitions.Add(
                new System.Windows.Controls.RowDefinition
                {
                    Height = System.Windows.GridLength.Auto
                }
            );

            System.Windows.Controls.TextBox resultsTextBox =
                new System.Windows.Controls.TextBox
                {
                    Text = summary,
                    IsReadOnly = true,
                    AcceptsReturn = true,
                    AcceptsTab = true,
                    TextWrapping =
                        System.Windows.TextWrapping.NoWrap,
                    VerticalScrollBarVisibility =
                        System.Windows.Controls.ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility =
                        System.Windows.Controls.ScrollBarVisibility.Auto,
                    Margin =
                        new System.Windows.Thickness(10),
                    FontFamily =
                        new System.Windows.Media.FontFamily(
                            "Consolas"
                        ),
                    FontSize = 14,
                    VerticalContentAlignment =
                        System.Windows.VerticalAlignment.Top,
                    HorizontalContentAlignment =
                        System.Windows.HorizontalAlignment.Left
                };

            System.Windows.Controls.Grid.SetRow(
                resultsTextBox,
                0
            );

            resultsGrid.Children.Add(
                resultsTextBox
            );

            System.Windows.Controls.Button closeButton =
                new System.Windows.Controls.Button
                {
                    Content = "Close",
                    Width = 100,
                    Height = 35,
                    HorizontalAlignment =
                        System.Windows.HorizontalAlignment.Right,
                    Margin =
                        new System.Windows.Thickness(
                            10,
                            0,
                            10,
                            10
                        )
                };

            closeButton.Click += (sender, e) =>
            {
                resultsWindow.Close();
            };

            System.Windows.Controls.Grid.SetRow(
                closeButton,
                1
            );

            resultsGrid.Children.Add(
                closeButton
            );

            resultsWindow.Content = resultsGrid;

            resultsWindow.ShowDialog();
        }

        private string FindRawFolderForVerification(
            string modFolder)
        {
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

            string[] rawFolders = Directory.GetDirectories(
                modFolder,
                "raw",
                SearchOption.AllDirectories
            );

            string? bestRawFolder = null;
            int bestFileCount = -1;

            foreach (string rawFolder in rawFolders)
            {
                try
                {
                    string[] files =
                        Directory.GetFiles(
                            rawFolder,
                            "*",
                            SearchOption.AllDirectories
                        );

                    int fileCount = files.Length;

                    string normalized =
                        rawFolder.Replace('\\', '/');

                    if (normalized.EndsWith(
                        "/image/x64/generations/raw",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        return rawFolder;
                    }

                    if (fileCount > bestFileCount)
                    {
                        bestFileCount = fileCount;
                        bestRawFolder = rawFolder;
                    }
                }
                catch
                {
                }
            }

            return bestRawFolder ?? string.Empty;
        }

        private void LaunchGame_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(
                    _settings.GamePath) ||
                !Directory.Exists(
                    _settings.GamePath))
            {
                System.Windows.MessageBox.Show(
                    "Please select your Sonic X Shadow Generations game folder first.",
                    "Game Folder Required",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );

                return;
            }

            string gameExecutable = Path.Combine(
                _settings.GamePath,
                "SONIC_X_SHADOW_GENERATIONS.exe"
            );

            if (!File.Exists(gameExecutable))
            {
                System.Windows.MessageBox.Show(
                    "SONIC_X_SHADOW_GENERATIONS.exe could not be found in the selected game folder.",
                    "Game Not Found",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );

                return;
            }

            try
            {
                System.Diagnostics.Process.Start(
                    new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = gameExecutable,
                        WorkingDirectory = _settings.GamePath,
                        UseShellExecute = true
                    }
                );
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"The game could not be launched.\n\n{ex.Message}",
                    "Launch Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void ModsNavigation_Click(
            object sender,
            RoutedEventArgs e)
        {
            ModsPage.Visibility = Visibility.Visible;
            SettingsPage.Visibility = Visibility.Collapsed;
        }

        private void SettingsNavigation_Click(
            object sender,
            RoutedEventArgs e)
        {
            ModsPage.Visibility = Visibility.Collapsed;
            SettingsPage.Visibility = Visibility.Visible;

            ShowModFolderCheckBox.IsChecked =
                _settings.ShowModFolder;

            ShowVerifyFilesCheckBox.IsChecked =
                _settings.ShowVerifyFiles;

            ShowFilesCheckBox.IsChecked =
                _settings.ShowFiles;
        }

        private void SaveSettings_Click(
            object sender,
            RoutedEventArgs e)
        {
            _settings.ShowModFolder =
                ShowModFolderCheckBox.IsChecked == true;

            _settings.ShowVerifyFiles =
                ShowVerifyFilesCheckBox.IsChecked == true;

            _settings.ShowFiles =
                ShowFilesCheckBox.IsChecked == true;

            _configService.SaveSettings(
                _settings
            );

            UpdateModList();

            ModsPage.Visibility = Visibility.Visible;
            SettingsPage.Visibility = Visibility.Collapsed;
        }
    }
}