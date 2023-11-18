using BlazorPong.Web.Shared;

namespace BlazorPong.Web.Server.SignalRHub;

public class ServerGameController
{
    public Dictionary<string, GameObject> GameObjectsDict = new();
    private BallManager? _ballManager;
    private string? _player1ConnectionId;
    private string? _player2ConnectionId;
    private bool _player1Ready;
    private bool _player2Ready;
    private int _player1Points;
    private int _player2Points;
    private bool _gameMustReset;

    public bool MustReset()
    {
        return _gameMustReset;
    }

    public bool MustPlayGame()
    {
        return this.GameObjectsDict.Count == 3
            && this._player1ConnectionId != null
            && this._player2ConnectionId != null
            && this._player1Ready
            && this._player2Ready;
    }

    public string GetPlayer1ConnectionId()
    {
        return this._player1ConnectionId;
    }

    public string GetPlayer2ConnectionId()
    {
        return this._player2ConnectionId;
    }

    public void SetPlayer1ConnectionId(string id)
    {
        this._player1ConnectionId = id;
    }

    public void SetPlayer1IsReady(bool ready)
    {
        this._player1Ready = ready;
    }

    public void SetPlayer2IsReady(bool ready)
    {
        this._player2Ready = ready;
    }

    public void SetPlayer2ConnectionId(string id)
    {
        this._player2ConnectionId = id;
    }

    /// <summary>
    /// Passando true si forza la reinizializzazione degli oggetti,
    /// false si va in aggiunta nel caso ne manchi qualcuno
    /// </summary>
    /// <param name="forceInitialization"></param>
    public void InitializeGameObjectsOnServer(bool forceInitialization)
    {
        var tempInitGameObjects = new Dictionary<string, GameObject>()
        {
            {
                "player1",
                new GameObject(Id: "player1",
                    LastUpdatedBy: string.Empty,
                    Width : 20,
                    Height : 100)
                {
                    Left=10,
                    Top=50
                }
            },
            {
                "player2",
                new GameObject(Id : "player2",
                    LastUpdatedBy: string.Empty,
                    Width : 20,
                    Height : 100)
                {
                    Left=90,
                    Top=50
                }
            },
            {
                "ball",
                new GameObject(Id : "ball",
                    LastUpdatedBy: string.Empty,
                    Width : 20,
                    Height : 20)
                {
                    Left=50,
                    Top=50
                }
            }
        };

        if (!forceInitialization)
        {
            foreach (var tempInitPair in tempInitGameObjects)
            {
                if (!GameObjectsDict.TryGetValue(tempInitPair.Key, out var _))
                {
                    GameObjectsDict.Add(tempInitPair.Key, tempInitPair.Value);

                    if (tempInitPair.Key == "ball")
                        _ballManager = new BallManager(tempInitPair.Value);
                }
            }
        }
        else
        {
            GameObjectsDict = tempInitGameObjects;
            _ballManager = new BallManager(tempInitGameObjects["ball"]);
        }
    }

    public void OnPlayer1Hit()
    {
        _ballManager.OnPlayer1Hit();
    }

    public void OnPlayer2Hit()
    {
        _ballManager.OnPlayer2Hit();
    }

    public void UpdateGameObjectPositionOnServer(GameObject clientUpdatedObject)
    {
        var gameObject = GameObjectsDict[clientUpdatedObject.Id];

        if (gameObject != null)
        {
            gameObject = gameObject with
            {
                Left = clientUpdatedObject.Left,
                Top = clientUpdatedObject.Top,
                LastUpdatedBy = clientUpdatedObject.LastUpdatedBy,
                LastUpdateTicks = clientUpdatedObject.LastUpdateTicks,
                LastTickServerReceivedUpdate = DateTimeOffset.UtcNow.Ticks
            };

            GameObjectsDict[clientUpdatedObject.Id] = gameObject;
        }
    }

    public int AddPlayer1Point()
    {
        _player1Points++;
        InitializeGameObjectsOnServer(true);
        if (_player1Points == 3)
        {
            _gameMustReset = true;
        }
        return _player1Points;
    }

    public void Player1Disconnected()
    {
        InitializeGameObjectsOnServer(true);
        _player2Points = 3;
        _gameMustReset = true;
    }

    public int AddPlayer2Point()
    {
        _player2Points++;
        InitializeGameObjectsOnServer(true);
        if (_player2Points == 3)
        {
            _gameMustReset = true;
        }
        return _player2Points;
    }

    public void Player2Disconnected()
    {
        InitializeGameObjectsOnServer(true);
        _player1Points = 3;
        _gameMustReset = true;
    }

    public string GetGameOverMessage()
    {
        string result = null;
        // Dato che prendo il messaggio, riporto i punti a 0 come anche lo stato di player ready
        if (_player1Points == 3)
        {
            result = "Player1 won!";
        }
        else if (_player2Points == 3)
        {
            result = "Player2 won!";
        }

        _player1Points = 0;
        _player2Points = 0;
        _player2Ready = false;
        _player1Ready = false;
        _gameMustReset = false;

        return result;
    }

    internal string UpdateBallPosition()
    {
        return _ballManager.Update();
    }
}
