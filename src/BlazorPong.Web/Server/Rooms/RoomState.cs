using BlazorPong.Web.Shared;

namespace BlazorPong.Web.Server.Rooms;

public class RoomState
{
    public Guid RoomId { get; set; }
    public Dictionary<string, GameObject?> GameObjectsDictionary { get; set; } = [];
    public bool GameMustReset { get; set; }
    public string? Player1ConnectionId { get; set; }
    public string? Player2ConnectionId { get; set; }
    public bool Player1Ready { get; set; }
    public bool Player2Ready { get; set; }
    public int Player1Points { get; set; }
    public int Player2Points { get; set; }
    public string? ServerName { get; set; }

    public bool MustPlayGame
    {
        get => GameObjectsDictionary.Count == 3
            && !string.IsNullOrEmpty(Player1ConnectionId)
            && !string.IsNullOrEmpty(Player2ConnectionId)
            && Player1Ready
            && Player2Ready;
    }
}
