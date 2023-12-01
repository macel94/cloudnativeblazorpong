using BlazorPong.Web.Shared;

namespace BlazorPong.Web.Server.SignalRHub;

public class BallManager
{
    private enum CollisionItem
    {
        Wall = 0,
        Player1 = 1,
        Player2 = 2
    }

    private const double DegreeToRadians = Math.PI / 180;

    private const int LeftBounds = 0;
    private const int RightBounds = 100;
    private const int BottomBounds = 0;
    private const int TopBounds = 100;

    private GameObject _ball;
    public GameObject Ball { get => _ball; }
    private int _angle;
    private readonly float _speed;
    private CollisionItem _lastCollisionItem;

    public BallManager(GameObject ballGameObject)
    {
        ballGameObject.Left = 50;
        ballGameObject.Top = 50;

        _ball = ballGameObject;
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

        return !(
            aTop + aHeight <= bTop ||
            aTop >= bTop + bHeight ||
            aLeft + aWidth <= bLeft ||
            aLeft >= bLeft + bWidth
        );
    }

    private string HandleCollisions()
    {
        if (_ball.Left <= LeftBounds)
        {
            return "player2";
        }

        if (_ball.Left >= RightBounds)
        {
            return "player1";
        }


        if (_ball.Top <= BottomBounds ||
            _ball.Top >= TopBounds)
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
        _ball = _ball with
        {
            LastUpdatedBy = "server",
            LastTickServerReceivedUpdate = currentTicks,
            LastUpdateTicks = currentTicks + 1,// always needs to be re-rendered
            Left = _ball.Left + Math.Cos(_angle * DegreeToRadians) * _speed,
            Top = _ball.Top + Math.Sin(_angle * DegreeToRadians) * _speed
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
