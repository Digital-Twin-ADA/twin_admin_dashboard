namespace AdminDashboard.Models;

public class StageCapacity
{
    public string StageName { get; set; } = string.Empty;
    public int CurrentCapacity { get; set; }
    public int MaxCapacity { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double RadiusMeters { get; set; }
    public double FillPercentage => MaxCapacity > 0 ? (double)CurrentCapacity / MaxCapacity : 0;
}
