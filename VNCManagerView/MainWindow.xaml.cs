// MainWindow.xaml.cs
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using static VNCManagerView.SystemConfig;


namespace VNCManagerView
{
    public partial class MainWindow : Window
    {
        private const string SystemConfigFileName = "system_config.json";
        private SystemConfig _systemConfig = new SystemConfig();
        private const string ConfigFileName = "devices_config.json";
        private string _configFilePath;
        private ObservableCollection<TreeNodeViewModel> _treeNodes = new();

        public MainWindow()
        {
            InitializeComponent();
            LoadSystemConfig();
            FindConfigurationFile();
            LoadConfiguration();
            InitializeTreeView();

            SetWindowAndTaskbarIcon();
        }

        private void SetWindowAndTaskbarIcon()
        {
            try
            {
                var iconUri = new Uri("pack://application:,,,/VNCManagerView;component/Assets/main_icon.ico", UriKind.Absolute);
                this.Icon = BitmapFrame.Create(iconUri);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading icon: {ex.Message}");
                MessageBox.Show("Failed to load application icon: " + ex.Message);
            }
        }

        private void LoadSystemConfig()
        {
            var configPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "VNCManager",
                SystemConfigFileName);

            try
            {
                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath);
                    _systemConfig = JsonSerializer.Deserialize<SystemConfig>(json) ?? new SystemConfig();
                }

                // Apply config settings
                if (!string.IsNullOrEmpty(_systemConfig.LastDataFilePath))
                {
                    _configFilePath = _systemConfig.LastDataFilePath;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading system config: {ex.Message}");
            }
        }

        private void SaveSystemConfig()
        {
            var configDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "VNCManager");

            var configPath = Path.Combine(configDir, SystemConfigFileName);

            try
            {
                Directory.CreateDirectory(configDir);

                // Update config with current settings
                _systemConfig.LastDataFilePath = _configFilePath;

                var options = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(configPath, JsonSerializer.Serialize(_systemConfig, options));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving system config: {ex.Message}");
            }
        }

        private void FindConfigurationFile()
        {
            var searchPaths = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), ConfigFileName),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "VNCManager", ConfigFileName),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VNCManager", ConfigFileName)
            };

            foreach (var path in searchPaths)
            {
                if (File.Exists(path))
                {
                    _configFilePath = path;
                    return;
                }
            }

            var documentsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "VNCManager");
            Directory.CreateDirectory(documentsPath);
            _configFilePath = Path.Combine(documentsPath, ConfigFileName);
        }

        private void LoadConfiguration()
        {
            try
            {
                List<Branch> branches;
                if (File.Exists(_configFilePath))
                {
                    var json = File.ReadAllText(_configFilePath);
                    branches = JsonSerializer.Deserialize<List<Branch>>(json) ?? new List<Branch>();
                }
                else
                {
                    branches = CreateSampleConfig();
                    SaveConfiguration(branches);
                    MessageBox.Show($"Sample configuration file created at:\n{_configFilePath}\n\nPlease edit it with your actual devices.", "Configuration Created", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                ConvertToTreeNodes(branches);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading configuration: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                _treeNodes = new ObservableCollection<TreeNodeViewModel>();
            }
        }

        private void SaveConfiguration(List<Branch> branches)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(_configFilePath, JsonSerializer.Serialize(branches, options));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving configuration: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ConvertToTreeNodes(List<Branch> branches)
        {
            _treeNodes.Clear();
            foreach (var branch in branches)
            {
                var branchNode = new TreeNodeViewModel
                {
                    Name = branch.Name,
                    Type = NodeType.Branch,
                    Branch = branch,
                    Icon = "🏢",
                    IconBackground = new SolidColorBrush(Color.FromRgb(52, 152, 219)),
                    StatusColor = new SolidColorBrush(Color.FromRgb(46, 204, 113)),
                    IsExpanded = false
                };

                foreach (var plant in branch.Plants)
                {
                    var plantNode = new TreeNodeViewModel
                    {
                        Name = plant.Name,
                        Type = NodeType.Plant,
                        Plant = plant,
                        Branch = branch,
                        Icon = "🏭",
                        IconBackground = new SolidColorBrush(Color.FromRgb(155, 89, 182)),
                        StatusColor = new SolidColorBrush(Color.FromRgb(52, 152, 219)),
                        IsExpanded = false,
                        Parent = branchNode
                    };

                    foreach (var device in plant.Devices)
                    {
                        var deviceNode = new TreeNodeViewModel
                        {
                            Name = device.Name,
                            Type = NodeType.Device,
                            Device = device,
                            Plant = plant,
                            Branch = branch,
                            Icon = "💻",
                            IconBackground = new SolidColorBrush(Color.FromRgb(52, 73, 94)),
                            StatusColor = GetDeviceStatusColor(device),
                            ConnectionInfo = $"{device.IP}:{device.Port}",
                            Parent = plantNode
                        };
                        plantNode.Children.Add(deviceNode);
                    }
                    branchNode.Children.Add(plantNode);
                }
                _treeNodes.Add(branchNode);
            }
        }

        private SolidColorBrush GetDeviceStatusColor(Device device)
        {
            // You can implement actual connectivity checking here
            // For demo purposes, we'll randomly assign colors
            var random = new Random(device.Name.GetHashCode());
            var colors = new[]
            {
                Color.FromRgb(46, 204, 113),   // Green - Online
                //Color.FromRgb(241, 196, 15),   // Yellow - Warning
                //Color.FromRgb(231, 76, 60)     // Red - Offline
            };
            return new SolidColorBrush(colors[random.Next(colors.Length)]);
        }

        private List<Branch> CreateSampleConfig()
        {
            return new List<Branch>
            {
                new Branch
                {
                    Name = "Head Office",
                    Plants = new List<Plant>
                    {
                        new Plant
                        {
                            Name = "IT Department",
                            Devices = new List<Device>
                            {
                                new Device { Name = "Server Room PC", IP = "192.168.1.100", Port = 5900, Password = "admin123" },
                                new Device { Name = "Network Switch Console", IP = "192.168.1.101", Port = 5901, Password = "network456" }
                            }
                        },
                        new Plant
                        {
                            Name = "Reception",
                            Devices = new List<Device>
                            {
                                new Device { Name = "Reception Desktop", IP = "192.168.1.150", Port = 5900, Password = "reception789" }
                            }
                        }
                    }
                },
                new Branch
                {
                    Name = "Branch Office",
                    Plants = new List<Plant>
                    {
                        new Plant
                        {
                            Name = "Sales Department",
                            Devices = new List<Device>
                            {
                                new Device { Name = "Sales Manager PC", IP = "192.168.2.100", Port = 5900, Password = "sales2024" },
                                new Device { Name = "Conference Room PC", IP = "192.168.2.101", Port = 5900, Password = "conference" }
                            }
                        }
                    }
                }
            };
        }

        private void InitializeTreeView()
        {
            DeviceTreeView.ItemsSource = _treeNodes;
        }

        // New event handler for clickable expand/collapse
        private void TreeItemHeader_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is TreeNodeViewModel node)
            {
                // Find the parent TreeViewItem
                var treeViewItem = FindParent<TreeViewItem>(border);
                if (treeViewItem != null && treeViewItem.HasItems)
                {
                    // Toggle the expansion state
                    node.IsExpanded = !node.IsExpanded;
                    treeViewItem.IsExpanded = node.IsExpanded;

                    // Prevent the event from bubbling up
                    e.Handled = true;
                }
            }
        }

        // Helper method to find parent of specific type
        private T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);

            if (parentObject == null) return null;

            T parent = parentObject as T;
            if (parent != null)
                return parent;
            else
                return FindParent<T>(parentObject);
        }

        private void DeviceTreeView_Loaded(object sender, RoutedEventArgs e)
        {
            // Apply fade-in animation to the entire tree view
            var fadeIn = (Storyboard)FindResource("FadeInAnimation");
            if (fadeIn != null)
            {
                fadeIn.Begin(DeviceTreeView);
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            //// Add a small animation to the refresh button
            //var button = sender as Button;
            //var rotateTransform = new RotateTransform();
            //button.RenderTransform = rotateTransform;
            //button.RenderTransformOrigin = new Point(0.5, 0.5);

            //var animation = new DoubleAnimation(0, 360, TimeSpan.FromMilliseconds(500));
            //animation.Completed += (s, args) =>
            //{
            //    LoadConfiguration();
            //    InitializeTreeView();
            //};

            //rotateTransform.BeginAnimation(RotateTransform.AngleProperty, animation);

            LoadConfiguration();
            InitializeTreeView();
        }

        private void OpenConfigButton_Click(object sender, RoutedEventArgs e)
        {
            var configDialog = new ConfigDialog(_systemConfig.VncViewerPath, _configFilePath)
            {
                Owner = this
            };

            if (configDialog.ShowDialog() == true)
            {
                _systemConfig.VncViewerPath = configDialog.VncViewerPath;
                _configFilePath = configDialog.DataFilePath;
                _systemConfig.LastDataFilePath = _configFilePath;
                SaveSystemConfig();
                LoadConfiguration();
            }
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is TreeNodeViewModel node && node.Type == NodeType.Device)
            {
                LaunchVncViewer(node.Device);
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is TreeNodeViewModel node)
            {
                ShowAddDialog(node);
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is TreeNodeViewModel node)
            {
                var originalIsExpanded = node.IsExpanded;
                var parentExpansionStates = GetParentExpansionStates(node);

                ShowEditDialog(node);

                // Restore expansion states
                node.IsExpanded = originalIsExpanded;
                RestoreParentExpansionStates(node, parentExpansionStates);
            }
        }

        private Dictionary<TreeNodeViewModel, bool> GetParentExpansionStates(TreeNodeViewModel node)
        {
            var states = new Dictionary<TreeNodeViewModel, bool>();
            var current = node.Parent;
    
            while (current != null)
            {
                states[current] = current.IsExpanded;
                current = current.Parent;
            }
    
            return states;
        }

        private void RestoreParentExpansionStates(TreeNodeViewModel node, Dictionary<TreeNodeViewModel, bool> states)
        {
            var current = node.Parent;
    
            while (current != null)
            {
                if (states.TryGetValue(current, out var wasExpanded))
                {
                    current.IsExpanded = wasExpanded;
                }
                current = current.Parent;
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is TreeNodeViewModel node)
            {
                ShowDeleteDialog(node);
            }
        }

        private void ShowAddDialog(TreeNodeViewModel parentNode)
        {
            // Save current expansion states
            var originalIsExpanded = parentNode.IsExpanded;
            var parentExpansionStates = GetParentExpansionStates(parentNode);

            switch (parentNode.Type)
            {
                case NodeType.Branch:
                    var plantDialog = new AddEditDialog("Add Plant", "Plant Name:", "") { Owner = this };
                    if (plantDialog.ShowDialog() == true)
                    {
                        var newPlant = new Plant { Name = plantDialog.ResultText };
                        parentNode.Branch.Plants.Add(newPlant);

                        // Create new node and add to tree
                        var newPlantNode = new TreeNodeViewModel
                        {
                            Name = newPlant.Name,
                            Type = NodeType.Plant,
                            Plant = newPlant,
                            Branch = parentNode.Branch,
                            Icon = "🏭",
                            IconBackground = new SolidColorBrush(Color.FromRgb(155, 89, 182)),
                            StatusColor = new SolidColorBrush(Color.FromRgb(52, 152, 219)),
                            Parent = parentNode
                        };

                        parentNode.Children.Add(newPlantNode);
                        SaveConfiguration(GetCurrentBranches());
                    }
                    break;

                case NodeType.Plant:
                    // Pass the specific branch and plant to the dialog
                    var deviceDialog = new DeviceDialog(GetCurrentBranches(), null, parentNode.Branch, parentNode.Plant) { Owner = this };
                    if (deviceDialog.ShowDialog() == true)
                    {
                        parentNode.Plant.Devices.Add(deviceDialog.Device);

                        // Create new node and add to tree
                        var newDeviceNode = new TreeNodeViewModel
                        {
                            Name = deviceDialog.Device.Name,
                            Type = NodeType.Device,
                            Device = deviceDialog.Device,
                            Plant = parentNode.Plant,
                            Branch = parentNode.Branch,
                            Icon = "💻",
                            IconBackground = new SolidColorBrush(Color.FromRgb(52, 73, 94)),
                            StatusColor = GetDeviceStatusColor(deviceDialog.Device),
                            ConnectionInfo = $"{deviceDialog.Device.IP}:{deviceDialog.Device.Port}",
                            Parent = parentNode
                        };

                        parentNode.Children.Add(newDeviceNode);
                        SaveConfiguration(GetCurrentBranches());
                    }
                    break;
            }

            // Restore expansion states
            parentNode.IsExpanded = originalIsExpanded;
            RestoreParentExpansionStates(parentNode, parentExpansionStates);
        }

        private void AddBranchButton_Click(object sender, RoutedEventArgs e)
        {
            var branchDialog = new AddEditDialog("Add Branch", "Branch Name:", "");
            if (branchDialog.ShowDialog() == true)
            {
                var branches = GetCurrentBranches();
                branches.Add(new Branch { Name = branchDialog.ResultText });
                SaveConfiguration(branches);
                LoadConfiguration();
                InitializeTreeView();
            }
        }

        private void ShowEditDialog(TreeNodeViewModel node)
        {
            // Save expansion states before any changes
            var originalIsExpanded = node.IsExpanded;
            var parentExpansionStates = GetParentExpansionStates(node);

            switch (node.Type)
            {
                case NodeType.Branch:
                    var branchDialog = new AddEditDialog("Edit Branch", "Branch Name:", node.Branch.Name) { Owner = this };
                    if (branchDialog.ShowDialog() == true)
                    {
                        node.Branch.Name = branchDialog.ResultText;
                        node.UpdateFromBranch(node.Branch);
                        SaveConfiguration(GetCurrentBranches());
                    }
                    break;

                case NodeType.Plant:
                    var plantDialog = new AddEditDialog("Edit Plant", "Plant Name:", node.Plant.Name) { Owner = this };
                    if (plantDialog.ShowDialog() == true)
                    {
                        node.Plant.Name = plantDialog.ResultText;
                        node.UpdateFromPlant(node.Plant);
                        SaveConfiguration(GetCurrentBranches());
                    }
                    break;

                case NodeType.Device:
                    var deviceDialog = new DeviceDialog(GetCurrentBranches(), node.Device) { Owner = this };
                    if (deviceDialog.ShowDialog() == true)
                    {
                        // If plant changed, move the device
                        if (deviceDialog.SelectedPlant != node.Plant)
                        {
                            node.Plant.Devices.Remove(node.Device);
                            deviceDialog.SelectedPlant.AddDevice(deviceDialog.Device);
                        }

                        node.UpdateFromDevice(deviceDialog.Device);
                        SaveConfiguration(GetCurrentBranches());

                        // If branch/plant changed, rebuild the tree
                        if (deviceDialog.SelectedBranch != node.Branch ||
                            deviceDialog.SelectedPlant != node.Plant)
                        {
                            LoadConfiguration();
                            InitializeTreeView();
                        }
                    }
                    break;
            }

            // Restore expansion states after all operations
            node.IsExpanded = originalIsExpanded;
            RestoreParentExpansionStates(node, parentExpansionStates);
        }

        private void ShowDeleteDialog(TreeNodeViewModel node)
        {
            var result = MessageBox.Show($"Are you sure you want to delete '{node.Name}'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                switch (node.Type)
                {
                    case NodeType.Branch:
                        var branches = GetCurrentBranches();
                        branches.Remove(node.Branch);
                        SaveConfiguration(branches);
                        break;
                    case NodeType.Plant:
                        node.Branch.Plants.Remove(node.Plant);
                        SaveAndRefresh();
                        break;
                    case NodeType.Device:
                        node.Plant.Devices.Remove(node.Device);
                        SaveAndRefresh();
                        break;
                }
                LoadConfiguration();
                InitializeTreeView();
            }
        }

        private List<Branch> GetCurrentBranches()
        {
            return _treeNodes.Select(n => n.Branch).ToList();
        }

        private void SaveAndRefresh()
        {
            var branches = GetCurrentBranches();
            SaveConfiguration(branches);
        }

        private void LaunchVncViewer(Device device)
        {
            try
            {
                var args = $"-host={device.IP} -port={device.Port}";
                if (!string.IsNullOrEmpty(device.Password))
                {
                    args += $" -password={device.Password}";
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = _systemConfig.VncViewerPath, // Use configured path
                    Arguments = args,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to launch VNC viewer for {device.Name}: {ex.Message}",
                              "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    // Enums and ViewModels
    public enum NodeType { Branch, Plant, Device }

    public class TreeNodeViewModel : INotifyPropertyChanged
    {
        private bool _isExpanded;

        public string Name { get; set; }
        public string Icon { get; set; }
        public string ConnectionInfo { get; set; }
        public SolidColorBrush IconBackground { get; set; }
        public SolidColorBrush StatusColor { get; set; }
        public NodeType Type { get; set; }
        public Branch Branch { get; set; }
        public Plant Plant { get; set; }
        public Device Device { get; set; }
        public TreeNodeViewModel Parent { get; set; }
        public ObservableCollection<TreeNodeViewModel> Children { get; set; } = new();

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                _isExpanded = value;
                OnPropertyChanged(nameof(IsExpanded));
            }
        }

        public bool CanAddChildren => Type == NodeType.Branch || Type == NodeType.Plant;
        public bool IsDevice => Type == NodeType.Device;

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void UpdateFromBranch(Branch branch)
        {
            Name = branch.Name;
            OnPropertyChanged(nameof(Name));
        }

        public void UpdateFromPlant(Plant plant)
        {
            Name = plant.Name;
            OnPropertyChanged(nameof(Name));
        }

        public void UpdateFromDevice(Device device)
        {
            Name = device.Name;
            ConnectionInfo = $"{device.IP}:{device.Port}";
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(ConnectionInfo));
            OnPropertyChanged(nameof(StatusColor));
        }
    }

    // Model classes
    public class Branch
    {
        public string Name { get; set; }
        public List<Plant> Plants { get; set; } = new();
    }

    public class Plant
    {
        public string Name { get; set; }
        public List<Device> Devices { get; set; } = new();

        public void AddDevice(Device device)
        {
            device.ParentPlant = this;
            Devices.Add(device);
        }
    }

    public class Device
    {
        public string Name { get; set; }
        public string IP { get; set; }
        public int Port { get; set; }
        public string Password { get; set; }

        [JsonIgnore] // To prevent circular references in serialization
        public Plant ParentPlant { get; set; }
    }

    public class SystemConfig
    {
        public string VncViewerPath { get; set; } = @"C:\Program Files\TightVNC\tvnviewer.exe";
        public string LastDataFilePath { get; set; }
    }

}