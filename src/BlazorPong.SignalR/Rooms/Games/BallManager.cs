using System.Text;
using BlazorPong.Web.Shared;
using BlazorPong.Web.Shared.Clock;

namespace BlazorPong.SignalR.Rooms.Games;

public class BallManager(ILogger<BallManager> logger, ISystemClock systemClock) : IBallManager
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

    private string HandleCollisions(ref GameObject ball)
    {
        if (ball!.Left <= GameConstants.LeftBounds)
        {
            ball.LastUpdate = 0;
            return GameConstants.Player2RoleAsString;
        }

        if (ball.Left >= GameConstants.RightBounds)
        {
            ball.LastUpdate = 0;
            return GameConstants.Player1RoleAsString;
        }

        if (ball.Top <= GameConstants.BottomBounds ||
            ball.Top >= GameConstants.TopBounds)
        {
            _lastCollisionItem = CollisionItem.Wall;
            HandleHorizontalWallCollision(ref ball);
        }

        return string.Empty;
    }

    public static void HandleVerticalWallCollision(ref GameObject ball)
    {
        // Angle needs to be in the roomstate and shared between servers
        switch (ball.Angle)
        {
            case 45: ball.Angle = 135; break;
            case 135: ball.Angle = 45; break;
            case 225: ball.Angle = 315; break;
            case 315: ball.Angle = 225; break;
        }
    }

    private void HandleHorizontalWallCollision(ref GameObject ball)
    {
        switch (ball.Angle)
        {
            case 45: ball.Angle = 315; break;
            case 135: ball.Angle = 225; break;
            case 225: ball.Angle = 135; break;
            case 315: ball.Angle = 45; break;
        }
    }

    public string Update(ref GameObject ball)
    {
        var currentMilliseconds = systemClock.UtcNow.ToUnixTimeMilliseconds();
        long lastUpdateMilliseconds = ball.LastUpdate > 0 ? ball.LastUpdate : currentMilliseconds;

        // Convert ticks to milliseconds
        long millisecondsSinceLastUpdate = currentMilliseconds - lastUpdateMilliseconds;

        // Calculate how many updates should have occurred in this time frame
        double updatesCount = millisecondsSinceLastUpdate / GameConstants.GameDelayBetweenTicksInMs;

        // Update the position based on the number of elapsed updates
        double distanceToMove = updatesCount * GameConstants.SpeedPerTick;
        double angleInRadians = ball.Angle * GameConstants.DegreeToRadians;
        var leftMovement = Math.Cos(angleInRadians) * distanceToMove;
        var topMovement = Math.Sin(angleInRadians) * distanceToMove;

        ball.LastUpdatedBy = GameConstants.ServerRoleAsString;
        ball.LastTimeServerReceivedUpdate = currentMilliseconds;
        ball.LastSinglaRServerReceivedUpdateName = Environment.MachineName;
        // the ball always needs to be re-rendered when received from the server
        ball.LastUpdate = currentMilliseconds + 1;
        ball.Left = ball.Left + leftMovement;
        ball.Top = ball.Top + topMovement;

        return HandleCollisions(ref ball);
    }

    public void OnPlayer1Hit(ref GameObject ball)
    {
        if (_lastCollisionItem == CollisionItem.Player1)
            return;

        HandleVerticalWallCollision(ref ball);
        _lastCollisionItem = CollisionItem.Player1;
    }

    public void OnPlayer2Hit(ref GameObject ball)
    {
        if (_lastCollisionItem == CollisionItem.Player2)
            return;

        HandleVerticalWallCollision(ref ball);
        _lastCollisionItem = CollisionItem.Player2;
    }
}
