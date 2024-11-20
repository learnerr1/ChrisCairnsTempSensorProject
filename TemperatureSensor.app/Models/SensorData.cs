namespace TemperatureSensor.App.Models;

public class SensorData
{
    public double Temperature { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsValid { get; set; }
}