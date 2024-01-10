namespace BlazorPong.Web.Shared.Hubs;

public interface IGameHub
{
    Task<Dictionary<string, GameObject>> GetGameObjects(Guid roomId);
    Task<Roles> JoinRoom(Guid roomId, string userName);
    Task LeaveRoom(Guid roomId);
    Task OnConnectedAsync();
    Task OnDisconnectedAsync(Exception? exception);
    Task<Roles> OpenRoom(Guid roomId, string userName);
    Task SetPlayerIsReady(Guid roomId);
    Task UpdateGameObjectPosition(Guid roomId, GameObject clientGameObject);
}
