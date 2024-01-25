using BlazorPong.Web.Server;
using BlazorPong.Web.Shared;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddLogging();
builder.Services.AddRazorPages();
builder.AddServiceDefaults();

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
app.MapDefaultEndpoints();
// Map get for GetBaseClientConfig endpoint that
app.MapGet("/api/GetBaseClientConfig",
    () => new BaseClientConfig(app.Configuration["GameHubEndpoint"] ?? throw new InvalidOperationException("GameHubEndpoint not found in config"))
);
app.MapRazorPages();
app.MapFallbackToFile("index.html");

app.Run();
