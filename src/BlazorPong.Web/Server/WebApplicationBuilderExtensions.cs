using System.Reflection;
using BlazorPong.Web.Server.Cache;
using BlazorPong.Web.Server.EFCore;
using BlazorPong.Web.Server.Rooms;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

public static class WebApplicationBuilderExtensions
{
    public static void AddRedis(this WebApplicationBuilder builder)
    {
        var redisCs = builder.Configuration.GetConnectionString("Redis") ?? throw new Exception("Redis CS is missing");
        builder.Services.AddSignalR()
            .AddStackExchangeRedis(redisCs,
            options =>
            {
                options.Configuration.ChannelPrefix = CacheConstants.ChannelPrefixLiteral;
            }
        );
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisCs;
            options.InstanceName = CacheConstants.ChannelPrefix;
        });
        var _redis = ConnectionMultiplexer.Connect(redisCs);
        var server = _redis.GetServer(_redis.GetEndPoints().First());
        var database = _redis.GetDatabase();
        builder.Services.AddSingleton(server);
        builder.Services.AddSingleton(database);
        builder.Services.AddSingleton<RedisRoomStateCache>();
    }

    public static void AddAzureSql(this WebApplicationBuilder builder)
    {
        var sqlConnectionString = builder.Configuration.GetConnectionString("AzureSql") ?? throw new Exception("Azure SQL connection string is missing");
        builder.Services.AddDbContext<PongDbContext>(options => options.UseSqlServer(sqlConnectionString), contextLifetime: ServiceLifetime.Singleton);
    }
}

