﻿using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;

namespace BlazorPong.Web.Server.Rooms;

public class RedisRoomStateCache(IDistributedCache cache, IServer redisServer, IDatabase redisDatabase)
{
    private readonly IDistributedCache _cache = cache;
    private readonly TimeSpan _expiration = TimeSpan.FromMinutes(10); // Example expiration time
    private static readonly string ChannelPrefix = Assembly.GetExecutingAssembly().GetName().Name!;
    private const string _roomPrefix = "room-";

    // Get the list of keys of rooms in Redis
    public List<Guid> GetRoomKeys()
    {
        // Only for this type of low level search we also need to add channelprefix
        var keys = redisServer.Keys(redisDatabase.Database, $"{ChannelPrefix}{_roomPrefix}*").Select(x =>
        {
            var key = x.ToString();
            // And here we need to remove it
            var res = RevertToActualKey(key.Replace(ChannelPrefix, ""));
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
        return $"{_roomPrefix}{roomId}";
    }

    private static Guid RemoveChannelPrefix(string key)
    {
        return Guid.Parse(key.Replace(_roomPrefix, ""));
    }

    private static Guid RevertToActualKey(string key)
    {
        return Guid.Parse(key.Replace(_roomPrefix, ""));
    }
}
