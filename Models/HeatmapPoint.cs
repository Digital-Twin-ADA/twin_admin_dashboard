using System.Text.Json.Serialization;

namespace AdminDashboard.Models;

public class HeatmapPoint
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    
    [JsonPropertyName("weight")]
    public double Intensity { get; set; }
}
