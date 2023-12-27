namespace BlazorPong.Web.Server.Room;

public class Room
{
    public Guid Id { get; set; }
    public string ServerName { get; set; }
    public List<Client> Clients { get; set; }
}
