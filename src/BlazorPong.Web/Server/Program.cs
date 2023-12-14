using BlazorPong.Web.Server.SignalRHub;
using Microsoft.AspNetCore.SignalR.Client;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllersWithViews();
builder.Services.AddLogging();
builder.Services.AddRazorPages();
builder.Services.AddTransient<HubConnectionBuilder>();
//This will scale differently
builder.Services.AddSingleton<ServerGameController>();
builder.Services.AddSignalR();
builder.Services.AddHostedService<Broadcaster>();

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
