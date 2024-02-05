using AspNetCore.SignalR.OpenTelemetry;
using BlazorPong.SignalR.Cache;
using BlazorPong.SignalR.EFCore;
using BlazorPong.SignalR.Rooms;
using BlazorPong.SignalR.Rooms.Games;
using BlazorPong.Web.Shared.Clock;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace BlazorPong.SignalR;

// THIS FILE IS DUPLICATED ON PURPOSE
public static class WebApplicationBuilderExtensions
{
    public static void AddRedis(this WebApplicationBuilder builder)
    {
        var redisCs = builder.Configuration.GetConnectionString("Redis") ?? throw new Exception("Redis CS is missing");
        builder.Services.AddSignalR()
            .AddHubInstrumentation()
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

    public static void AddGameServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddTransient<IBallManager, BallManager>();
        builder.Services.AddSingleton<ISystemClock, SystemClock>();
        builder.Services.AddSingleton<IRoomsManager, RoomsManager>();
    }

    public static void AddHostedServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddHostedService<RoomService>();
        builder.Services.AddHostedService<GamesService>();
    }
}
