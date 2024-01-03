namespace BlazorPong.Web.Shared.Clock;

public sealed class SystemClock : ISystemClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
