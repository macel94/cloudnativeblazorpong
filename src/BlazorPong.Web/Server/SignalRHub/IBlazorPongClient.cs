using BlazorPong.Web.Shared;

namespace BlazorPong.Web.Server.SignalRHub;

public interface IBlazorPongClient
{
    Task UpdateGameObjectPositionOnClient(GameObject gameObject);
    Task UpdatePlayerPoints(ClientType clientType, int points);
    Task UpdateGameMessage(string gameOverMessage);
}
