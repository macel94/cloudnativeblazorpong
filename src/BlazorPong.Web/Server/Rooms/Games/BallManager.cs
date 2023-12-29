using System.Text;
using BlazorPong.Web.Shared;

namespace BlazorPong.Web.Server.Rooms.Games;

public class BallManager(ILogger<BallManager> logger)
{
    private readonly StringBuilder _stringBuilder = new();
    private CollisionItem _lastCollisionItem;

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
            logger.LogDebug(debugMsg);
        }

        return result;
    }

    private string HandleCollisions(GameObject ball)
    {
        if (ball!.Left <= GameConstants.LeftBounds)
        {
            return "player2";
        }

        if (ball.Left >= GameConstants.RightBounds)
        {
            return "player1";
        }


        if (ball.Top <= GameConstants.BottomBounds ||
            ball.Top >= GameConstants.TopBounds)
        {
            _lastCollisionItem = CollisionItem.Wall;
            HandleHorizontalWallCollision();
        }

        return string.Empty;
    }

    private void HandleVerticalWallCollision()
    {
        // Angle needs to be in the roomstate and shared between servers
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

    public string Update(ref GameObject ball)
    {
        var currentTicks = DateTimeOffset.UtcNow.Ticks;
        ball = ball! with
        {
            LastUpdatedBy = "server",
            LastTickConnectedServerReceivedUpdate = currentTicks,
            LastSinglaRServerReceivedUpdateName = Environment.MachineName,
            LastUpdateTicks = currentTicks + 1,// the ball always needs to be re-rendered when received from the server
            Left = ball.Left + Math.Cos(_angle * GameConstants.DegreeToRadians) * GameConstants.SpeedPerTick,
            Top = ball.Top + Math.Sin(_angle * GameConstants.DegreeToRadians) * GameConstants.SpeedPerTick
        };

        return HandleCollisions(ball);
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
