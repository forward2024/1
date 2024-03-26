public class Note
{
    public string Title { get; set; }
    public string Text { get; set; }
    public DateTime DataCreating { get; set; } = DateTime.Now;
}