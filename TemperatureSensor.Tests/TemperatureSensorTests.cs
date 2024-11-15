using Microsoft.Extensions.Logging;
using Moq;
using TemperatureSensor.App.Services;
using Xunit;
using System;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace TemperatureSensor.Tests
{
    public class TemperatureSensorTests
    {
        private readonly Mock<ILogger<TempSensor>> _loggerMock;
        private readonly TempSensor _sensor;

        public TemperatureSensorTests()
        {
            _loggerMock = new Mock<ILogger<TempSensor>>();
            _sensor = new TempSensor(_loggerMock.Object);
        }

        [Fact]
        public async Task SimulateData_GeneratesReadingsWithinRange()
        {
            // Arrange
            string configPath = "TestData/validConfig.json";
            Directory.CreateDirectory("TestData");
            await File.WriteAllTextAsync(configPath, @"{
                ""Name"": ""Test-Sensor"",
                ""Location"": ""Test-Location"",
                ""MinValue"": 22.0,
                ""MaxValue"": 24.0,
                ""ReadingIntervalMs"": 100,
                ""NoiseLevel"": 0.1
            }");

            await _sensor.InitializeSensor(configPath);

            // Act
            await _sensor.StartSensor();
            
            // Wait for a few readings
            await Task.Delay(500);

            // Get multiple readings
            var readings = new List<double>();
            for (int i = 0; i < 5; i++)
            {
                readings.Add(_sensor.GetCurrentReading());
                await Task.Delay(100);
            }

            await _sensor.StopSensor();

            // Assert
            Assert.All(readings, reading => 
                Assert.True(reading >= 22.0 && reading <= 24.0, 
                    $"Reading {reading} is outside the valid range"));

            // Verify readings are not constant (simulation is working)
            Assert.True(readings.Distinct().Count() > 1, 
                "Readings should vary due to simulated noise");

            // Cleanup
            File.Delete(configPath);
            Directory.Delete("TestData");
        }

        [Fact]
        public async Task GetCurrentReading_WhenSensorNotRunning_ThrowsException()
        {
            // Arrange
            string configPath = "TestData/validConfig.json";
            Directory.CreateDirectory("TestData");
            await File.WriteAllTextAsync(configPath, @"{
                ""Name"": ""Test-Sensor"",
                ""Location"": ""Test-Location"",
                ""MinValue"": 22.0,
                ""MaxValue"": 24.0,
                ""ReadingIntervalMs"": 1000,
                ""NoiseLevel"": 0.1
            }");

            await _sensor.InitializeSensor(configPath);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => _sensor.GetCurrentReading()
            );
            Assert.Contains("not running", exception.Message);

            // Cleanup
            File.Delete(configPath);
            Directory.Delete("TestData");
        }
    }
}