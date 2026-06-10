namespace AdminDashboard.Models;

public class AlertMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Type { get; set; } = "Info";
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
