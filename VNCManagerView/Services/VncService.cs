// Services/VncService.cs
using System.ComponentModel;
using System.Diagnostics;
using System.IO;

public class VncService
{
    private const string TightVncPath = @"C:\Program Files\TightVNC\tvnviewer.exe";

    public void LaunchVncViewer(Machine machine)
    {
        if (!File.Exists(TightVncPath))
        {
            throw new FileNotFoundException($"TightVNC not found at: {TightVncPath}");
        }

        var args = $"-host={machine.IP} -port={machine.Port}";
        if (!string.IsNullOrEmpty(machine.Password))
        {
            args += $" -password={machine.Password}";
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = TightVncPath,
            Arguments = args,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            Process.Start(startInfo);
        }
        catch (Win32Exception ex)
        {
            // Handle cases where UAC blocks the execution
            startInfo.Verb = "runas"; // Run as administrator if needed
            Process.Start(startInfo);
        }
    }
}