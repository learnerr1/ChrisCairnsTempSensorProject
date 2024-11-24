using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using TemperatureSensor.App.Models;
using TemperatureSensor.App.Services;
using Xunit;

namespace TemperatureSensor.Tests;

public class FileLoggerTests : IDisposable
{
    private readonly Mock<ILogger<FileLogger>> _mockLogger;
    private readonly string _testLogDirectory;
    private readonly FileLogger _fileLogger;

    public FileLoggerTests()
    {
        _mockLogger = new Mock<ILogger<FileLogger>>();
        _fileLogger = new FileLogger(_mockLogger.Object);
        _testLogDirectory = Path.Combine(Path.GetTempPath(), "TempSensorTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testLogDirectory);
    }

    [Fact]
    public async Task Initialize_CreatesNewLogFile_WhenFileDoesNotExist()
    {
       
        string logPath = Path.Combine(_testLogDirectory, "test.log");

     
        bool result = await _fileLogger.Initialize(logPath);

    
        Assert.True(result);
        Assert.True(File.Exists(logPath));
        string content = await File.ReadAllTextAsync(logPath);
        Assert.Contains("Temperature Sensor Log - Started", content);
    }

    [Fact]
    public async Task Initialize_AppendsToExistingFile_WhenFileExists()
    {
    
        string logPath = Path.Combine(_testLogDirectory, "existing.log");
        await File.WriteAllTextAsync(logPath, "Existing content\n");


        bool result = await _fileLogger.Initialize(logPath);

   
        Assert.True(result);
        string content = await File.ReadAllTextAsync(logPath);
        Assert.Contains("Existing content", content);
        Assert.Contains("New Session Started", content);
    }

    [Fact]
    public async Task Initialize_CreatesDirectory_WhenDirectoryDoesNotExist()
    {
    
        string newDir = Path.Combine(_testLogDirectory, "newdir");
        string logPath = Path.Combine(newDir, "test.log");

     
        bool result = await _fileLogger.Initialize(logPath);


        Assert.True(result);
        Assert.True(Directory.Exists(newDir));
        Assert.True(File.Exists(logPath));
    }

    [Fact]
    public async Task LogData_WritesCorrectFormat_WhenInitialized()
    {
       
        string logPath = Path.Combine(_testLogDirectory, "data.log");
        await _fileLogger.Initialize(logPath);
        var sensorData = new SensorData
        {
            Temperature = 23.5,
            Timestamp = new DateTime(2024, 1, 1, 12, 0, 0),
            IsValid = true
        };

       
        bool result = await _fileLogger.LogData(sensorData);

        
        Assert.True(result);
        string content = await File.ReadAllTextAsync(logPath);
        Assert.Contains("Temperature: 23.50°C", content);
        Assert.Contains("Valid: True", content);
        Assert.Contains("2024-01-01 12:00:00", content);
    }

    [Fact]
    public async Task LogData_ReturnsFalse_WhenNotInitialized()
    {
    
        var sensorData = new SensorData
        {
            Temperature = 23.5,
            Timestamp = DateTime.Now,
            IsValid = true
        };

    
        bool result = await _fileLogger.LogData(sensorData);

      
        Assert.False(result);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Logger not initialized")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once
        );
    }

    [Theory]
    [InlineData(22.5, true)]
    [InlineData(25.0, false)]
    [InlineData(18.3, true)]
    public async Task LogData_HandlesVariousTemperatures_Successfully(double temperature, bool isValid)
    {
      
        string logPath = Path.Combine(_testLogDirectory, "temps.log");
        await _fileLogger.Initialize(logPath);
        var sensorData = new SensorData
        {
            Temperature = temperature,
            Timestamp = DateTime.Now,
            IsValid = isValid
        };

      
        bool result = await _fileLogger.LogData(sensorData);

      
        Assert.True(result);
        string content = await File.ReadAllTextAsync(logPath);
        Assert.Contains($"Temperature: {temperature:F2}°C", content);
        Assert.Contains($"Valid: {isValid}", content);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testLogDirectory))
            {
                Directory.Delete(_testLogDirectory, true);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error cleaning up test directory: {ex.Message}");
        }
    }
}