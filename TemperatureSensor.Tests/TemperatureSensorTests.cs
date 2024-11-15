C
Build FAILED.
C:\Users\Chris\TemperatureSensorProject\TemperatureSensor.Tests\TemperatureSensorTests.cs(2,1): error CS0106: The modifier 'publi 
c' is not valid for this item [C:\Users\Chris\TemperatureSensorProject\TemperatureSensor.Tests\TemperatureSensor.Tests.csproj]    
C:\Users\Chris\TemperatureSensorProject\TemperatureSensor.Tests\TemperatureSensorTests.cs(49,1): error CS0106: The modifier 'publ 
ic' is not valid for this item [C:\Users\Chris\TemperatureSensorProject\TemperatureSensor.Tests\TemperatureSensor.Tests.csproj]   
    0 Warning(s)
    2 Error(s)
Time Elapsed 00:00:02.27
PS C:\Users\Chris\TemperatureSensorProject>


The error occurs because we need to place the new test methods inside the existing test class. Let's fix the test file:

csharp

Copy
// TemperatureSensor.Tests/TemperatureSensorTests.cs
using Microsoft.Extensions.Logging;
using Moq;
using TemperatureSensor.App.Services;
using Xunit;

namespace TemperatureSensor.Tests;

public class TemperatureSensorTests
{
    private readonly Mock<ILogger<TempSensor>> _loggerMock;
    private readonly TempSensor _sensor;

    public TemperatureSensorTests()
    {
        _loggerMock = new Mock<ILogger<TempSensor>>();
        _sensor = new TempSensor(_loggerMock.Object);
    }

    // Existing initialization tests...

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