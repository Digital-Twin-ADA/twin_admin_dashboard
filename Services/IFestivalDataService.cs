using AdminDashboard.Models;

namespace AdminDashboard.Services;

public interface IFestivalDataService
{
    Task<FestivalInfo?> GetFestivalInfoAsync();
    Task<List<HeatmapPoint>?> GetHeatmapPointsAsync();
}
