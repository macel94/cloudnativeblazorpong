using System.Text;
using BlazorPong.Web.Shared;

namespace BlazorPong.Web.Server.Game;

public class BallManager
{
    private StringBuilder _stringBuilder = new();
    private readonly ILogger _logger;

    public GameObject Ball { get; private set; }
    private int _angle;
    private readonly float _speed;
    private CollisionItem _lastCollisionItem;

    public BallManager(GameObject ballGameObject, ILogger logger)
    {
        ballGameObject.Left = 50;
        ballGameObject.Top = 50;

        Ball = ballGameObject;
        _logger = logger;
        _speed = 0.3f;
        var random = new Random(DateTime.Now.Millisecond);
        var next = random.Next(1, 5);
        switch (next)
        {
            case 1:
                _angle = 45;
                break;
            case 2:
                _angle = 135;
                break;
            case 3:
                _angle = 225;
                break;
            case 4:
                _angle = 315;
                break;
        }
    }

    public bool VerifyObjectsCollision(GameObject gameObjectA, GameObject gameObjectB)
    {
        var aLeft = gameObjectA.Left;
        var aTop = gameObjectA.Top;
        var aWidth = gameObjectA.Width;
        var aHeight = gameObjectA.Height;
        var bLeft = gameObjectB.Left;
        var bTop = gameObjectB.Top;
        var bWidth = gameObjectB.Width;
        var bHeight = gameObjectB.Height;

        var result = !(aTop + aHeight <= bTop ||
                    aTop >= bTop + bHeight ||
                    aLeft + aWidth <= bLeft ||
                    aLeft >= bLeft + bWidth);

        if (result)
        {
            var debugMsg = _stringBuilder
                            .Clear()
                            .AppendLine($"Collision detected between {gameObjectA.Id} and {gameObjectB.Id}")
                            .AppendLine($"aLeft: {aLeft}, aTop: {aTop}, aWidth: {aWidth}, aHeight: {aHeight}")
                            .AppendLine($"bLeft: {bLeft}, bTop: {bTop}, bWidth: {bWidth}, bHeight: {bHeight}")
                            .ToString();
            _logger.LogDebug(debugMsg);
        }

        return result;
    }

    private string HandleCollisions()
    {
        if (Ball.Left <= GameConstants.LeftBounds)
        {
            return "player2";
        }

        if (Ball.Left >= GameConstants.RightBounds)
        {
            return "player1";
        }


        if (Ball.Top <= GameConstants.BottomBounds ||
            Ball.Top >= GameConstants.TopBounds)
        {
            _lastCollisionItem = CollisionItem.Wall;
            HandleHorizontalWallCollision();
        }

        return string.Empty;
    }

    private void HandleVerticalWallCollision()
    {
        switch (_angle)
        {
            case 45: _angle = 135; break;
            case 135: _angle = 45; break;
            case 225: _angle = 315; break;
            case 315: _angle = 225; break;
        }
    }

    private void HandleHorizontalWallCollision()
    {
        switch (_angle)
        {
            case 45: _angle = 315; break;
            case 135: _angle = 225; break;
            case 225: _angle = 135; break;
            case 315: _angle = 45; break;
        }
    }

    public string Update()
    {
        var currentTicks = DateTimeOffset.UtcNow.Ticks;
        Ball = Ball with
        {
            LastUpdatedBy = "server",
            LastTickServerReceivedUpdate = currentTicks,
            LastUpdateTicks = currentTicks + 1,// always needs to be re-rendered
            Left = Ball.Left + Math.Cos(_angle * GameConstants.DegreeToRadians) * _speed,
            Top = Ball.Top + Math.Sin(_angle * GameConstants.DegreeToRadians) * _speed
        };

        return HandleCollisions();
    }

    public void OnPlayer1Hit()
    {
        if (_lastCollisionItem == CollisionItem.Player1)
            return;

        HandleVerticalWallCollision();
        _lastCollisionItem = CollisionItem.Player1;
    }

    public void OnPlayer2Hit()
    {
        if (_lastCollisionItem == CollisionItem.Player2)
            return;

        HandleVerticalWallCollision();
        _lastCollisionItem = CollisionItem.Player2;
    }
}
