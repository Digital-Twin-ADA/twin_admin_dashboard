namespace AdminDashboard.Models;

public class HeatmapPoint
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Intensity { get; set; } // e.g., 0.0 to 1.0
}
