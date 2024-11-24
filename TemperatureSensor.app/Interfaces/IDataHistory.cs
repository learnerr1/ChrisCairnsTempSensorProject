namespace TemperatureSensor.App.Interfaces;

using System.Collections.Generic;
using System.Threading.Tasks;
using TemperatureSensor.App.Models;
public interface IDataHistory
{
    Task<bool> StoreData(SensorData data);
    IEnumerable<SensorData> GetHistory();
    double GetAverageTemperature();
    void ClearHistory();
    
    double SmoothData(int windowSize = 5);
}