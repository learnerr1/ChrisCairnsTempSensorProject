using System;
using System.Text;
using System.IO;
using Microsoft.Extensions.Logging;
using TemperatureSensor.App.Interfaces;
using TemperatureSensor.App.Models;

namespace TemperatureSensor.App.Services;

public class FileLogger : IDataLogger
{
    private readonly ILogger<FileLogger> _logger;
    private readonly IDataHistory _dataHistory;
    private string _logPath = string.Empty;

    public FileLogger(ILogger<FileLogger> logger, IDataHistory dataHistory)
    {
        _logger = logger;
        _dataHistory = dataHistory;
    }

    public async Task<bool> Initialize(string logPath)
    {
        try
        {
            var directory = Path.GetDirectoryName(logPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (!File.Exists(logPath))
            {
            
                await File.WriteAllTextAsync(logPath, 
                    $"Temperature Sensor Log - Started {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                    "=================================================\n\n");
            }
            else 
            {
            
                await File.AppendAllTextAsync(logPath, 
                    $"\nNew Session Started - {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                    "=================================================\n\n");
            }

            _logPath = logPath;
            
            _logger.LogInformation("File logger initialized with path: {LogPath}", logPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize file logger at path: {LogPath}", logPath);
            return false;
        }
    }

    public async Task<bool> LogData(SensorData data)
    {
        if (string.IsNullOrEmpty(_logPath))
        {
            _logger.LogError("Logger not initialized");
            return false;
        }

        try
        {
            var smoothedTemp = _dataHistory.SmoothData();
            
            var logEntry = new StringBuilder();
            logEntry.AppendLine($"Timestamp: {data.Timestamp:yyyy-MM-dd HH:mm:ss.fff}");
            logEntry.AppendLine($"Temperature: {data.Temperature:F2}°C");
            logEntry.AppendLine($"Smoothed Average: {smoothedTemp:F2}°C");
            logEntry.AppendLine($"Valid: {data.IsValid}");
            logEntry.AppendLine("-------------------");

            await File.AppendAllTextAsync(_logPath, logEntry.ToString());

            _logger.LogDebug("Successfully logged sensor data");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log sensor data");
            return false;
        }
    }
}
