using System.Text.Json.Serialization;

namespace AdminDashboard.Models;

public class StageInfo
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("capacity")]
    public int Capacity { get; set; }

    [JsonPropertyName("currentCrowd")]
    public int CurrentCrowd { get; set; }

    [JsonPropertyName("overcrowded")]
    public bool Overcrowded { get; set; }

    [JsonPropertyName("zoneCode")]
    public string ZoneCode { get; set; } = string.Empty;

    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }
}
