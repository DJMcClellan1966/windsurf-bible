namespace AI_Bible_App.Core.Models;

/// <summary>
/// Represents a generated prayer
/// </summary>
public class Prayer
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Content { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<string> Tags { get; set; } = new();
}
