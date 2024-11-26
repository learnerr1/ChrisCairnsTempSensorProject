using Microsoft.Extensions.Logging;
using TemperatureSensor.App.Services;
using TemperatureSensor.App.Interfaces;
using TemperatureSensor.App.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using System.Text.Json;

var services = new ServiceCollection()
    .AddLogging(builder =>
    {
        builder.AddConsole();
        builder.SetMinimumLevel(LogLevel.Information);
    })
    .AddSingleton<IDataHistory, DataHistory>()
    .AddSingleton<IDataLogger, FileLogger>()
    .AddSingleton<IAlertService, AlertService>()
    .AddSingleton<ITemperatureSensor, TempSensor>()
    .BuildServiceProvider();

var sensor = services.GetRequiredService<ITemperatureSensor>();
var alertService = services.GetRequiredService<IAlertService>();

try
{
    Console.WriteLine("Temperature Sensor Simulation");
    Console.WriteLine("----------------------------");
    
    bool initialized = await sensor.InitializeSensor("config.json");
    if (!initialized)
    {
        Console.WriteLine("Failed to initialize sensor");
        return;
    }

    // Read current config to show thresholds
    var configJson = await File.ReadAllTextAsync("config.json");
    var config = JsonSerializer.Deserialize<SensorConfig>(configJson);

    if (config != null)
    {
        Console.WriteLine("\nCurrent Thresholds:");
        Console.WriteLine($"Warning: {config.WarningThreshold}°C");
        Console.WriteLine($"Critical: {config.CriticalThreshold}°C");

        Console.Write("\nWould you like to update thresholds? (y/n): ");
        if (Console.ReadLine()?.ToLower() == "y")
        {
            Console.Write("Enter warning threshold (°C): ");
            if (double.TryParse(Console.ReadLine(), out double warningThreshold))
            {
                Console.Write("Enter critical threshold (°C): ");
                if (double.TryParse(Console.ReadLine(), out double criticalThreshold))
                {
                    try
                    {
                        alertService.UpdateThresholds(warningThreshold, criticalThreshold);
                        Console.WriteLine("Thresholds updated successfully!");
                    }
                    catch (ArgumentException ex)
                    {
                        Console.WriteLine($"Error: {ex.Message}");
                    }
                }
            }
        }
    }

    Console.WriteLine($"\nSensor {sensor.Name} initialized successfully at location {sensor.Location}");
    
    await sensor.StartSensor();
    Console.WriteLine("Sensor started. Press any key to stop...");

    while (!Console.KeyAvailable)
    {
        var reading = sensor.GetCurrentReading();
        var smoothedReading = sensor.GetSmoothedReading();
        
        Console.WriteLine($"{DateTime.Now:HH:mm:ss} - Temperature: {reading:F2}°C (Moving Smoothed Average: {smoothedReading:F2}°C)");
        await Task.Delay(1000);
    }

    await sensor.StopSensor();
    Console.WriteLine("Sensor stopped.");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}