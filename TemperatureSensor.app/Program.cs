// TemperatureSensor.App/Program.cs
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using TemperatureSensor.App.Services;

var loggerFactory = LoggerFactory.Create(builder => 
{
    builder.AddConsole();
});

var logger = loggerFactory.CreateLogger<TempSensor>();
var sensor = new TempSensor(logger);

try
{
    bool initialized = await sensor.InitializeSensor("config.json");
    if (!initialized)
    {
        Console.WriteLine("Failed to initialize sensor");
        return;
    }

    Console.WriteLine($"Sensor {sensor.Name} initialized successfully at location {sensor.Location}");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}