using BlazorPong.Web.Shared;

namespace BlazorPong.Web.Shared.Hubs;

public interface IBlazorPongClient
{
    Task UpdateGameObjectPositionOnClient(GameObject gameObject);
    Task UpdatePlayerPoints(Role clientType, int points);
    Task UpdateGameMessage(string gameOverMessage);
}
