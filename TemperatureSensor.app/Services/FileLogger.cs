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
    private string _logPath = string.Empty;

    public FileLogger(ILogger<FileLogger> logger)
    {
        _logger = logger;
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

            // Only create a new file if it doesn't exist
            if (!File.Exists(logPath))
            {
                // Add a header when creating a new file
                await File.WriteAllTextAsync(logPath, 
                    $"Temperature Sensor Log - Started {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                    "=================================================\n\n");
            }
            else 
            {
                // Add a separator when appending to existing file
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
            var logEntry = new StringBuilder();
            logEntry.AppendLine($"Timestamp: {data.Timestamp:yyyy-MM-dd HH:mm:ss.fff}");
            logEntry.AppendLine($"Temperature: {data.Temperature:F2}°C");
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