using AdminDashboard.Models;
using Microsoft.Extensions.DependencyInjection;

namespace AdminDashboard.Services;

public class FestivalStompService : IFestivalStompService, IDisposable
{
    public event Action<List<HeatmapPoint>>? OnHeatmapUpdated;
    public event Action<List<StageCapacity>>? OnStagesUpdated;
    public event Action<AlertMessage>? OnAlertReceived;

    private System.Timers.Timer? _timer;
    private readonly Random _random = new Random();

    // Center of the festival map (e.g., somewhere in a generic park, or Untold festival coordinates)
    private double _baseLat = 46.7688; // Cluj-Napoca (Untold Festival example)
    private double _baseLng = 23.5700;

    private List<StageCapacity> _stages = new();

    private readonly IServiceScopeFactory _scopeFactory;

    public FestivalStompService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public void Initialize(FestivalInfo festivalInfo)
    {
        _baseLat = festivalInfo.Latitude;
        _baseLng = festivalInfo.Longitude;

        _stages = festivalInfo.Stages.Select(s => new StageCapacity
        {
            StageName = s.Name,
            MaxCapacity = s.Capacity > 0 ? s.Capacity : 10000, // Fallback if API returns 0
            Latitude = s.Latitude,
            Longitude = s.Longitude,
            RadiusMeters = 20 
        }).ToList();
    }

    public Task ConnectAsync()
    {
        // Placeholder for actual STOMP connection logic using Netina.Stomp.Client
        // Since we don't have the Spring Boot server yet, we'll simulate the data stream using a timer
        
        _timer = new System.Timers.Timer(120000); // Trigger every 2 minutes
        _timer.Elapsed += async (sender, args) => await GenerateDataAsync();
        _ = GenerateDataAsync();
        _timer.Start();

        return Task.CompletedTask;
    }

    public Task DisconnectAsync()
    {
        _timer?.Stop();
        _timer?.Dispose();
        return Task.CompletedTask;
    }

    private async Task GenerateDataAsync()
    {
        var heatmapPoints = new List<HeatmapPoint>();

        using (var scope = _scopeFactory.CreateScope())
        {
            var dataService = scope.ServiceProvider.GetRequiredService<IFestivalDataService>();
            var points = await dataService.GetHeatmapPointsAsync();
            if (points != null)
            {
                heatmapPoints = points;
                // If intensity is missing (0), default it to a reasonable value for the heatmap
                foreach (var pt in heatmapPoints)
                {
                    if (pt.Intensity == 0) pt.Intensity = 1.0;
                }
            }
        }

        OnHeatmapUpdated?.Invoke(heatmapPoints);

        // 2. Calculate Stage Capacities based on heatmap points
        foreach(var stage in _stages) stage.CurrentCapacity = 0;

        foreach(var point in heatmapPoints)
        {
            foreach(var stage in _stages)
            {
                // Euclidean distance approximation in meters
                double dLat = (point.Latitude - stage.Latitude) * 111320.0;
                double dLng = (point.Longitude - stage.Longitude) * 76300.0;
                double distanceMeters = Math.Sqrt(dLat * dLat + dLng * dLng);

                if (distanceMeters <= stage.RadiusMeters)
                {
                    // Each point represents exactly one person
                    stage.CurrentCapacity += 1;
                }
            }
        }
        
        // Ensure it doesn't wildly exceed max capacity for display purposes
        foreach(var stage in _stages)
        {
            if (stage.CurrentCapacity > stage.MaxCapacity * 1.05) 
                stage.CurrentCapacity = (int)(stage.MaxCapacity * 1.05);
        }

        OnStagesUpdated?.Invoke(_stages.ToList());

        // 3. Random Alerts (occassionally)
        if (_random.NextDouble() > 0.9)
        {
            var alertTypes = new[] { "Warning", "Critical", "Info" };
            var alertMessages = new[] { "High density near Main Stage", "Medical team dispatched to Forest Stage", "Gate 3 is clear" };
            
            var alert = new AlertMessage
            {
                Type = alertTypes[_random.Next(alertTypes.Length)],
                Message = alertMessages[_random.Next(alertMessages.Length)]
            };
            OnAlertReceived?.Invoke(alert);
        }
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
