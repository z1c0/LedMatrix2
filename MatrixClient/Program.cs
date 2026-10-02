using MatrixClient.Models;
using MatrixClient.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();

var rulesPath = Path.Combine(builder.Environment.ContentRootPath, "rules.yaml");
var rulesFound = File.Exists(rulesPath);
var autoSendConfig = rulesFound
    ? AutoSendConfig.FromYaml(File.ReadAllText(rulesPath))
    : new AutoSendConfig();

// Fail at startup rather than when the background service first trips over a bad rule
var ruleErrors = autoSendConfig.Validate(Path.Combine(builder.Environment.WebRootPath, "data"));
if (ruleErrors.Count > 0)
    throw new InvalidOperationException($"Invalid {rulesPath}:\n  " + string.Join("\n  ", ruleErrors));

builder.Services.AddSingleton(autoSendConfig);

// Clock, calendar and schedule run on this zone regardless of the host's TZ (containers default to UTC)
var timeZoneId = builder.Configuration["TimeZone"];
var timeProvider = string.IsNullOrWhiteSpace(timeZoneId)
    ? TimeProvider.System
    : new ZonedTimeProvider(TimeZoneInfo.FindSystemTimeZoneById(timeZoneId));
builder.Services.AddSingleton(timeProvider);
builder.Services.AddSingleton<GalleryService>();
builder.Services.AddSingleton<DynamicImageService>();
builder.Services.AddSingleton<AutoSendService>();
var picoConfig = builder.Configuration.GetSection("Pico").Get<PicoConfig>() ?? new PicoConfig();
builder.Services.AddSingleton(picoConfig);
builder.Services.AddSingleton<PicoStatus>();
builder.Services.AddSingleton<IPicoProbe, TcpPicoProbe>();
builder.Services.AddSingleton<PixelSenderService>();
builder.Services.AddHostedService<AutoSendBackgroundService>();

var weatherConfig = builder.Configuration.GetSection("Weather").Get<WeatherConfig>() ?? new WeatherConfig();
builder.Services.AddSingleton(weatherConfig);
builder.Services.AddSingleton<WeatherService>();

var app = builder.Build();

app.Logger.LogInformation("Local time zone: {TimeZone}", timeProvider.LocalTimeZone.Id);
if (!rulesFound)
    app.Logger.LogWarning("No rules file at {Path}; auto-send has no schedule", rulesPath);
foreach (var overlap in autoSendConfig.DescribeOverlaps())
    app.Logger.LogInformation("Schedule overlap: {Overlap}", overlap);

if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler("/Home/Error");

// Plain HTTP only: the container listens on 8080 and the app is LAN-only, so no HTTPS redirect/HSTS
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
