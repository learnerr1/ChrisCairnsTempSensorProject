
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TemperatureSensor.App.Models;
using TemperatureSensor.App.Interfaces;

namespace TemperatureSensor.App.Services;

public class TempSensor : ITemperatureSensor
{
    private readonly IDataHistory _dataHistory;
    private SensorConfig? _config;
    private bool _isRunning;
    private readonly ILogger<TempSensor> _logger;
    private readonly Random _random;
    private double _currentReading;
    private Timer? _timer;

    public string Name => _config?.Name ?? string.Empty;
    public string Location => _config?.Location ?? string.Empty;
    public bool IsRunning => _isRunning;

    public TempSensor(ILogger<TempSensor> logger, IDataHistory dataHistory)
    {
        _logger = logger;
        _dataHistory = dataHistory;
        _isRunning = false;
        _random = new Random();
        _currentReading = 0;
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
            // Initialize with base temperature
            _currentReading = (_config.MaxValue + _config.MinValue) / 2;
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
        if (_config == null)
        {
            throw new InvalidOperationException("Sensor must be initialized before starting");
        }

        if (!_isRunning)
        {
            _isRunning = true;
            _timer = new Timer(SimulateReading, null, 0, _config.ReadingIntervalMs);
            _logger.LogInformation("Sensor started: {SensorName}", Name);
        }

        return Task.CompletedTask;
    }

    public Task StopSensor()
    {
        if (_isRunning)
        {
            _isRunning = false;
            _timer?.Dispose();
            _timer = null;
            _logger.LogInformation("Sensor stopped: {SensorName}", Name);
        }

        return Task.CompletedTask;
    }

    public double GetCurrentReading()
    {
        if (!_isRunning || _config == null)
        {
            throw new InvalidOperationException("Sensor is not running or not initialized");
        }
        
        return _currentReading;
    }

    private async void SimulateReading(object? state)
    {
        if (!_isRunning || _config == null) return;

        // Add random noise to the current reading
        double noise = (_random.NextDouble() * 2 - 1) * _config.NoiseLevel;
        _currentReading += noise;

        // Ensure reading stays within bounds
        _currentReading = Math.Max(_config.MinValue, Math.Min(_config.MaxValue, _currentReading));

        var sensorData = new SensorData
        {
            Temperature = _currentReading,
            Timestamp = DateTime.UtcNow,
            IsValid = true // You can implement validation logic here
        };

        await _dataHistory.StoreData(sensorData);

        _logger.LogDebug("New reading for {SensorName}: {Reading}°C", Name, _currentReading.ToString("F2"));
    }
}