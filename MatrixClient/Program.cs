using MatrixClient.Models;
using MatrixClient.Services;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();

var rulesPath = Path.Combine(builder.Environment.ContentRootPath, "rules.yaml");
var autoSendConfig = File.Exists(rulesPath)
    ? new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .Build()
        .Deserialize<AutoSendConfig>(File.ReadAllText(rulesPath))
    : new AutoSendConfig();

builder.Services.AddSingleton(autoSendConfig);
builder.Services.AddSingleton<AutoSendService>();
builder.Services.AddSingleton<PixelSenderService>();
builder.Services.AddHostedService<AutoSendBackgroundService>();

var weatherConfig = builder.Configuration.GetSection("Weather").Get<WeatherConfig>() ?? new WeatherConfig();
builder.Services.AddSingleton(weatherConfig);
builder.Services.AddSingleton<WeatherService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
