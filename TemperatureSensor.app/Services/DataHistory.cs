using Microsoft.Extensions.Logging;
using TemperatureSensor.App.Interfaces;
using TemperatureSensor.App.Models;

namespace TemperatureSensor.App.Services;

public class DataHistory : IDataHistory
{
    private readonly List<SensorData> _history;
    private readonly ILogger<DataHistory> _logger;
    private const int MAX_HISTORY_SIZE = 1000; 

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

    public double SmoothData(int windowSize = 5)
    {
        try
        {
            if (!_history.Any())
            {
                _logger.LogWarning("No data available for smoothing");
                return 0;
            }

   
            windowSize = Math.Min(windowSize, _history.Count);
            windowSize = Math.Max(1, windowSize);

            var recentReadings = _history
                .OrderByDescending(x => x.Timestamp)
                .Take(windowSize)
                .ToList();

         
            double smoothedValue = 0;
            double weightSum = 0;
            
            for (int i = 0; i < recentReadings.Count; i++)
            {
              
                double weight = windowSize - i;
                smoothedValue += recentReadings[i].Temperature * weight;
                weightSum += weight;
            }

            var result = Math.Round(smoothedValue / weightSum, 2);
            
            _logger.LogDebug(
                "Smoothed temperature calculated: {Temperature}°C (window size: {WindowSize})", 
                result, 
                windowSize
            );

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating smoothed temperature");
            return _history.LastOrDefault()?.Temperature ?? 0;
        }
    }
}