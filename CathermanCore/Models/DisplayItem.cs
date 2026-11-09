namespace CathermanCore;

public class DisplayItem
{
    public int Id { get; set; }
    public int ParentId { get; set; }
    public string Title { get; set; } = "";
    public string Text { get; set; } = "";
    public string AuthorName { get; set; } = "";
    public string AuthorEmail { get; set; } = "";
    public DateTime PublishDate { get; set; } = DateTime.MinValue;
    public DateTime Updated { get; set; } = DateTime.MinValue;
    public bool IsLive { get; set; }
}
