namespace BlazorPong.Web.Server.Rooms.Games;

internal static class GameConstants
{
    internal const double DegreeToRadians = Math.PI / 180;
    internal const int LeftBounds = 0;
    internal const int RightBounds = 100;
    internal const int BottomBounds = 0;
    internal const int TopBounds = 100;
#if DEBUG
    internal const float SpeedPerTick = 1.1f;
    internal const int GameDelayBetweenTicksInMs = 100;
#endif
#if RELEASE
    internal const float SpeedPerTick = 1.3f;
    internal const int GameDelayBetweenTicksInMs = 33;
#endif
    internal const int IdleDelayInMs = 10000;
    internal const int RoomCheckDelayInMs = 10000;
}
