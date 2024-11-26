using Microsoft.Extensions.Logging;
using TemperatureSensor.App.Models;

namespace TemperatureSensor.App.Interfaces;

public interface IAlertService
{
    void UpdateThresholds(double warningThreshold, double criticalThreshold);
    AlertLevel CheckThresholds(double temperature);
    event EventHandler<AlertEvent> OnAlert;
}