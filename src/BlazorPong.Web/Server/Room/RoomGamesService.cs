using BlazorPong.Web.Server.SignalRHub;
using BlazorPong.Web.Shared;
using Microsoft.AspNetCore.SignalR;

namespace BlazorPong.Web.Server.Room;

public class RoomGamesService : BackgroundService
{
    private readonly IHubContext<GameHub, IBlazorPongClient> _hubContext;
    private readonly RoomGameManager _roomGameManager;
    private readonly ILogger<RoomGamesService> _logger;

    public RoomGamesService(IHubContext<GameHub, IBlazorPongClient> hub, RoomGameManager roomGameManager, ILogger<RoomGamesService> logger)
    {
        _hubContext = hub;
        _roomGameManager = roomGameManager;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _hubContext != null)
        {
            if (_roomGameManager.MustPlayGame())
            {
                // Faccio sempre muovere la palla
                var pointPlayerName = _roomGameManager.UpdateBallPosition();

                // Se nessuno ha fatto punto
                if (string.IsNullOrEmpty(pointPlayerName))
                {
                    foreach (var kvPair in _roomGameManager.GameObjectsDict.Where(g => g.Value.WasUpdated))
                    {
                        kvPair.Value.LastTickServerReceivedUpdate = DateTimeOffset.UtcNow.Ticks;

                        // Se so chi ha fatto l'update evito di mandarglielo
                        if (kvPair.Value.LastUpdatedBy != null && !kvPair.Value.LastUpdatedBy.Equals("server"))
                        {
                            await _hubContext.Clients.AllExcept(kvPair.Value.LastUpdatedBy).UpdateGameObjectPositionOnClient(kvPair.Value);
                        }
                        else
                        {
                            await _hubContext.Clients.All.UpdateGameObjectPositionOnClient(kvPair.Value);
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
                        playerPoints = _roomGameManager.AddPlayer1Point();
                        playerType = ClientType.Player1;
                    }
                    else
                    {
                        playerPoints = _roomGameManager.AddPlayer2Point();
                        playerType = ClientType.Player2;
                    }

                    await _hubContext.Clients.All.UpdatePlayerPoints(playerType, playerPoints);
                    // Da il tempo di visualizzare il messaggio del punto se il gioco non deve essere resettato
                    if (!_roomGameManager.MustReset())
                    {
                        await Task.Delay(3000, cancellationToken);
                    }
                }
            }

            if (_roomGameManager.MustReset())
            {
                var gameOverMessage = _roomGameManager.GetGameOverMessage();
                await _hubContext.Clients.All.UpdateGameMessage(gameOverMessage);
            }
            else
            {
                await Task.Delay(10, cancellationToken);
            }
        }
    }
}
