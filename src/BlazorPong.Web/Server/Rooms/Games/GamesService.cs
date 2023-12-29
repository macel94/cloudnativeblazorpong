using BlazorPong.Web.Server.Rooms.Games.Hubs;
using BlazorPong.Web.Shared;
using BlazorPong.Web.Shared.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace BlazorPong.Web.Server.Rooms.Games;

public class GamesService(IHubContext<GameHub, IBlazorPongClient> hub, RoomsManager roomGameManager, RedisRoomStateCache roomsDictionary, ILogger<GamesService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && hub != null)
        {
            try
            {
                var roomKeys = roomsDictionary.GetRoomKeys();
                if (roomKeys.Count == 0)
                {
                    logger.LogInformation("No rooms found, waiting");
                    await Task.Delay(GameConstants.IdleDelayInMs, cancellationToken);
                    continue;
                }

                logger.LogInformation("GamesService is running");
                var tasks = roomKeys.Select(roomKey => ManageGameAsync(hub, roomKey, roomGameManager, cancellationToken)).ToList();
                tasks.Add(Task.Delay(GameConstants.GameDelayBetweenTicksInMs, cancellationToken));
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, $"Critical Unmanaged error in {nameof(GamesService)}");
            }
        }
    }

    private async Task ManageGameAsync(IHubContext<GameHub, IBlazorPongClient> hub, Guid roomId, RoomsManager roomGameManager, CancellationToken cancellationToken)
    {
        var roomState = await roomsDictionary.UnsafeGetRoomStateAsync(roomId);

        if(!roomState.MustPlayGame && !roomState.GameMustReset)
        {
            logger.LogInformation($"Room {roomId} Game must not play or reset, waiting");
            await Task.Delay(GameConstants.IdleDelayInMs, cancellationToken);
        }

        if (roomState.MustPlayGame)
        {
            logger.LogInformation($"Room {roomId} Game must play");

            // Faccio sempre muovere la palla
            var pointPlayerName = await roomGameManager.UpdateBallPosition(roomId);

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
            logger.LogInformation($"Room {roomId} Game must reset");
            var gameOverMessage = await roomGameManager.GetGameOverMessage(roomId);
            await hub.Clients.Group(roomState.RoomId.ToString()).UpdateGameMessage(gameOverMessage);
        }
        else
        {
            logger.LogInformation($"Room {roomId} Game must not reset");
        }
    }

    private static async Task ManagePlayerPoint(IHubContext<GameHub, IBlazorPongClient> hub, RoomsManager roomGameManager, RoomState roomState, string pointPlayerName, CancellationToken cancellationToken)
    {
        int playerPoints;
        Role playerType;
        // Altrimenti aggiungo il punto e resetto il tutto
        if (pointPlayerName.Equals("player1"))
        {
            playerPoints = await roomGameManager.AddPlayer1Point(roomState.RoomId);
            playerType = Role.Player1;
        }
        else
        {
            playerPoints = await roomGameManager.AddPlayer2Point(roomState.RoomId);
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
            kvPair.Value!.LastTickConnectedServerReceivedUpdate = DateTimeOffset.UtcNow.Ticks;
            kvPair.Value!.LastSinglaRServerReceivedUpdateName = Environment.MachineName;

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
