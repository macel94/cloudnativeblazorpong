using BlazorPong.Web.Shared;
using Microsoft.AspNetCore.SignalR;

namespace BlazorPong.Web.Server.Rooms.Game.SignalRHub;

public class GameHub(RoomGameManager roomGamesManager) : Hub<IBlazorPongClient>
{
    public void UpdateGameObjectPosition(GameObject clientGameObject)
    {
        clientGameObject = clientGameObject with { LastUpdatedBy = Context.ConnectionId };
        roomGamesManager.UpdateGameObjectPositionOnServer(clientGameObject);
    }

    public ClientType GetClientType()
    {
        if (roomGamesManager.GetPlayer1ConnectionId() == Context.ConnectionId)
            return ClientType.Player1;

        if (roomGamesManager.GetPlayer2ConnectionId() == Context.ConnectionId)
            return ClientType.Player2;

        return ClientType.Spectator;
    }

    public void SetPlayerIsReady()
    {
        if (roomGamesManager.GetPlayer1ConnectionId() == Context.ConnectionId)
        {
            roomGamesManager.SetPlayer1IsReady(true);
        }
        else if (roomGamesManager.GetPlayer2ConnectionId() == Context.ConnectionId)
        {
            roomGamesManager.SetPlayer2IsReady(true);
        }
    }

    public Dictionary<string, GameObject> GetGameObjects()
    {
        if (roomGamesManager.GameObjectsDict == null || roomGamesManager.GameObjectsDict.Count != 3)
        {
            // Aggiungo solo i mancanti se sono qui
            roomGamesManager.InitializeGameObjectsOnServer(false);
        }

        return roomGamesManager.GameObjectsDict!;
    }

    public override async Task OnConnectedAsync()
    {
        // Teniamo così traccia di chi è quale player
        if (string.IsNullOrEmpty(roomGamesManager.GetPlayer1ConnectionId()))
        {
            roomGamesManager.SetPlayer1ConnectionId(Context.ConnectionId);
        }
        else if (string.IsNullOrEmpty(roomGamesManager.GetPlayer2ConnectionId()))
        {
            roomGamesManager.SetPlayer2ConnectionId(Context.ConnectionId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        if (roomGamesManager.GetPlayer1ConnectionId() == Context.ConnectionId)
        {
            if (roomGamesManager.MustPlayGame())
            {
                roomGamesManager.Player1Disconnected();
            }
            roomGamesManager.SetPlayer1ConnectionId(null);
        }
        else if (roomGamesManager.GetPlayer2ConnectionId() == Context.ConnectionId)
        {
            if (roomGamesManager.MustPlayGame())
            {
                roomGamesManager.Player2Disconnected();
            }
            roomGamesManager.SetPlayer2ConnectionId(null);
        }

        await base.OnDisconnectedAsync(exception);
    }
}
