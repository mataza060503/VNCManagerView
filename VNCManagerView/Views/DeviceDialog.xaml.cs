// DeviceDialog.xaml.cs
using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media.Animation;
using VNCManagerView;

namespace VNCManagerView
{
    public partial class DeviceDialog : Window
    {
        public Device Device { get; private set; }
        public Branch SelectedBranch { get; private set; }
        public Plant SelectedPlant { get; private set; }

        private List<Branch> _branches;
        private Device _originalDevice;

        public DeviceDialog(List<Branch> branches, Device device = null)
        {
            InitializeComponent();
            _branches = branches;

            // Initialize branch combo box
            BranchComboBox.ItemsSource = _branches;

            if (device != null)
            {
                // Edit mode
                _originalDevice = device;
                Device = new Device
                {
                    Name = device.Name,
                    IP = device.IP,
                    Port = device.Port,
                    Password = device.Password
                };

                // Find the branch and plant that contain this device
                foreach (var branch in _branches)
                {
                    foreach (var plant in branch.Plants)
                    {
                        if (plant.Devices.Contains(device))
                        {
                            BranchComboBox.SelectedItem = branch;
                            PlantComboBox.SelectedItem = plant;
                            break;
                        }
                    }

                    if (BranchComboBox.SelectedItem != null)
                        break;
                }

                NameTextBox.Text = device.Name;
                IPTextBox.Text = device.IP;
                PortTextBox.Text = device.Port.ToString();
                PasswordTextBox.Password = device.Password;
            }
            else
            {
                // Add mode
                Device = new Device { Port = 5900 }; // Default VNC port
                PortTextBox.Text = "5900";

                // Select first branch and first plant by default
                if (_branches.Count > 0)
                {
                    BranchComboBox.SelectedIndex = 0;
                    if (((Branch)BranchComboBox.SelectedItem).Plants.Count > 0)
                    {
                        PlantComboBox.SelectedIndex = 0;
                    }
                }
            }

            // Apply entrance animation
            ApplyEntranceAnimation();

            // Focus the first control
            Loaded += (s, e) => BranchComboBox.Focus();
        }

        private void BranchComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (BranchComboBox.SelectedItem is Branch selectedBranch)
            {
                SelectedBranch = selectedBranch;
                PlantComboBox.ItemsSource = selectedBranch.Plants;

                // Auto-select first plant if available
                if (selectedBranch.Plants.Count > 0)
                {
                    PlantComboBox.SelectedIndex = 0;
                }
                else
                {
                    PlantComboBox.ItemsSource = null;
                    PlantComboBox.SelectedItem = null;
                }
            }
        }

        private void ApplyEntranceAnimation()
        {
            // Start with the dialog slightly scaled down and transparent
            this.Opacity = 0;
            DialogBorder.RenderTransform = new System.Windows.Media.ScaleTransform(0.9, 0.9);
            DialogBorder.RenderTransformOrigin = new Point(0.5, 0.5);

            // Create entrance animation
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300));
            var scaleXAnimation = new DoubleAnimation(0.9, 1, TimeSpan.FromMilliseconds(300));
            var scaleYAnimation = new DoubleAnimation(0.9, 1, TimeSpan.FromMilliseconds(300));

            // Apply easing for smooth animation
            var easingFunction = new BackEase
            {
                EasingMode = EasingMode.EaseOut,
                Amplitude = 0.1
            };

            scaleXAnimation.EasingFunction = easingFunction;
            scaleYAnimation.EasingFunction = easingFunction;

            // Start animations
            this.BeginAnimation(Window.OpacityProperty, fadeIn);
            ((System.Windows.Media.ScaleTransform)DialogBorder.RenderTransform).BeginAnimation(
                System.Windows.Media.ScaleTransform.ScaleXProperty, scaleXAnimation);
            ((System.Windows.Media.ScaleTransform)DialogBorder.RenderTransform).BeginAnimation(
                System.Windows.Media.ScaleTransform.ScaleYProperty, scaleYAnimation);
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput())
                return;

            // Get selected branch and plant
            SelectedBranch = BranchComboBox.SelectedItem as Branch;
            SelectedPlant = PlantComboBox.SelectedItem as Plant;

            if (SelectedBranch == null || SelectedPlant == null)
            {
                MessageBox.Show("Please select both a Branch and a Plant.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Device.Name = NameTextBox.Text.Trim();
            Device.IP = IPTextBox.Text.Trim();
            Device.Port = int.Parse(PortTextBox.Text.Trim());
            Device.Password = PasswordTextBox.Password;

            // If editing, remove from original plant if moved
            if (_originalDevice != null && SelectedPlant != _originalDevice.ParentPlant)
            {
                _originalDevice.ParentPlant?.Devices.Remove(_originalDevice);
            }

            ApplyExitAnimation(() => DialogResult = true);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            ApplyExitAnimation(() => DialogResult = false);
        }

        private bool ValidateInput()
        {
            // Validate device name
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                ShowValidationError("Please enter a device name.", NameTextBox);
                return false;
            }

            // Validate IP address
            if (string.IsNullOrWhiteSpace(IPTextBox.Text))
            {
                ShowValidationError("Please enter an IP address.", IPTextBox);
                return false;
            }

            if (!IsValidIPAddress(IPTextBox.Text.Trim()))
            {
                ShowValidationError("Please enter a valid IP address (e.g., 192.168.1.100).", IPTextBox);
                return false;
            }

            // Validate port
            if (string.IsNullOrWhiteSpace(PortTextBox.Text))
            {
                ShowValidationError("Please enter a port number.", PortTextBox);
                return false;
            }

            if (!int.TryParse(PortTextBox.Text.Trim(), out int port) || port < 1 || port > 65535)
            {
                ShowValidationError("Please enter a valid port number (1-65535).", PortTextBox);
                return false;
            }

            return true;
        }

        private bool IsValidIPAddress(string ip)
        {
            // Simple IP address validation regex
            var ipRegex = new Regex(@"^(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$");
            return ipRegex.IsMatch(ip);
        }

        private void ShowValidationError(string message, System.Windows.Controls.Control controlToFocus)
        {
            MessageBox.Show(message, "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            controlToFocus.Focus();
        }

        private void ApplyExitAnimation(Action onComplete)
        {
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(200));
            var scaleAnimation = new DoubleAnimation(1, 0.9, TimeSpan.FromMilliseconds(200));

            fadeOut.Completed += (s, e) => {
                if (this.IsLoaded && this.IsVisible)
                    onComplete?.Invoke();
            };

            this.BeginAnimation(Window.OpacityProperty, fadeOut);
            ((System.Windows.Media.ScaleTransform)DialogBorder.RenderTransform).BeginAnimation(
                System.Windows.Media.ScaleTransform.ScaleXProperty, scaleAnimation);
            ((System.Windows.Media.ScaleTransform)DialogBorder.RenderTransform).BeginAnimation(
                System.Windows.Media.ScaleTransform.ScaleYProperty, scaleAnimation);
        }
    }
}