using BlazorPong.Web.Server.Rooms;
using BlazorPong.Web.Server.Rooms.Games;
using BlazorPong.Web.Server.Rooms.Games.Hubs;
using BlazorPong.Web.Shared.Clock;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddLogging();
builder.Services.AddRazorPages();

builder.AddRedis();
builder.AddAzureSql();

builder.Services.AddTransient<BallManager>();

builder.Services.AddSingleton<ISystemClock, SystemClock>();
builder.Services.AddSingleton<RoomsManager>();
builder.Services.AddHostedService<RoomService>();
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
