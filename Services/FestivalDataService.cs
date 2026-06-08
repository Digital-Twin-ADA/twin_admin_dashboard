using System.Net.Http.Json;
using AdminDashboard.Models;

namespace AdminDashboard.Services;

public class FestivalDataService : IFestivalDataService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FestivalDataService> _logger;

    public FestivalDataService(HttpClient httpClient, ILogger<FestivalDataService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<FestivalInfo?> GetFestivalInfoAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/festival/info");
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<FestivalInfo>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch festival info from the server.");
            return null;
        }
    }
    public async Task<List<HeatmapPoint>?> GetHeatmapPointsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/participant-locations/heatmap");
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<List<HeatmapPoint>>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch heatmap points from the server.");
            return null;
        }
    }
}
