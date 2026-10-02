# TODO

## MatrixClient review (2026-10-02)

### High

- [x] **`rules.yaml` isn't in the publish output, so production probably runs without schedule rules.** The Web SDK doesn't copy `.yaml` files when publishing (confirmed with `dotnet publish`: the output has `appsettings*.json` but no `rules.yaml`). In the container, `File.Exists(rulesPath)` in `Program.cs` is false and the app quietly falls back to an empty `AutoSendConfig`. Auto-send then never fires, and the Schedule page says "No rules defined". Fix: add `<None Update="rules.yaml" CopyToPublishDirectory="PreserveNewest" />` (or `<Content Include=...>`) to `MatrixClient.csproj`, unless the k8s deployment mounts it as a ConfigMap. Also log a warning when the file is missing.
- [ ] **Time zone in the container.** `ClockRenderer`, `CalendarRenderer` and `AutoSendService.GetActiveRule` all use `DateTime.Now`. The `aspnet:10.0-noble-chiseled` base image defaults to UTC, and as far as I know the plain chiseled image doesn't include tzdata. The clock, the calendar day and every schedule rule would then be off by 1–2 h (Europe/Vienna). Check on the running pod. Fix: set `TZ=Europe/Vienna` and use the `-extra` chiseled image (or copy tzdata in), or make the time zone a config value and convert explicitly with `TimeZoneInfo`.
- [ ] **Auto-send is off after every restart.** `AutoSendService._enabled` lives only in memory and starts as `false`. CI runs `kubectl rollout restart` on every push to `main`, so each deploy silently turns auto-send off. Persist the flag (file or volume) or add a config default such as `AutoSend:EnabledOnStartup`.
- [ ] **A bad rule time can take down the host.** `GetActiveRule` calls `TimeSpan.Parse(r.From/To)` every minute, outside the `try` in `AutoSendBackgroundService.ExecuteAsync`. A typo in `rules.yaml` (such as `"7:0x"`) throws from the background service. With the .NET 8+ default (`BackgroundServiceExceptionBehavior.StopHost`) that stops the whole app, and `/AutoSend/Status` returns 500s. Fix: parse and validate rules once at startup (days, times, `from <= to`, known category) and fail fast with a clear message.

### Medium

- [ ] **Path traversal in `Pixels/SendToMatrix`.** The `fileName` goes straight into `Path.Combine(env.WebRootPath, "data", path)` in `PixelSenderService.ResolveImageAsync`. Values like `../../x.png` or absolute paths (`Path.Combine` drops earlier segments when given a rooted path) can load any image on the filesystem. The `e.Message` returned to the client also tells you whether a file exists. Fix: resolve the full path and check it starts with the data root, or only accept paths that are in the gallery list.
- [ ] **Double URL-decoding.** MVC model binding already decodes `fileName`, and `PixelController` then calls `WebUtility.UrlDecode` again. File names containing `+` or `%` break (`+` turns into a space). Remove the extra decode.
- [ ] **No HTTP timeout to the Pico.** `httpClientFactory.CreateClient()` uses the default 100 s timeout. If the Pico is offline, the "Send" button hangs for over a minute and the auto-send loop stalls. Register a named or typed client with a short timeout (around 5 s).
- [ ] **The Pico URL is hardcoded** (`PixelSenderService.cs:9`, `http://192.168.1.225/`). Move it to `appsettings.json` / an environment variable (`Pico:Url`) so a DHCP change doesn't need a rebuild and redeploy.
- [ ] **Manual and automatic sends can overlap.** A manual send, a Draw send and the background loop can all POST to the Pico at the same time. The CircuitPython HTTP server handles one request at a time. Serialize sends in `PixelSenderService` with a `SemaphoreSlim(1)`.
- [ ] **The clock lags by up to about a minute.** The background loop uses `Task.Delay(1 min)` and isn't aligned to minute boundaries, so the displayed time can be up to ~59 s behind (plus drift from the time each send takes). Align the next tick to the start of the next minute, and send the clock right away when its rule becomes active.
- [ ] **Misleading weather fallback.** `WeatherService._cached` starts as `(0, 0, "°C")`. If the first Open-Meteo call fails, the matrix shows "0 °C, sunny" as if it were real data. Render an error or "--" state instead. Also, after a failure the cache expiry isn't set, so every call retries right away. Add a short back-off.

### Low / cleanup

- [ ] **Dynamic modes are listed in three places:** `DynamicController.KnownModes`, `AutoSendService.GetImagePath` and `PixelSenderService.ResolveImageAsync`, plus the hardcoded `<img>`s in `Views/Home/Index.cshtml`. Use one registry such as `IDictionary<string, Func<Task<Image<Rgba32>>>>`. `DynamicController.IsDynamic` isn't used anywhere.
- [ ] **`SendAsync` and `SendRawAsync` duplicate the POST and error handling.** Have `SendAsync` build the hex string and call `SendRawAsync`. Also dispose the `HttpResponseMessage` and the `JsonDocument` (in `WeatherService`).
- [ ] **`Draw/Send` only checks the length.** It doesn't check that the payload is hex. Validate with a regex (`^[0-9A-Fa-f]{6144}$`) before forwarding to the Pico. Use `BadRequest` instead of `throw new Exception(...)` for flow control.
- [ ] **Generic `Exception` with `e.Message` sent to the client** in the controllers. Use specific exception types and return a fixed user-facing message; keep the details in the logs.
- [ ] **`HomeController` uses `Directory.GetCurrentDirectory()`** to build the data path, while `AutoSendService` uses `env.WebRootPath`. Inject `IWebHostEnvironment` and use `WebRootPath` everywhere; the current approach breaks when the app is started from another directory.
- [ ] **`Directory.GetFiles(dir, "*.*")`** picks up any file (`.DS_Store`, `Thumbs.db`, …). Auto-send could choose one at random and fail. Filter by image extension (`.png`, `.bmp`, `.jpg`).
- [ ] **Rule time boundaries.** `to: "23:59"` is compared with `<=` against `TimeOfDay`, so the last 59 seconds of the day match nothing. Rules that cross midnight (`from > to`) aren't supported, and they'd draw with a negative height on the Schedule page. Consider an exclusive `to` with `24:00` allowed, or explicit wrap-around support.
- [ ] **Overlapping rules: the first match wins without any warning.** For example, "Evening Clock" never runs on Fri/Sat because "Gaming Night" comes earlier in the list. The Schedule page draws both blocks on top of each other. Show precedence in the UI or warn about overlaps at startup.
- [ ] **`/AutoSend/Status` always reports the global `IntervalMinutes`,** even when the active rule overrides it (for example the clock rules at 1 min). Return `rule.IntervalMinutes ?? Config.IntervalMinutes`.
- [ ] **Dynamic previews can be cached by the browser.** `/Dynamic/Preview/{mode}` has no cache headers, so the gallery thumbnails for clock/weather can go stale. Add `[ResponseCache(NoStore = true)]`.
- [ ] **`WeatherConfig.TemperatureUnit` isn't validated.** Any value other than `fahrenheit` is labelled °C but still sent to the API as-is. Use an enum.
- [x] **Dockerfile:** it runs `dotnet build -o /app` and then `dotnet publish -o /app` into the same folder, which is redundant and mixes outputs. Publish only (`dotnet publish -c Release -o /app/publish`) and copy from there. There's no `.dockerignore`, so `bin/`, `obj/` and `.vscode/` go into the build context.
- [ ] **`UseHttpsRedirection` in a container that only serves HTTP on 8080** logs "Failed to determine the https port" and does nothing. Remove it, or configure `ForwardedHeaders` if TLS ends at the ingress.
- [ ] **Unused `using System.Globalization;`** in `Program.cs`. `HomeController` uses tabs and a classic constructor, while the other controllers use spaces and primary constructors.
- [x] **No tests.** `PixelFont`, the renderers (snapshot of the pixel output), rule matching and the hex encoding are all easy to unit-test.

### Docs drift (CLAUDE.md)

- [ ] CLAUDE.md says the Pico IP lives in `PixelController.cs` as `http://192.168.1.41/`. It's actually in `Services/PixelSenderService.cs` and is `http://192.168.1.225/`. The data-flow section also credits `PixelController` with the conversion, which `PixelSenderService` now does. The dependencies list is missing `YamlDotNet`.
