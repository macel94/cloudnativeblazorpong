using System.Reflection;
using BlazorPong.Web.Server.Room;
using BlazorPong.Web.Server.Room.Game;
using BlazorPong.Web.Server.Room.Game.SignalRHub;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddLogging();
builder.Services.AddRazorPages();

//This will scale differently
builder.Services.AddSingleton<RoomGameManager>();
builder.Services.AddTransient<BallManager>();
builder.Services.AddSignalR()
    .AddStackExchangeRedis(builder.Configuration.GetConnectionString("Redis") ?? throw new Exception("Redis CS is missing"),
    options =>
    {
        var name = Assembly.GetExecutingAssembly().GetName().Name;
        options.Configuration.ChannelPrefix = name;
    }
);
builder.Services.AddHostedService<RoomGamesService>();

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
