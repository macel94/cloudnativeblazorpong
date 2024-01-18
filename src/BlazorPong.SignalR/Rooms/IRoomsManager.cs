using BlazorPong.Web.Shared;

namespace BlazorPong.SignalR.Rooms
{
    public interface IRoomsManager
    {
        Task<int> AddPlayer1Point(Guid roomId);
        Task<int> AddPlayer2Point(Guid roomId);
        Task<string> GetGameOverMessage(Guid roomId);
        Task<RoomState> InitializeGameObjectsOnServer(Guid roomId, bool forceInitialization);
        Task Player1Disconnected(Guid roomId);
        Task Player2Disconnected(Guid roomId);
        Task SetPlayerConnectionIdAsync(RoomState roomstate, Roles role, string connectionId);
        Task SetPlayerIsReadyAsync(Guid roomId, string connectionId);
        Task TryLockRoomAsync();
        Task UnlockRoomAsync(Guid key);
        Task<string> UpdateBallPosition(Guid roomId);
        Task UpdateGameObjectPositionOnServer(Guid roomId, GameObject clientUpdatedObject);
    }
}
