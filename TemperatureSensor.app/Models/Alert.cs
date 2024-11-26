namespace TemperatureSensor.App.Models;

public enum AlertLevel
{
    Normal,
    Warning,
    Critical
}

public class AlertConfig
{
    public double WarningThreshold { get; set; }
    public double CriticalThreshold { get; set; }
}

public class AlertEvent
{
    public DateTime Timestamp { get; set; }
    public AlertLevel Level { get; set; }
    public double Temperature { get; set; }
    public string Message { get; set; }

    public AlertEvent(AlertLevel level, double temperature, string message)
    {
        Timestamp = DateTime.UtcNow;
        Level = level;
        Temperature = temperature;
        Message = message;
    }
}