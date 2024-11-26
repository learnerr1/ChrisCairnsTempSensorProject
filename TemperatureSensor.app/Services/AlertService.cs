using Microsoft.Extensions.Logging;
using TemperatureSensor.App.Interfaces;
using TemperatureSensor.App.Models;

namespace TemperatureSensor.App.Services;

public class AlertService : IAlertService
{
    private readonly ILogger<AlertService> _logger;
    private double _warningThreshold;
    private double _criticalThreshold;
    public event EventHandler<AlertEvent>? OnAlert;

    public AlertService(ILogger<AlertService> logger)
    {
        _logger = logger;
    }

    public void UpdateThresholds(double warningThreshold, double criticalThreshold)
    {
        if (warningThreshold >= criticalThreshold)
        {
            throw new ArgumentException("Warning threshold must be less than critical threshold");
        }

        _warningThreshold = warningThreshold;
        _criticalThreshold = criticalThreshold;
        _logger.LogInformation("Alert thresholds updated - Warning: {Warning}°C, Critical: {Critical}°C", 
            warningThreshold, criticalThreshold);
    }

    public AlertLevel CheckThresholds(double temperature)
    {
        var level = AlertLevel.Normal;
        string? message = null;

        if (temperature >= _criticalThreshold)
        {
            level = AlertLevel.Critical;
            message = $"CRITICAL: Temperature {temperature:F2}°C exceeds critical threshold of {_criticalThreshold:F2}°C";
        }
        else if (temperature >= _warningThreshold)
        {
            level = AlertLevel.Warning;
            message = $"WARNING: Temperature {temperature:F2}°C exceeds warning threshold of {_warningThreshold:F2}°C";
        }

        if (level != AlertLevel.Normal)
        {
            var alertEvent = new AlertEvent(level, temperature, message!);
            OnAlert?.Invoke(this, alertEvent);
            _logger.LogWarning(message);
        }

        return level;
    }
}