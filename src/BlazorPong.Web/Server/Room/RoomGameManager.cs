using BlazorPong.Web.Server.Room.Game;
using BlazorPong.Web.Shared;

namespace BlazorPong.Web.Server.Room;

public class RoomGameManager(BallManager ballManager)
{
    public Dictionary<string, GameObject?> GameObjectsDict = [];
    private BallManager _ballManager = ballManager;
    private string _player1ConnectionId = string.Empty;
    private string _player2ConnectionId = string.Empty;
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
        return GameObjectsDict.Count == 3
            && !string.IsNullOrEmpty(_player1ConnectionId)
            && !string.IsNullOrEmpty(_player2ConnectionId)
            && _player1Ready
            && _player2Ready;
    }

    public string GetPlayer1ConnectionId()
    {
        return _player1ConnectionId;
    }

    public string GetPlayer2ConnectionId()
    {
        return _player2ConnectionId;
    }

    public void SetPlayer1ConnectionId(string id)
    {
        _player1ConnectionId = id;
    }

    public void SetPlayer1IsReady(bool ready)
    {
        _player1Ready = ready;
    }

    public void SetPlayer2IsReady(bool ready)
    {
        _player2Ready = ready;
    }

    public void SetPlayer2ConnectionId(string id)
    {
        _player2ConnectionId = id;
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
                new(Id: "player1",
                    LastUpdatedBy: string.Empty,
                    Width : 2,
                    Height : 9)
                {
                    Left=10,
                    Top=50
                }
            },
            {
                "player2",
                new(Id : "player2",
                    LastUpdatedBy: string.Empty,
                    Width: 2,
                    Height: 9)
                {
                    Left=90,
                    Top=50
                }
            },
            {
                "ball",
                new(Id : "ball",
                    LastUpdatedBy: string.Empty,
                    Width: 1.5,
                    Height: 3)
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
                        _ballManager.SetBall(tempInitPair.Value);
                }
            }
        }
        else
        {
            GameObjectsDict = tempInitGameObjects;
            _ballManager.SetBall(tempInitGameObjects["ball"]);
        }
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
        var result = string.Empty;
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
        var res = _ballManager!.Update();
        GameObjectsDict["ball"] = _ballManager.Ball;

        // Verify collisions between player1 and ball
        if (_ballManager.VerifyObjectsCollision(_ballManager.Ball, GameObjectsDict["player1"]))
        {
            _ballManager.OnPlayer1Hit();
        }
        if (_ballManager.VerifyObjectsCollision(_ballManager.Ball, GameObjectsDict["player2"]))
        {
            _ballManager.OnPlayer2Hit();
        }

        return res;
    }
}
