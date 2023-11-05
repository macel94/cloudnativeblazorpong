using BlazorPong.Web.Shared;
using Microsoft.AspNetCore.SignalR;

namespace BlazorPong.Web.Server.SignalRHub;

public class Broadcaster : BackgroundService
{
    private readonly IHubContext<GameHub, IBlazorPongClient> _hubContext;
    private readonly ServerGameController _gameController;

    public Broadcaster(IHubContext<GameHub, IBlazorPongClient> hub, ServerGameController gameController)
    {
        _hubContext = hub;
        _gameController = gameController;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _hubContext != null)
        {
            if (_gameController.MustPlayGame())
            {
                // Faccio sempre muovere la palla
                var pointPlayerName = _gameController.UpdateBallPosition();

                // Se nessuno ha fatto punto
                if (pointPlayerName == null)
                {
                    foreach (var kvPair in _gameController.GameObjectsDict.Where(g => g.Value.WasUpdated))
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
                        playerPoints = _gameController.AddPlayer1Point();
                        playerType = ClientType.Player1;
                    }
                    else
                    {
                        playerPoints = _gameController.AddPlayer2Point();
                        playerType = ClientType.Player2;
                    }

                    await _hubContext.Clients.All.UpdatePlayerPoints(playerType, playerPoints);
                    // Da il tempo di visualizzare il messaggio del punto se il gioco non deve essere resettato
                    if (!_gameController.MustReset())
                    {
                        await Task.Delay(3000, cancellationToken);
                    }
                }
            }

            if (_gameController.MustReset())
            {
                string gameOverMessage = _gameController.GetGameOverMessage();
                await _hubContext.Clients.All.UpdateGameMessage(gameOverMessage);
            }
            else
            {
                await Task.Delay(10, cancellationToken);
            }
        }
    }
}
