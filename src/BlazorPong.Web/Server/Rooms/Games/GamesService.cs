using BlazorPong.Web.Server.Rooms.Games.Hubs;
using BlazorPong.Web.Shared;
using BlazorPong.Web.Shared.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace BlazorPong.Web.Server.Rooms.Games;

public class GamesService(IHubContext<GameHub, IBlazorPongClient> hub, RoomsManager roomGameManager, ILogger<GamesService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && hub != null)
        {
            try
            {
                var roomKeys = roomGameManager.RoomsDictionary.Keys;
                var tasks = roomKeys.Select(x => ManageGameAsync(hub, x, roomGameManager, cancellationToken));
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, $"Critical Unmanaged error in {nameof(GamesService)}");
            }
        }
    }

    private static async Task ManageGameAsync(IHubContext<GameHub, IBlazorPongClient> hub, Guid roomId, RoomsManager roomGameManager, CancellationToken cancellationToken)
    {
        var roomState = roomGameManager.RoomsDictionary[roomId];

        if (roomState.MustPlayGame)
        {
            // Faccio sempre muovere la palla
            var pointPlayerName = roomGameManager.UpdateBallPosition(roomId);

            // Se nessuno ha fatto punto
            if (string.IsNullOrEmpty(pointPlayerName))
            {
                await ManageNoPlayerPoint(hub, roomState);
            }
            else
            {
                await ManagePlayerPoint(hub, roomGameManager, roomState, pointPlayerName, cancellationToken);
            }
        }

        if (roomState.GameMustReset)
        {
            var gameOverMessage = roomGameManager.GetGameOverMessage(roomId);
            await hub.Clients.Group(roomState.RoomId.ToString()).UpdateGameMessage(gameOverMessage);
        }
        else
        {
            await Task.Delay(GameConstants.GameDelayBetweenTicksInMs, cancellationToken);
        }
    }

    private static async Task ManagePlayerPoint(IHubContext<GameHub, IBlazorPongClient> hub, RoomsManager roomGameManager, RoomState roomState, string pointPlayerName, CancellationToken cancellationToken)
    {
        int playerPoints;
        Role playerType;
        // Altrimenti aggiungo il punto e resetto il tutto
        if (pointPlayerName.Equals("player1"))
        {
            playerPoints = roomGameManager.AddPlayer1Point(roomState.RoomId);
            playerType = Role.Player1;
        }
        else
        {
            playerPoints = roomGameManager.AddPlayer2Point(roomState.RoomId);
            playerType = Role.Player2;
        }

        await hub.Clients.Group(roomState.RoomId.ToString()).UpdatePlayerPoints(playerType, playerPoints);
        // Da il tempo di visualizzare il messaggio del punto se il gioco non deve essere resettato
        if (!roomState.GameMustReset)
        {
            await Task.Delay(3000, cancellationToken);
        }
    }

    private static async Task ManageNoPlayerPoint(IHubContext<GameHub, IBlazorPongClient> hub, RoomState roomState)
    {
        foreach (var kvPair in roomState.GameObjectsDictionary
                            .Where(kvPair => kvPair.Value != null
                                && kvPair.Value.WasUpdated))
        {
            kvPair.Value!.LastTickServerReceivedUpdate = DateTimeOffset.UtcNow.Ticks;

            // Se so chi ha fatto l'update evito di mandarglielo
            if (kvPair.Value.LastUpdatedBy != null && !kvPair.Value.LastUpdatedBy.Equals("server"))
            {
                await hub.Clients.GroupExcept(roomState.RoomId.ToString(), kvPair.Value.LastUpdatedBy).UpdateGameObjectPositionOnClient(kvPair.Value);
            }
            else
            {
                await hub.Clients.Group(roomState.RoomId.ToString()).UpdateGameObjectPositionOnClient(kvPair.Value);
            }
        }
    }
}
