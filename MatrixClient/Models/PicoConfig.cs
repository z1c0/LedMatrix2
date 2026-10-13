namespace MatrixClient.Models;

public class PicoConfig
{
    public string Url { get; set; } = "http://192.168.1.225/";
    public double TimeoutSeconds { get; set; } = 5;
    // How often to check whether the matrix is powered; bounds how long it stays dark after power-on
    public double ProbeSeconds { get; set; } = 60;
    // The Pico's WiFi can take a few seconds to answer after a quiet spell
    public double ProbeTimeoutSeconds { get; set; } = 5;
}
