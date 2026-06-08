using AdminDashboard.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Netina.Stomp.Client;
using Netina.Stomp.Client.Interfaces;

namespace AdminDashboard.Services;

public class FestivalStompService : IFestivalStompService, IDisposable
{
    public event Action<List<HeatmapPoint>>? OnHeatmapUpdated;
    public event Action<List<StageCapacity>>? OnStagesUpdated;
    public event Action<AlertMessage>? OnAlertReceived;
    public event Action? OnConnectionStateChanged;

    public bool IsConnected { get; private set; } = false;

    private System.Timers.Timer? _timer;
    private readonly Random _random = new Random();

    // Center of the festival map (e.g., somewhere in a generic park, or Untold festival coordinates)
    private double _baseLat = 46.7688; // Cluj-Napoca (Untold Festival example)
    private double _baseLng = 23.5700;

    private List<StageCapacity> _stages = new();

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private IStompClient? _stompClient;

    public FestivalStompService(IServiceScopeFactory scopeFactory, IConfiguration config)
    {
        _scopeFactory = scopeFactory;
        _config = config;
    }

    public void Initialize(FestivalInfo festivalInfo)
    {
        _baseLat = festivalInfo.Latitude;
        _baseLng = festivalInfo.Longitude;

        _stages = festivalInfo.Stages.Select(s => new StageCapacity
        {
            StageId = s.Id,
            StageName = s.Name,
            MaxCapacity = s.Capacity > 0 ? s.Capacity : 10000, // Fallback if API returns 0
            Latitude = s.Latitude,
            Longitude = s.Longitude,
            RadiusMeters = 20 
        }).ToList();
    }

    public Task ConnectAsync()
    {
        var baseUrl = _config.GetValue<string>("FestivalApiBaseUrl");
        if (!string.IsNullOrEmpty(baseUrl))
        {
            // Spring Boot with SockJS exposes the raw WebSocket endpoint at /ws/websocket
            var wsUrl = baseUrl.Replace("http://", "ws://").Replace("https://", "wss://") + "/ws/websocket";
            _stompClient = new StompClient(wsUrl);
            
            _stompClient.OnConnect += async (s, e) => 
            {
                IsConnected = true;
                OnConnectionStateChanged?.Invoke();
                
                try 
                {
                    await _stompClient.SubscribeAsync("/topic/heatmap", new Dictionary<string, string>(), HandleHeatmapMessage);
                    await _stompClient.SubscribeAsync("/topic/alerts", new Dictionary<string, string>(), HandleAlertMessage);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Subscription failed: {ex.Message}");
                }
            };
            
            _stompClient.OnClose += (s, e) => 
            {
                IsConnected = false;
                OnConnectionStateChanged?.Invoke();
            };
            
            // Connect in the background so it doesn't block the UI or the HTTP timer
            _ = Task.Run(async () =>
            {
                try
                {
                    await _stompClient.ConnectAsync(new Dictionary<string, string>());
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"STOMP Connection failed: {ex.Message}");
                }
            });
        }

        // Initial load only, no polling timer anymore
        _ = GenerateDataAsync();

        return Task.CompletedTask;
    }

    public Task DisconnectAsync()
    {
        _stompClient?.DisconnectAsync();
        return Task.CompletedTask;
    }

    public async Task SendAlertAsync(CustomAlertRequest alert)
    {
        if (_stompClient != null && IsConnected)
        {
            var options = new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase };
            var jsonPayload = System.Text.Json.JsonSerializer.Serialize(alert, options);
            
            // Add content-type header for JSON
            var headers = new Dictionary<string, string>
            {
                { "content-type", "application/json" }
            };
            
            await _stompClient.SendAsync(jsonPayload, "/app/alerts", headers);
        }
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
    }

    private void HandleHeatmapMessage(object? sender, Netina.Stomp.Client.Messages.StompMessage e)
    {
        if (string.IsNullOrEmpty(e.Body)) return;
        
        try
        {
            var options = new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase };
            var points = System.Text.Json.JsonSerializer.Deserialize<List<HeatmapPoint>>(e.Body, options);
            
            if (points != null)
            {
                var heatmapPoints = points;
                // If intensity is missing (0), default it to a reasonable value for the heatmap
                foreach (var pt in heatmapPoints)
                {
                    if (pt.Intensity == 0) pt.Intensity = 1.0;
                }

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

                OnHeatmapUpdated?.Invoke(heatmapPoints);
                OnStagesUpdated?.Invoke(_stages.ToList());
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing heatmap websocket message: {ex.Message}");
        }
    }

    private void HandleAlertMessage(object? sender, Netina.Stomp.Client.Messages.StompMessage e)
    {
        if (string.IsNullOrEmpty(e.Body)) return;
        
        try
        {
            var options = new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase };
            var alertData = System.Text.Json.JsonSerializer.Deserialize<CustomAlertRequest>(e.Body, options);
            
            if (alertData != null)
            {
                // Map the backend's HIGH/MEDIUM/LOW severity to our UI's Critical/Warning/Info
                string uiType = "Info";
                if (!string.IsNullOrEmpty(alertData.Severity))
                {
                    var sev = alertData.Severity.ToUpper();
                    if (sev == "HIGH" || sev == "CRITICAL") uiType = "Critical";
                    else if (sev == "MEDIUM" || sev == "WARNING") uiType = "Warning";
                }
                else if (!string.IsNullOrEmpty(alertData.Type))
                {
                    uiType = alertData.Type; // Fallback
                }

                var msg = new AlertMessage 
                {
                    Type = uiType,
                    Message = alertData.Message,
                    Timestamp = DateTime.UtcNow
                };
                OnAlertReceived?.Invoke(msg);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing alert websocket message: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _stompClient?.DisconnectAsync();
    }
}
