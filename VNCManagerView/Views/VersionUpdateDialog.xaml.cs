using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using AutoUpdaterDotNET;

namespace VNCManagerView
{
    public partial class VersionUpdateDialog : Window
    {
        private readonly UpdateInfoEventArgs _updateArgs;

        public VersionUpdateDialog(UpdateInfoEventArgs args)
        {
            InitializeComponent();
            _updateArgs = args;

            // Get current version from Assembly
            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";

            // Assign UI text
            CurrentVersionText.Text = $"Current: v{TrimVersion(currentVersion)}";
            LatestVersionText.Text = $"Latest: v{TrimVersion(args.CurrentVersion)}";

            // Highlight if update available
            if (IsUpToDate(currentVersion, args.CurrentVersion))
            {
                LatestVersionText.Text = $"v{args.CurrentVersion} (up to date)";
                LatestVersionText.Foreground = System.Windows.Media.Brushes.Gray;
                UpdateNowButton.IsEnabled = false;
            }
            else
            {
                LatestVersionText.Foreground = System.Windows.Media.Brushes.OrangeRed;
            }
        }

        /// <summary>
        /// Compare two version strings safely.
        /// </summary>
        private bool IsUpToDate(string current, string latest)
        {
            try
            {
                var vCurrent = new Version(current);
                var vLatest = new Version(latest);
                return vCurrent >= vLatest;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Handle Update button.
        /// </summary>
        private void UpdateNow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (AutoUpdater.DownloadUpdate(_updateArgs))
                {
                    Application.Current.Shutdown();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not start update:\n{ex.Message}",
                                "Update Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handle Changelog link click.
        /// </summary>
        private void ChangelogLink_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!string.IsNullOrEmpty(_updateArgs.ChangelogURL))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = _updateArgs.ChangelogURL,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not open changelog:\n{ex.Message}",
                                    "Error",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Handle "Later" button.
        /// </summary>
        private void Later_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private string TrimVersion(string version)
        {
            if (string.IsNullOrWhiteSpace(version))
                return version;

            // Try parse into Version object
            if (Version.TryParse(version, out var v))
            {
                // If Revision is 0, just return Major.Minor.Build
                if (v.Revision == 0)
                    return $"{v.Major}.{v.Minor}.{v.Build}";

                // Otherwise return full 4-part version
                return v.ToString();
            }

            return version;
        }
    }
}
