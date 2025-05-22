
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using WinForms = System.Windows.Forms;
using ModSwitcher.Properties;
using System.Windows.Media;

namespace ModSwitcher
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public ObservableCollection<string> AvailableModVersions { get; set; } = new ObservableCollection<string>();

        private string _selectedModVersion;
        public string SelectedModVersion
        {
            get => _selectedModVersion;
            set
            {
                _selectedModVersion = value;
                NotifyPropertyChanged(nameof(SelectedModVersion));
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            // Restore last-used folders
            MinecraftModsPathTextBox.Text = Settings.Default.MinecraftModsFolder;
            ModStoragePathTextBox.Text = Settings.Default.ModStorageFolder;

            // Populate versions list if possible
            if (Directory.Exists(ModStoragePathTextBox.Text))
                LoadModVersions();

            // Show which version is active in the mods folder
            UpdateActiveModStatus();
        }
        private void UpdateActiveModStatus()
        {
            var mcPath = MinecraftModsPathTextBox.Text;
            if (!Directory.Exists(mcPath))
            {
                ActiveVersionTextBlock.Text = "Active Version: (folder not set)";
                return;
            }

            var txtFiles = Directory.GetFiles(mcPath, "*.txt");
            if (txtFiles.Length == 0)
            {
                ActiveVersionTextBlock.Text = "Active Version: (none)";
            }
            else
            {
                var version = Path.GetFileNameWithoutExtension(txtFiles[0]);
                ActiveVersionTextBlock.Text = $"Active Version: {version}";
            }
        }
        private void LoadModVersions()
        {
            AvailableModVersions.Clear();

            var storagePath = ModStoragePathTextBox.Text;
            if (!Directory.Exists(storagePath)) return;

            foreach (var dir in Directory.GetDirectories(storagePath))
            {
                var versionName = Path.GetFileName(dir);
                var versionFile = Path.Combine(dir, $"{versionName}.txt");

                if (File.Exists(versionFile))
                    AvailableModVersions.Add(versionName);
            }

            StatusTextBlock.Text = $"📦 Loaded {AvailableModVersions.Count} version(s).";
            StatusTextBlock.Foreground = new SolidColorBrush(Colors.DarkGreen);
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadModVersions();
        }

        private void ActivateMods_Click(object sender, RoutedEventArgs e)
        {
            var selectedVersion = SelectedModVersion;
            var minecraftPath = MinecraftModsPathTextBox.Text;
            var storagePath = ModStoragePathTextBox.Text;

            if (string.IsNullOrEmpty(selectedVersion) || !Directory.Exists(storagePath) || !Directory.Exists(minecraftPath))
            {
                StatusTextBlock.Text = "❌ Error: Ensure both paths are set and a version is selected.";
                StatusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                return;
            }

            var versionFolder = Path.Combine(storagePath, selectedVersion);
            if (!Directory.Exists(versionFolder))
            {
                StatusTextBlock.Text = "❌ Selected version folder not found.";
                StatusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                return;
            }

            try
            {
                // Clear current mods
                foreach (var file in Directory.GetFiles(minecraftPath, "*.jar"))
                    File.Delete(file);

                // Delete txt indicator file
                foreach (var file in Directory.GetFiles(minecraftPath, "*.txt"))
                    File.Delete(file);

                // Copy new mods
                foreach (var modFile in Directory.GetFiles(versionFolder, "*.jar"))
                {
                    var destFile = Path.Combine(minecraftPath, Path.GetFileName(modFile));
                    File.Copy(modFile, destFile);
                }

                // Copy version .txt
                var txtPath = Path.Combine(versionFolder, $"{selectedVersion}.txt");
                var destTxtPath = Path.Combine(minecraftPath, $"{selectedVersion}.txt");
                File.Copy(txtPath, destTxtPath, true);
                UpdateActiveModStatus();
                StatusTextBlock.Text = $"✅ Mods for {selectedVersion} activated.\n\n" + File.ReadAllText(txtPath);
                StatusTextBlock.Foreground = new SolidColorBrush(Colors.DarkGreen);
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"❌ Error switching mods: {ex.Message}";
                StatusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
            }
        }

        private void ModVersionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SelectedModVersion == null) return;

            var storagePath = ModStoragePathTextBox.Text;
            var versionFile = Path.Combine(storagePath, SelectedModVersion, $"{SelectedModVersion}.txt");

            if (File.Exists(versionFile))
                StatusTextBlock.Text = File.ReadAllText(versionFile);
        }

        private void BrowseMinecraftPath_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new WinForms.FolderBrowserDialog
            {
                Description = "Select the Minecraft mods folder"
            };

            if (dialog.ShowDialog() == WinForms.DialogResult.OK)
            {
                MinecraftModsPathTextBox.Text = dialog.SelectedPath;

                // Save the path to settings
                Settings.Default.MinecraftModsFolder = MinecraftModsPathTextBox.Text;
                Settings.Default.Save();

                LoadModVersions();
            }
        }

        private void BrowseModStoragePath_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new WinForms.FolderBrowserDialog
            {
                Description = "Select the mod storage folder (with subfolders per version)"
            };

            if (dialog.ShowDialog() == WinForms.DialogResult.OK)
            {
                ModStoragePathTextBox.Text = dialog.SelectedPath;

                // Save the path to settings
                Settings.Default.ModStorageFolder = ModStoragePathTextBox.Text;
                Settings.Default.Save();

                LoadModVersions();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            Settings.Default.MinecraftModsFolder = MinecraftModsPathTextBox.Text;
            Settings.Default.ModStorageFolder = ModStoragePathTextBox.Text;
            Settings.Default.Save();
        }
    }
}
