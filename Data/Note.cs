public class Note
{
    public Guid Id { get; set; } = Guid.Empty;
    public string? Title { get; set; }
    public string? Content { get; set; }
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
}