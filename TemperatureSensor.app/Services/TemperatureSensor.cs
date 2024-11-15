using System.Text.Json;
using Microsoft.Extensions.Logging;
using TemperatureSensor.App.Models;
using TemperatureSensor.App.Interfaces;

namespace TemperatureSensor.App.Services;

public class TempSensor : ITemperatureSensor
{
    private SensorConfig? _config;
    private bool _isRunning;
    private readonly ILogger<TempSensor> _logger;
    public string Name => _config?.Name ?? string.Empty;
    public string Location => _config?.Location ?? string.Empty;
    public bool IsRunning => _isRunning;

    public TempSensor(ILogger<TempSensor> logger)
    {
        _logger = logger;
        _isRunning = false;
    }

    public async Task<bool> InitializeSensor(string configPath)
    {
        try
        {
            if (!File.Exists(configPath))
            {
                _logger.LogError("Configuration file not found: {ConfigPath}", configPath);
                return false;
            }

            string jsonString = await File.ReadAllTextAsync(configPath);
            var config = JsonSerializer.Deserialize<SensorConfig>(jsonString);

            if (config == null)
            {
                _logger.LogError("Failed to deserialize configuration");
                return false;
            }

            if (!ValidateConfig(config))
            {
                _logger.LogError("Invalid configuration");
                return false;
            }

            _config = config;
            _logger.LogInformation("Sensor initialized successfully: {SensorName}", Name);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing sensor");
            return false;
        }
    }

    private bool ValidateConfig(SensorConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.Name))
        {
            _logger.LogError("Sensor name is required");
            return false;
        }

        if (string.IsNullOrWhiteSpace(config.Location))
        {
            _logger.LogError("Sensor location is required");
            return false;
        }

        if (config.MinValue >= config.MaxValue)
        {
            _logger.LogError("MinValue must be less than MaxValue");
            return false;
        }

        if (config.ReadingIntervalMs <= 0)
        {
            _logger.LogError("ReadingIntervalMs must be greater than 0");
            return false;
        }

        return true;
    }

    public Task StartSensor()
    {
        _isRunning = true;
        return Task.CompletedTask;
    }

    public Task StopSensor()
    {
        _isRunning = false;
        return Task.CompletedTask;
    }

    public double GetCurrentReading()
    {
        if (!_isRunning || _config == null)
        {
            throw new InvalidOperationException("Sensor is not running or not initialized");
        }
        return 0.0;
    }
}