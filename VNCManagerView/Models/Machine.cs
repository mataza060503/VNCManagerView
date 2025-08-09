// Models/Machine.cs
public class Machine
{
    public string Name { get; set; }
    public string IP { get; set; }
    public int Port { get; set; } = 5900;
    public string Password { get; set; } // Optional
}