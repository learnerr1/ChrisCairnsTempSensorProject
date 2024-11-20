using Microsoft.Extensions.Logging;
using TemperatureSensor.App.Interfaces;
using TemperatureSensor.App.Models;

namespace TemperatureSensor.App.Services;

public class DataHistory : IDataHistory
{
    private readonly List<SensorData> _history;
    private readonly ILogger<DataHistory> _logger;
    private const int MAX_HISTORY_SIZE = 1000; // Maintain last 1000 readings

    public DataHistory(ILogger<DataHistory> logger)
    {
        _history = new List<SensorData>();
        _logger = logger;
    }

    public Task<bool> StoreData(SensorData data)
    {
        try
        {
            _history.Add(data);
            
            // Keep history size manageable by removing oldest entries
            if (_history.Count > MAX_HISTORY_SIZE)
            {
                _history.RemoveRange(0, _history.Count - MAX_HISTORY_SIZE);
            }

            _logger.LogDebug("Stored new reading: {Temperature}°C at {Timestamp}", 
                data.Temperature, data.Timestamp);
                
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing temperature reading");
            return Task.FromResult(false);
        }
    }

    public IEnumerable<SensorData> GetHistory()
    {
        return _history.AsReadOnly();
    }

    public double GetAverageTemperature()
    {
        if (!_history.Any())
            return 0;

        return _history.Average(d => d.Temperature);
    }

    public void ClearHistory()
    {
        _history.Clear();
        _logger.LogInformation("History cleared");
    }
}