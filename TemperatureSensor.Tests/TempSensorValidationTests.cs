using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using TemperatureSensor.App.Services;
using TemperatureSensor.App.Interfaces;
using TemperatureSensor.App.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace TemperatureSensor.Tests;

public class TempSensorValidationTests : IDisposable
{
    private readonly Mock<ILogger<TempSensor>> _loggerMock;
    private readonly Mock<IDataHistory> _dataHistoryMock;
    private readonly TempSensor _sensor;
    private const string TEST_CONFIG_PATH = "test_config.json";

    public TempSensorValidationTests()
    {
        _loggerMock = new Mock<ILogger<TempSensor>>();
        _dataHistoryMock = new Mock<IDataHistory>();
        _sensor = new TempSensor(_loggerMock.Object, _dataHistoryMock.Object);
    }

    [Fact]
    public void ValidateData_UninitializedSensor_ReturnsInvalid()
    {
        var result = _sensor.ValidateData(23.0);

        Assert.False(result.IsValid);
        Assert.Contains("not initialized", result.Message);
    }

    [Theory]
    [InlineData(21.9)] 
    [InlineData(24.1)]
    public async Task ValidateData_OutsideRange_ReturnsInvalid(double temperature)
    {
        await InitializeSensorWithConfig();

        var result = _sensor.ValidateData(temperature);

        Assert.False(result.IsValid);
        Assert.Contains("outside valid range", result.Message);
    }

    [Theory]
    [InlineData(22.5)]
    [InlineData(23.0)]
    [InlineData(23.5)]
    public async Task ValidateData_WithinRange_ReturnsValid(double temperature)
    {
        await InitializeSensorWithConfig();

        var result = _sensor.ValidateData(temperature);

        Assert.True(result.IsValid);
        Assert.Equal("Temperature reading is valid", result.Message);
    }

    [Fact]
    public async Task ValidateData_RapidChange_ReturnsInvalid()
    {
        await InitializeSensorWithConfig();
        
        var recentReadings = new List<SensorData>
        {
            new() { Temperature = 23.0, Timestamp = DateTime.UtcNow.AddMinutes(-3) },
            new() { Temperature = 23.1, Timestamp = DateTime.UtcNow.AddMinutes(-2) },
            new() { Temperature = 23.0, Timestamp = DateTime.UtcNow.AddMinutes(-1) }
        };
        
        _dataHistoryMock.Setup(x => x.GetHistory())
            .Returns(recentReadings);

        var result = _sensor.ValidateData(25.0); 

        Assert.False(result.IsValid);
        Assert.Contains("rapid temperature change", result.Message);
    }

    [Fact]
    public void ValidateData_NoHistoricalData_ValidatesCurrentReadingOnly()
    {
        var emptyHistory = new List<SensorData>();
        _dataHistoryMock.Setup(x => x.GetHistory())
            .Returns(emptyHistory);

        var result = _sensor.ValidateData(23.0);

        Assert.True(result.IsValid);
    }

    private async Task InitializeSensorWithConfig()
    {
        var configJson = @"{
            ""Name"": ""TestSensor"",
            ""Location"": ""TestLocation"",
            ""MinValue"": 22,
            ""MaxValue"": 24,
            ""ReadingIntervalMs"": 1000,
            ""NoiseLevel"": 0.1
        }";
        
        await File.WriteAllTextAsync(TEST_CONFIG_PATH, configJson);
        await _sensor.InitializeSensor(TEST_CONFIG_PATH);
        File.Delete(TEST_CONFIG_PATH);
    }

    public void Dispose()
    {
        if (File.Exists(TEST_CONFIG_PATH))
        {
            File.Delete(TEST_CONFIG_PATH);
        }
    }
}