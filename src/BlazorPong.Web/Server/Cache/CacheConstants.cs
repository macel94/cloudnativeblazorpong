using System.Reflection;
using StackExchange.Redis;

namespace BlazorPong.Web.Server.Cache;

internal static class CacheConstants
{
    public static readonly string ChannelPrefix = $"{Assembly.GetExecutingAssembly().GetName().Name!}_";
    public static readonly RedisChannel ChannelPrefixLiteral = RedisChannel.Literal(ChannelPrefix);
    public const string RoomPrefix = "room-";
    public static readonly string RoomKeysSearchValue = $"{CacheConstants.ChannelPrefix}{CacheConstants.RoomPrefix}*";
}
