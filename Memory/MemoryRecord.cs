namespace PrivateAIOverlay.Memory;

public sealed class MemoryRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Content { get; set; } = string.Empty;
    public string Category { get; set; } = "General";
    public string Scope { get; set; } = "Global";
    public int Importance { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string Source { get; set; } = "user";
    public bool Active { get; set; } = true;
    public string Metadata { get; set; } = "{}";
}
