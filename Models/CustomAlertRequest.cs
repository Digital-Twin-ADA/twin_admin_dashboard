namespace AdminDashboard.Models;

public class CustomAlertRequest
{
    public int StageId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
}
