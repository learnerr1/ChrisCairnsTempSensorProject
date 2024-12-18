using System.Text.Json;
using Microsoft.Extensions.Logging;
using TemperatureSensor.App.Models;
using TemperatureSensor.App.Interfaces;
using System;
using System.Linq;
using System.Collections.Generic;

namespace TemperatureSensor.App.Services;

public class TempSensor : ITemperatureSensor
{
    private readonly IDataHistory _dataHistory;
    private readonly IDataLogger _dataLogger;
    private readonly IAlertService _alertService;
    private SensorConfig? _config;
    private bool _isRunning;
    private readonly ILogger<TempSensor> _logger;
    private readonly Random _random;
    private double _currentReading;
    private Timer? _timer;
    private bool _isInFaultState;
    private DateTime _faultStartTime;

    public string Name => _config?.Name ?? string.Empty;
    public string Location => _config?.Location ?? string.Empty;
    public bool IsRunning => _isRunning;

    public TempSensor(ILogger<TempSensor> logger, IDataHistory dataHistory, IDataLogger dataLogger, IAlertService alertService)
    {
        _logger = logger;
        _dataHistory = dataHistory;
        _dataLogger = dataLogger;
        _alertService = alertService;
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

            _alertService.UpdateThresholds(_config.WarningThreshold, _config.CriticalThreshold);
            _alertService.OnAlert += HandleAlert;
        
            var logPath = Path.Combine("logs", $"{_config.Name}_readings.log");
            var loggerInitialized = await _dataLogger.Initialize(logPath);
            if (!loggerInitialized)
            {
                _logger.LogError("Failed to initialize data logger");
                return false;
            }

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

        if (config.WarningThreshold >= config.CriticalThreshold)
        {
            _logger.LogError("Warning threshold must be less than critical threshold");
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
            _alertService.OnAlert -= HandleAlert;
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

        if (!_isInFaultState && ShouldInjectFault())
        {
            InjectFault();
        }

        if (_isInFaultState)
        {
            UpdateFaultReading();
        }
        else
        {
            double noise = (_random.NextDouble() * 2 - 1) * _config.NoiseLevel;
            _currentReading += noise;
            _currentReading = Math.Max(_config.MinValue, Math.Min(_config.MaxValue, _currentReading));
        }

        var validationResult = ValidateData(_currentReading);
        var alertLevel = _alertService.CheckThresholds(_currentReading);
        
        var sensorData = new SensorData
        {
            Temperature = Math.Round(_currentReading, 2),
            Timestamp = DateTime.UtcNow,
            IsValid = validationResult.IsValid,
            AlertLevel = alertLevel
        };

        await _dataHistory.StoreData(sensorData);
        await _dataLogger.LogData(sensorData);

        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Invalid reading detected: {Message}", validationResult.Message);
        }

        _logger.LogDebug("New reading for {SensorName}: {Reading}°C (Valid: {IsValid})", 
            Name, 
            _currentReading.ToString("F2"),
            validationResult.IsValid);
    }

    private void HandleAlert(object? sender, AlertEvent e)
    {
        Console.ForegroundColor = e.Level == AlertLevel.Critical ? ConsoleColor.Red : ConsoleColor.Yellow;
        Console.WriteLine(e.Message);
        Console.ResetColor();
    }

    public ValidationResult ValidateData(double temperature)
    {
        if (_config == null)
        {
            return new ValidationResult(false, "Sensor not initialized");
        }

        if (temperature < _config.MinValue || temperature > _config.MaxValue)
        {
            return new ValidationResult(
                false,
                $"Temperature {temperature}°C is outside valid range ({_config.MinValue}°C to {_config.MaxValue}°C)"
            );
        }
   
        var recentReadings = _dataHistory.GetHistory()
            .OrderByDescending(x => x.Timestamp)
            .Take(5)
            .ToList();

        if (!recentReadings.Any())
        {
            return new ValidationResult(true, "Temperature reading is valid");
        }

        var avgRecentTemp = recentReadings.Average(x => x.Temperature);
        var suddenChange = Math.Abs(temperature - avgRecentTemp);
    
        var validRange = _config.MaxValue - _config.MinValue;
        var maxAllowedChange = validRange * 0.2;
        
        if (suddenChange > maxAllowedChange)
        {
            return new ValidationResult(
                false,
                $"Suspicious rapid temperature change detected: {suddenChange:F2}°C difference from recent average"
            );
        }

        return new ValidationResult(true, "Temperature reading is valid");
    }

    public double GetSmoothedReading(int windowSize = 5)
    {
        if (!_isRunning || _config == null)
        {
            throw new InvalidOperationException("Sensor is not running or not initialized");
        }
        
        return _dataHistory.SmoothData(windowSize);
    }

    private bool ShouldInjectFault()
    {
        if (_config == null) return false;
        return _random.NextDouble() < _config.FaultProbability;
    }

    private void InjectFault()
    {
        _isInFaultState = true;
        _faultStartTime = DateTime.UtcNow;
        _logger.LogWarning("Fault injected in sensor {SensorName}", Name);
    }

    private void UpdateFaultReading()
    {
        if (_config == null) return;

        if ((DateTime.UtcNow - _faultStartTime).TotalMilliseconds >= _config.FaultDuration)
        {
            _isInFaultState = false;
            _logger.LogInformation("Fault cleared in sensor {SensorName}", Name);
            return;
        }

        double timeFactor = (DateTime.UtcNow - _faultStartTime).TotalSeconds / (_config.FaultDuration / 1000.0);
        double progressToFault = 1 - Math.Exp(-3 * timeFactor);
        _currentReading = _config.MaxValue + (_config.FaultTemperature - _config.MaxValue) * progressToFault;
    }
}