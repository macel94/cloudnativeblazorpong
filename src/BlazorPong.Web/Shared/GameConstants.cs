namespace BlazorPong.Web.Shared;

public static class GameConstants
{
    public const double DegreeToRadians = Math.PI / 180;
    public const int LeftBounds = 0;
    public const int RightBounds = 100;
    public const int BottomBounds = 0;
    public const int TopBounds = 100;
#if DEBUG
    public const float SpeedPerTick = 1.1f;
    public const int GameDelayBetweenTicksInMs = 100;
#endif
#if RELEASE
    public const float SpeedPerTick = 1.3f;
    public const int GameDelayBetweenTicksInMs = 33;
#endif
    public const int IdleDelayInMs = 10000;
    public const int RoomCheckDelayInMs = 10000;
    public static readonly string BallRoleAsString = Roles.Ball.ToString();
    public static readonly string ServerRoleAsString = Roles.Server.ToString();
    public static readonly string SpectatorRoleAsString = Roles.Spectator.ToString();
    public static readonly string Player1RoleAsString = Roles.Player1.ToString();
    public static readonly string Player2RoleAsString = Roles.Player2.ToString();
}
