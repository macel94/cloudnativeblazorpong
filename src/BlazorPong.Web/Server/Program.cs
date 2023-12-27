using System.Reflection;
using BlazorPong.Web.Server.Room;
using BlazorPong.Web.Server.Room.Game;
using BlazorPong.Web.Server.Room.Game.SignalRHub;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddLogging();
builder.Services.AddRazorPages();
var redisCs = builder.Configuration.GetConnectionString("Redis") ?? throw new Exception("Redis CS is missing");
var prjName = Assembly.GetExecutingAssembly().GetName().Name;
builder.Services.AddSignalR()
    .AddStackExchangeRedis(redisCs,
    options =>
    {
        options.Configuration.ChannelPrefix = prjName;
    }
);
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisCs;
    options.InstanceName = prjName;
});

var sqlConnectionString = builder.Configuration.GetConnectionString("AzureSql") ?? throw new Exception("Azure SQL connection string is missing");
builder.Services.AddDbContext<RoomDbContext>(options => options.UseSqlServer(sqlConnectionString));

builder.Services.AddSingleton<RoomGameManager>();
builder.Services.AddTransient<BallManager>();
builder.Services.AddHostedService<GamesService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error");
}

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();

app.MapRazorPages();
app.MapControllers();
app.MapHub<GameHub>("/gamehub");
app.MapFallbackToFile("index.html");

app.Run();
