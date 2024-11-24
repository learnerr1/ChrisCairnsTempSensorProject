using Microsoft.Extensions.Logging;
using TemperatureSensor.App.Services;
using TemperatureSensor.App.Interfaces;
using TemperatureSensor.App.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;


var services = new ServiceCollection()
    .AddLogging(builder =>
    {
        builder.AddConsole();
        builder.SetMinimumLevel(LogLevel.Information);
    })
    .AddSingleton<IDataHistory, DataHistory>()
    .AddSingleton<IDataLogger, FileLogger>()
    .AddSingleton<ITemperatureSensor, TempSensor>()
    .BuildServiceProvider();



var sensor = services.GetRequiredService<ITemperatureSensor>();

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

    Console.WriteLine($"Sensor {sensor.Name} initialized successfully at location {sensor.Location}");
    
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