using System.Reflection;
using StackExchange.Redis;

namespace BlazorPong.SignalR.Cache;

internal static class CacheConstants
{
    public static readonly string ChannelPrefix = $"{Assembly.GetExecutingAssembly().GetName().Name!}_";
    public static readonly RedisChannel ChannelPrefixLiteral = RedisChannel.Literal(ChannelPrefix);
    public const string RoomPrefix = "room-";
    public static readonly string RoomKeysSearchValue = $"{ChannelPrefix}{RoomPrefix}*";
}
