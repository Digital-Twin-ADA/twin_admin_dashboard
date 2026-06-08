using AdminDashboard.Models;

namespace AdminDashboard.Services;

public interface IFestivalStompService
{
    event Action<List<HeatmapPoint>>? OnHeatmapUpdated;
    event Action<List<StageCapacity>>? OnStagesUpdated;
    event Action<AlertMessage>? OnAlertReceived;

    void Initialize(FestivalInfo festivalInfo);
    Task ConnectAsync();
    Task DisconnectAsync();
}
