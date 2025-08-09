using Microsoft.Win32;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace VNCManagerView
{
    public partial class ConfigDialog : Window
    {
        public string VncViewerPath { get; set; }
        public string DataFilePath { get; set; }
        private string _originalDataPath { get; set; }
        public bool DataFilePathChanged { get; private set; }
        public ConfigDialog(string currentVncPath, string currentDataPath)
        {
            InitializeComponent();
            VncViewerPath = currentVncPath;
            DataFilePath = currentDataPath;
            _originalDataPath = currentDataPath; // Store original for comparison
            DataContext = this;
        }

        private void BrowseVncViewer_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Executable files (*.exe)|*.exe",
                InitialDirectory = Path.GetDirectoryName(VncViewerPath) ?? "C:\\Program Files"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                VncViewerPath = openFileDialog.FileName;
                VncPathTextBox.Text = VncViewerPath;
            }
        }

        private void BrowseDataFile_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json",
                InitialDirectory = Path.GetDirectoryName(DataFilePath) ??
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                FileName = Path.GetFileName(DataFilePath)
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                DataFilePath = saveFileDialog.FileName;
                DataFileTextBox.Text = DataFilePath;
            }
        }

        private void ImportJson_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json",
                Multiselect = false,
                InitialDirectory = Path.GetDirectoryName(DataFilePath) ??
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    // Read the import file
                    string importJson = File.ReadAllText(openFileDialog.FileName);

                    // Validate JSON format by attempting to deserialize
                    var importedBranches = JsonSerializer.Deserialize<List<Branch>>(importJson);
                    if (importedBranches == null)
                    {
                        throw new Exception("The file doesn't contain valid branch data.");
                    }

                    // Read current data file
                    List<Branch> currentBranches;
                    if (File.Exists(DataFilePath))
                    {
                        string currentJson = File.ReadAllText(DataFilePath);
                        currentBranches = JsonSerializer.Deserialize<List<Branch>>(currentJson) ?? new List<Branch>();
                    }
                    else
                    {
                        currentBranches = new List<Branch>();
                    }

                    // Merge data (simple merge by adding all branches)
                    foreach (var importedBranch in importedBranches)
                    {
                        // Check if branch already exists
                        var existingBranch = currentBranches.FirstOrDefault(b => b.Name == importedBranch.Name);

                        if (existingBranch != null)
                        {
                            // Merge plants if branch exists
                            foreach (var importedPlant in importedBranch.Plants)
                            {
                                var existingPlant = existingBranch.Plants.FirstOrDefault(p => p.Name == importedPlant.Name);

                                if (existingPlant != null)
                                {
                                    // Merge devices if plant exists
                                    foreach (var importedDevice in importedPlant.Devices)
                                    {
                                        // Only add if device doesn't exist (based on IP:Port combination)
                                        if (!existingPlant.Devices.Any(d =>
                                            d.IP == importedDevice.IP && d.Port == importedDevice.Port))
                                        {
                                            existingPlant.Devices.Add(importedDevice);
                                        }
                                    }
                                }
                                else
                                {
                                    // Add new plant
                                    existingBranch.Plants.Add(importedPlant);
                                }
                            }
                        }
                        else
                        {
                            // Add new branch
                            currentBranches.Add(importedBranch);
                        }
                    }

                    // Save merged data back to file
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    File.WriteAllText(DataFilePath, JsonSerializer.Serialize(currentBranches, options));

                    MessageBox.Show($"Successfully imported and merged data from: {openFileDialog.FileName}",
                                  "Import Successful", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Set flag that data was changed
                    DataFilePathChanged = true;
                }
                catch (JsonException jsonEx)
                {
                    MessageBox.Show($"Invalid JSON format: {jsonEx.Message}",
                                  "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error importing configuration: {ex.Message}",
                                  "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExportJson_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json",
                DefaultExt = "json",
                InitialDirectory = Path.GetDirectoryName(DataFilePath) ??
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                FileName = "VNCManager_Export_" + DateTime.Now.ToString("yyyyMMdd") + ".json"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    // Check if current data file exists
                    if (!File.Exists(DataFilePath))
                    {
                        MessageBox.Show("No configuration data found to export.",
                                      "Export Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Read current data
                    string currentJson = File.ReadAllText(DataFilePath);

                    // Validate JSON format by attempting to deserialize
                    var currentBranches = JsonSerializer.Deserialize<List<Branch>>(currentJson);
                    if (currentBranches == null)
                    {
                        throw new Exception("Current configuration data is invalid.");
                    }

                    // Write to export file
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    File.WriteAllText(saveFileDialog.FileName, JsonSerializer.Serialize(currentBranches, options));

                    MessageBox.Show($"Configuration successfully exported to: {saveFileDialog.FileName}",
                                  "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (JsonException jsonEx)
                {
                    MessageBox.Show($"Invalid JSON format in current data: {jsonEx.Message}",
                                  "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error exporting configuration: {ex.Message}",
                                  "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(VncViewerPath))
            {
                MessageBox.Show("Please select a valid VNC viewer path", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            DataFilePathChanged = DataFilePath != _originalDataPath;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}