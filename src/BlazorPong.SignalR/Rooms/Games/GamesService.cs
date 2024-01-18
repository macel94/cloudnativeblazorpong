using BlazorPong.SignalR.Hubs;
using BlazorPong.SignalR.Rooms;
using BlazorPong.Web.Shared;
using BlazorPong.Web.Shared.Clock;
using BlazorPong.Web.Shared.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace BlazorPong.SignalR.Rooms.Games;

public class GamesService(IHubContext<GameHub, IBlazorPongClient> hub,
                          IRoomsManager roomGameManager,
                          RedisRoomStateCache roomsDictionary,
                          ILogger<GamesService> logger,
                          ISystemClock systemClock)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var activeRoomTasks = new Dictionary<Guid, Task>();

        while (!cancellationToken.IsCancellationRequested && hub != null)
        {
            try
            {
                var currentRoomKeys = roomsDictionary.GetRoomKeys();
                if (currentRoomKeys.Count == 0)
                {
                    logger.LogInformation("No rooms found, waiting");
                    await Task.Delay(GameConstants.IdleDelayInMs, cancellationToken);
                    continue;
                }

                logger.LogInformation("GamesService is running");
                foreach (var roomKey in currentRoomKeys)
                {
                    var state = await roomsDictionary.UnsafeGetRoomStateAsync(roomKey);

                    // Start a task for new rooms assigned to this server
                    if (!activeRoomTasks.ContainsKey(roomKey)
                        && state?.ServerName != null
                        && state.ServerName.Equals(Environment.MachineName))
                    {
                        var roomTask = ManageGameSafeAsync(roomKey, cancellationToken);
                        activeRoomTasks.Add(roomKey, roomTask);
                    }
                };

                // Remove completed tasks
                var completedTasks = activeRoomTasks.Where(kvPair => kvPair.Value.IsCompleted).ToList();
                foreach (var completedTask in completedTasks)
                {
                    await roomGameManager.UnlockRoomAsync(completedTask.Key);
                    activeRoomTasks.Remove(completedTask.Key);
                    logger.LogInformation($"Room {completedTask.Key} task completed");
                }

                await Task.Delay(GameConstants.RoomCheckDelayInMs, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, $"Critical Unmanaged error in {nameof(GamesService)}");
            }
        }
    }

    private async Task ManageGameSafeAsync(Guid roomId, CancellationToken cancellationToken)
    {
        try
        {
            await ManageGameAsync(roomId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Unmanaged error in room {roomId}");
        }
    }

    private async Task ManageGameAsync(Guid roomId, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && hub != null)
        {
            var roomState = await roomsDictionary.UnsafeGetRoomStateAsync(roomId);

            if (!roomState.MustPlayGame && !roomState.GameMustReset)
            {
                logger.LogInformation($"Room {roomId} Game must not play or reset, waiting");
                await Task.Delay(GameConstants.IdleDelayInMs, cancellationToken);
                continue;
            }

            if (roomState.MustPlayGame)
            {
                logger.LogDebug($"Room {roomId} Game must play");

                // Faccio sempre muovere la palla
                var pointPlayerName = await roomGameManager.UpdateBallPosition(roomId);

                // Se nessuno ha fatto punto
                if (string.IsNullOrEmpty(pointPlayerName))
                {
                    await ManageNoPlayerPoint(roomState);
                }
                else
                {
                    await ManagePlayerPoint(roomState, pointPlayerName, cancellationToken);
                }
            }

            if (roomState.GameMustReset)
            {
                logger.LogInformation($"Room {roomId} Game must reset");

                var gameOverMessage = await roomGameManager.GetGameOverMessage(roomId);
                await hub.Clients.Group(roomState.RoomId.ToString()).UpdateGameMessage(gameOverMessage);
                await roomGameManager.InitializeGameObjectsOnServer(roomId, true);
                return;
            }
            else
            {
                logger.LogDebug($"Room {roomId} Game must not reset");
                await Task.Delay(GameConstants.GameDelayBetweenTicksInMs, cancellationToken);
            }
        }
    }

    private async Task ManagePlayerPoint(RoomState roomState, string pointPlayerName, CancellationToken cancellationToken)
    {
        int playerPoints;
        Roles playerType;
        // Altrimenti aggiungo il punto e resetto il tutto
        if (pointPlayerName.Equals(GameConstants.Player1RoleAsString))
        {
            playerPoints = await roomGameManager.AddPlayer1Point(roomState.RoomId);
            playerType = Roles.Player1;
        }
        else
        {
            playerPoints = await roomGameManager.AddPlayer2Point(roomState.RoomId);
            playerType = Roles.Player2;
        }

        await hub.Clients.Group(roomState.RoomId.ToString()).UpdatePlayerPoints(playerType, playerPoints);
        if (!roomState.GameMustReset)
        {
            await Task.Delay(GameConstants.DelayAfterPointInMs, cancellationToken);
        }
    }

    private async Task ManageNoPlayerPoint(RoomState roomState)
    {
        foreach (var kvPair in roomState.GameObjectsDictionary
                            .Where(kvPair => kvPair.Value != null
                                && kvPair.Value.WasUpdated))
        {
            kvPair.Value!.LastTimeServerReceivedUpdate = systemClock.UtcNow.Ticks;
            kvPair.Value!.LastSinglaRServerReceivedUpdateName = Environment.MachineName;

            if (kvPair.Value.LastUpdatedBy != null && !kvPair.Value.LastUpdatedBy.Equals(GameConstants.ServerRoleAsString))
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
