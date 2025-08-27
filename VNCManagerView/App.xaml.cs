using System;
using System.Windows;
using AutoUpdaterDotNET;

namespace VNCManagerView
{
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // Subscribe to update events before starting the updater
            AutoUpdater.CheckForUpdateEvent += AutoUpdaterOnCheckForUpdateEvent;

            // Fixed update.xml URL (GitHub raw link)
            AutoUpdater.Start("https://raw.githubusercontent.com/mataza060503/VNCManagerView/refs/heads/master/update.xml");

            // Show main window
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
        }

        private void AutoUpdaterOnCheckForUpdateEvent(UpdateInfoEventArgs args)
        {
            if (args.Error != null)
            {
                MessageBox.Show($"Update check failed: {args.Error.Message}", "Updater Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (args.IsUpdateAvailable)
            {
                // Show custom update dialog on UI thread
                Current.Dispatcher.Invoke(() =>
                {
                    var dialog = new VersionUpdateDialog(args)
                    {
                        Owner = Current.MainWindow
                    };

                    // If user accepts → trigger download + restart
                    if (dialog.ShowDialog() == true)
                    {
                        if (AutoUpdater.DownloadUpdate(args))
                        {
                            Current.Shutdown();
                        }
                    }
                });
            }
        }
    }
}
