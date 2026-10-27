namespace AdPlay.Api.Models;

public class User
{
    public int Id { get; set; }
    public string Mobile { get; set; } = "";
    public bool IsSubscribed { get; set; }
    public DateTime? SubscribedAt { get; set; }
}
