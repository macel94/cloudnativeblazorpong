using BlazorPong.SignalR;
using BlazorPong.SignalR.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogging();
builder.AddRedis();
builder.AddAzureSql();
builder.AddHostedServices();
builder.AddGameServices();

var app = builder.Build();
app.MapHub<GameHub>("/gamehub");

app.Run();
