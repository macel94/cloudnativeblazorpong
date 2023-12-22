namespace BlazorPong.Web.Server.Room.Game;

internal static class GameConstants
{
    internal const double DegreeToRadians = Math.PI / 180;
    internal const int LeftBounds = 0;
    internal const int RightBounds = 100;
    internal const int BottomBounds = 0;
    internal const int TopBounds = 100;
    internal const float SpeedPerTick = 1.3f;
    internal const int NextServerCheckMillisecondsDelay = 33;
}
