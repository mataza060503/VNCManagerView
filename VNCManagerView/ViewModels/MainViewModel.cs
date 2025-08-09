// ViewModels/MainViewModel.cs
using System.Collections.ObjectModel;

public class MainViewModel
{
    public ObservableCollection<Machine> Machines { get; } = new();
    private readonly VncService _vncService = new();

    public MainViewModel()
    {
        // Sample data (replace with config file later)
        Machines.Add(new Machine { Name = "Machine 1", IP = "10.13.105.231" });
        Machines.Add(new Machine { Name = "Machine 2", IP = "10.13.105.232" });
    }

    public void ConnectToMachine(Machine machine) => _vncService.LaunchVncViewer(machine);
}