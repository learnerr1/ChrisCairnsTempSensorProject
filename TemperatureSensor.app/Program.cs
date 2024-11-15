// TemperatureSensor.App/Program.cs
using Microsoft.Extensions.Logging;
using TemperatureSensor.App.Services;

var loggerFactory = LoggerFactory.Create(builder => 
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Debug);
});

var logger = loggerFactory.CreateLogger<TempSensor>();
var sensor = new TempSensor(logger);

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

    // Display readings for a while
    while (!Console.KeyAvailable)
    {
        var reading = sensor.GetCurrentReading();
        Console.WriteLine($"{DateTime.Now:HH:mm:ss} - Temperature: {reading:F2}°C");
        await Task.Delay(1000);
    }

    await sensor.StopSensor();
    Console.WriteLine("Sensor stopped.");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}