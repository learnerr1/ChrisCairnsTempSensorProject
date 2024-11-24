namespace TemperatureSensor.App.Interfaces;
using TemperatureSensor.App.Models;
public interface ITemperatureSensor
{
    string Name { get; }
    string Location { get; }
    bool IsRunning { get; }
    Task<bool> InitializeSensor(string configPath);
    Task StartSensor();
    Task StopSensor();
    double GetCurrentReading();
    ValidationResult ValidateData(double temperature);
    double GetSmoothedReading(int windowSize = 5);
}