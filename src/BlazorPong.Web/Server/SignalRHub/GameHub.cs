﻿using BlazorPong.Web.Shared;
using Microsoft.AspNetCore.SignalR;

namespace BlazorPong.Web.Server.SignalRHub;

public class GameHub : Hub<IBlazorPongClient>
{
    private readonly ServerGameController _gameController;

    // Tramite DI
    public GameHub(ServerGameController sgc)
    {
        _gameController = sgc;
    }

    public void UpdateGameObjectPosition(GameObject clientGameObject)
    {
        clientGameObject = clientGameObject with { LastUpdatedBy = Context.ConnectionId };
        _gameController.UpdateGameObjectPositionOnServer(clientGameObject);
    }

    public ClientType GetClientType()
    {
        if (_gameController.GetPlayer1ConnectionId() == Context.ConnectionId)
            return ClientType.Player1;

        if (_gameController.GetPlayer2ConnectionId() == Context.ConnectionId)
            return ClientType.Player2;

        return ClientType.Spectator;
    }

    public void OnPlayer1Hit()
    {
        _gameController.OnPlayer1Hit();
    }

    public void OnPlayer2Hit()
    {
        _gameController.OnPlayer2Hit();
    }

    public void SetPlayerIsReady()
    {
        if (_gameController.GetPlayer1ConnectionId() == Context.ConnectionId)
        {
            _gameController.SetPlayer1IsReady(true);
        }
        else if (_gameController.GetPlayer2ConnectionId() == Context.ConnectionId)
        {
            _gameController.SetPlayer2IsReady(true);
        }
    }

    public Dictionary<string, GameObject> GetGameObjects()
    {
        if (_gameController.GameObjectsDict == null || _gameController.GameObjectsDict.Count != 3)
        {
            // Aggiungo solo i mancanti se sono qui
            _gameController.InitializeGameObjectsOnServer(false);
        }

        return _gameController.GameObjectsDict;
    }

    public override async Task OnConnectedAsync()
    {
        // Teniamo così traccia di chi è quale player
        if (_gameController.GetPlayer1ConnectionId() == null)
        {
            _gameController.SetPlayer1ConnectionId(Context.ConnectionId);
        }
        else if (_gameController.GetPlayer2ConnectionId() == null)
        {
            _gameController.SetPlayer2ConnectionId(Context.ConnectionId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        if (_gameController.GetPlayer1ConnectionId() == Context.ConnectionId)
        {
            if (_gameController.MustPlayGame())
            {
                _gameController.Player1Disconnected();
            }
            _gameController.SetPlayer1ConnectionId(null);
        }
        else if (_gameController.GetPlayer2ConnectionId() == Context.ConnectionId)
        {
            if (_gameController.MustPlayGame())
            {
                _gameController.Player2Disconnected();
            }
            _gameController.SetPlayer2ConnectionId(null);
        }

        await base.OnDisconnectedAsync(exception);
    }
}
