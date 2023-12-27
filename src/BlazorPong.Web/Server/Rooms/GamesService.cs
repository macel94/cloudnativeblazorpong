using BlazorPong.Web.Server.Rooms.Game;
using BlazorPong.Web.Server.Rooms.Game.SignalRHub;
using BlazorPong.Web.Shared;
using Microsoft.AspNetCore.SignalR;

namespace BlazorPong.Web.Server.Rooms;

public class GamesService(IHubContext<GameHub, IBlazorPongClient> hub, RoomGameManager roomGameManager, ILogger<GamesService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && hub != null)
        {
            try
            {
                if (roomGameManager.MustPlayGame())
                {
                    // Faccio sempre muovere la palla
                    var pointPlayerName = roomGameManager.UpdateBallPosition();

                    // Se nessuno ha fatto punto
                    if (string.IsNullOrEmpty(pointPlayerName))
                    {
                        foreach (var kvPair in roomGameManager.GameObjectsDict
                            .Where(kvPair => kvPair.Value != null
                                && kvPair.Value.WasUpdated))
                        {
                            kvPair.Value!.LastTickServerReceivedUpdate = DateTimeOffset.UtcNow.Ticks;

                            // Se so chi ha fatto l'update evito di mandarglielo
                            if (kvPair.Value.LastUpdatedBy != null && !kvPair.Value.LastUpdatedBy.Equals("server"))
                            {
                                await hub.Clients.AllExcept(kvPair.Value.LastUpdatedBy).UpdateGameObjectPositionOnClient(kvPair.Value);
                            }
                            else
                            {
                                await hub.Clients.All.UpdateGameObjectPositionOnClient(kvPair.Value);
                            }
                        }
                    }
                    else
                    {
                        int playerPoints;
                        ClientType playerType;
                        // Altrimenti aggiungo il punto e resetto il tutto
                        if (pointPlayerName.Equals("player1"))
                        {
                            playerPoints = roomGameManager.AddPlayer1Point();
                            playerType = ClientType.Player1;
                        }
                        else
                        {
                            playerPoints = roomGameManager.AddPlayer2Point();
                            playerType = ClientType.Player2;
                        }

                        await hub.Clients.All.UpdatePlayerPoints(playerType, playerPoints);
                        // Da il tempo di visualizzare il messaggio del punto se il gioco non deve essere resettato
                        if (!roomGameManager.MustReset())
                        {
                            await Task.Delay(3000, cancellationToken);
                        }
                    }
                }

                if (roomGameManager.MustReset())
                {
                    var gameOverMessage = roomGameManager.GetGameOverMessage();
                    await hub.Clients.All.UpdateGameMessage(gameOverMessage);
                }
                else
                {
                    await Task.Delay(GameConstants.GameDelayBetweenTicksInMs, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in RoomGamesService");
            }
        }
    }
}

public class RoomService(RoomGameManager roomGameManager, ILogger<RoomService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (await roomGameManager.TryLockRoomAsync(Environment.MachineName))
                {
                    logger.LogInformation($"Room locked for server: {Environment.MachineName}");
                }
                else
                {
                    await Task.Delay(GameConstants.RoomCheckDelayBetweenTicksInMs, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in RoomGamesService");
            }
        }
    }
}
