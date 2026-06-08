using AdminDashboard.Models;

namespace AdminDashboard.Services;

public interface IFestivalStompService
{
    event Action<List<HeatmapPoint>>? OnHeatmapUpdated;
    event Action<List<StageCapacity>>? OnStagesUpdated;
    event Action<AlertMessage>? OnAlertReceived;
    event Action? OnConnectionStateChanged;

    bool IsConnected { get; }

    void Initialize(FestivalInfo festivalInfo);
    Task ConnectAsync();
    Task DisconnectAsync();
    Task SendAlertAsync(CustomAlertRequest alert);
}
