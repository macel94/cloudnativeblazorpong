﻿using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
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

    public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
    {
        builder.ConfigureOpenTelemetry();

        builder.AddDefaultHealthChecks();

        return builder;
    }

    public static IHostApplicationBuilder ConfigureOpenTelemetry(this IHostApplicationBuilder builder)
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
            //logging.AddConsoleExporter();
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddRuntimeInstrumentation()
                       .AddBuiltInMeters();
                //metrics.AddConsoleExporter();
            })
            .WithTracing(tracing =>
            {
                if (builder.Environment.IsDevelopment())
                {
                    // We want to view all traces in development
                    tracing.SetSampler(new AlwaysOnSampler());
                }

                tracing.AddAspNetCoreInstrumentation()
                       .AddGrpcClientInstrumentation()
                       .AddHttpClientInstrumentation();
                
                //tracing.AddConsoleExporter();
            });

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    private static IHostApplicationBuilder AddOpenTelemetryExporters(this IHostApplicationBuilder builder)
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.Configure<OpenTelemetryLoggerOptions>(logging => logging.AddOtlpExporter());
            builder.Services.ConfigureOpenTelemetryMeterProvider(metrics => metrics.AddOtlpExporter());
            builder.Services.ConfigureOpenTelemetryTracerProvider(tracing => tracing.AddOtlpExporter());
        }

        return builder;
    }

    public static IHostApplicationBuilder AddDefaultHealthChecks(this IHostApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks()
            // Add a default liveness check to ensure app is responsive
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // All health checks must pass for app to be considered ready to accept traffic after starting
        app.MapHealthChecks("/health");

        // Only health checks tagged with the "live" tag must pass for app to be considered alive
        app.MapHealthChecks("/alive", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("live")
        });

        return app;
    }

    private static MeterProviderBuilder AddBuiltInMeters(this MeterProviderBuilder meterProviderBuilder) =>
        meterProviderBuilder.AddMeter(
            "Microsoft.AspNetCore.Hosting",
            "Microsoft.AspNetCore.Server.Kestrel",
            "System.Net.Http");
}
