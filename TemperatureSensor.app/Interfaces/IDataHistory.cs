namespace TemperatureSensor.App.Interfaces;

public interface IDataHistory
{
    Task<bool> StoreData(SensorData data);
    IEnumerable<SensorData> GetHistory();
    double GetAverageTemperature();
    void ClearHistory();
}