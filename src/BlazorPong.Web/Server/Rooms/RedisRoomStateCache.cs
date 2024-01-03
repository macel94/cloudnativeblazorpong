using System.Text.Json;
using BlazorPong.Web.Server.Cache;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;

namespace BlazorPong.Web.Server.Rooms;

public class RedisRoomStateCache(IDistributedCache cache, IServer redisServer, IDatabase redisDatabase)
{
    private readonly IDistributedCache _cache = cache;
    private readonly TimeSpan _expiration = TimeSpan.FromMinutes(10); // Example expiration time

    // Get the list of keys of rooms in Redis
    public List<Guid> GetRoomKeys()
    {
        // Only for this type of low level search we also need to add channelprefix
        var keys = redisServer.Keys(redisDatabase.Database, CacheConstants.RoomKeysSearchValue).Select(x =>
        {
            var key = x.ToString();
            // And here we need to remove it
            var res = RevertToActualKey(key.Replace(CacheConstants.ChannelPrefix, ""));
            return res;
        }).ToList();
        return keys;
    }

    public async ValueTask<RoomState> UnsafeGetRoomStateAsync(Guid roomId)
    {
        // TODO - Limit the times this method is called to avoid Redis cache spamming
        var cachedData = await _cache.GetStringAsync(GetActualKey(roomId));
        return JsonSerializer.Deserialize<RoomState>(cachedData!)!;
    }

    public async Task SetRoomStateAsync(Guid roomId, RoomState roomState)
    {
        var serializedData = JsonSerializer.Serialize(roomState);
        var setKey = GetActualKey(roomId);
        await _cache.SetStringAsync(setKey, serializedData, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _expiration
        });
    }

    private static string GetActualKey(Guid roomId)
    {
        return $"{CacheConstants.RoomPrefix}{roomId}";
    }

    private static Guid RevertToActualKey(string key)
    {
        return Guid.Parse(key.Replace(CacheConstants.RoomPrefix, ""));
    }
}
