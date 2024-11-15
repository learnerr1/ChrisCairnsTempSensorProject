namespace TemperatureSensor.App.Interfaces;

public interface ITemperatureSensor
{
    string Name { get; }
    string Location { get; }
    bool IsRunning { get; }
    Task<bool> InitializeSensor(string configPath);
    Task StartSensor();
    Task StopSensor();
    double GetCurrentReading();
}