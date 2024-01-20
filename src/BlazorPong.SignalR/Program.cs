﻿using BlazorPong.SignalR;
using BlazorPong.SignalR.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogging();
builder.AddRedis();
builder.AddAzureSql();
builder.AddHostedServices();
builder.AddGameServices();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAnyOriginPolicy",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyHeader()
                   .AllowAnyMethod();
        });
});

var app = builder.Build();
app.UseCors("AllowAnyOriginPolicy");
app.MapHub<GameHub>("/gamehub");

app.Run();
