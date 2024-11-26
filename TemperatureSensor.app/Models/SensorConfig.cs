namespace TemperatureSensor.App.Models;

public class SensorConfig
{
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public double MinValue { get; set; }
    public double MaxValue { get; set; }
    public int ReadingIntervalMs { get; set; } = 6000; 
    public double NoiseLevel { get; set; } = 0.1; 

//for fault-injection
public double FaultProbability { get; set; } = 0.05; 
public double FaultDuration { get; set; } = 6000; 
public double FaultTemperature { get; set; } = 28.0;


}