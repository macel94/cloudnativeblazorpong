using BlazorPong.Web.Shared;

namespace BlazorPong.Web.Shared.Hubs;

public interface IBlazorPongClient
{
    Task UpdateGameObjectPositionOnClient(GameObject gameObject);
    Task UpdatePlayerPoints(Roles clientType, int points);
    Task UpdateGameMessage(string gameOverMessage);
}
