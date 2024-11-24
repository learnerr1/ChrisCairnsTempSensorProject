namespace TemperatureSensor.App.Interfaces;

using System.Collections.Generic;
using System.Threading.Tasks;
using TemperatureSensor.App.Models;
public interface IDataLogger
{
    Task<bool> LogData(SensorData data);
    Task<bool> Initialize(string logPath);
}