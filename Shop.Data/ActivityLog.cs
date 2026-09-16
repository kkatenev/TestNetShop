namespace Shop.Data;

public class ActivityLog
{
    public int Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public int? TaskId { get; set; }
    public string Message { get; set; } = string.Empty;
}
