# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

LED pixel matrix controller. See [README.md](README.md) and the [blog post](https://wolfgang-ziegler.com/blog/remote-controlled-led-matrix) for background.

## Architecture

Two loosely coupled components communicating over HTTP:

| Component | Language / Runtime | Role |
|---|---|---|
| `Matrix/` | CircuitPython on Raspberry Pi Pico W | Hardware server — drives the 32×32 RGB LED matrix |
| `MatrixClient/` | C# ASP.NET Core MVC (.NET 10) | Web UI — selects images, converts pixels, POSTs to Pico |

**Data flow**: User selects a 32×32 image (or a dynamic mode: clock/calendar/weather) → `PixelSenderService` converts it to a 6,144-char hex string (one 6-char RGB value per pixel) → HTTP POST to the Pico W → `main.py` parses hex, converts to RGB565, writes to framebuffer. `AutoSendBackgroundService` does the same on a schedule from `rules.yaml`.

## Build & Run

### MatrixClient (C#)
```bash
cd MatrixClient

# Run locally
dotnet run

# Build release
dotnet build -c Release

# Run tests (xUnit project lives outside the Docker build context, in ../MatrixClient.Tests)
dotnet test MatrixClient.sln

# Docker
docker build -f Dockerfile -t matrixclient .
docker run -p 8080:8080 matrixclient
```

CI (Woodpecker) builds via Kaniko and deploys with `kubectl rollout restart deployment/pixelmatrix -n pixelmatrix` on push to `main`.

### Matrix (CircuitPython)
No build step. The board is a **Raspberry Pi Pico 2 W** (RP2350) on CircuitPython 9.2.7 with `adafruit_httpserver` 4.6.2 in `lib/`; matrix clock pin is `GP11`.

- **Over the air** (normal way): `PICO_WEB_PASSWORD=... Matrix/deploy.sh` uploads `main.py` through CircuitPython's web workflow on port 8080 (configured in the device's `settings.toml`: `CIRCUITPY_WIFI_SSID`/`_PASSWORD`, `CIRCUITPY_WEB_API_PORT = 8080`, `CIRCUITPY_WEB_API_PASSWORD`), CircuitPython auto-reloads, and the script waits for `GET /status`. The password is not in this public repo. Writes are refused (409) while the `CIRCUITPY` drive is mounted over USB.
- **Over USB** (first setup / recovery): copy `Matrix/main.py`, a `secrets.py` (from `Matrix/secrets.py.example`) and `settings.toml` to the `CIRCUITPY` drive. It auto-runs `main.py` (as long as there's no `code.py`, which would take precedence) and prints its IP to the serial console.

## Key Conventions

- **32×32 strictly required** — both the Pico and `PixelSenderService` validate dimensions and reject other sizes.
- **Pixel format** — each pixel is 6 hex chars (`RRGGBB`); total payload is always 6,144 chars.
- **Transparency** — pixels with alpha < 50 are treated as black (`000000`), not skipped.
- **Images live in `MatrixClient/wwwroot/data/<category>/`** — `GalleryService` lists them (image extensions only, hidden files skipped) and confines send paths to that folder; no DB or upload UI yet.
- **Dynamic modes** (clock, calendar, weather) are registered in one place, `DynamicImageService`; the gallery, auto-send and validation all read from it.
- **Schedule rules** use `[from, to)` windows with `24:00` for end of day; where rules overlap, the one listed first wins (shown on the Schedule page and logged at startup). A rule has either `category` or `categories` (a mix; each send picks one at random), and an optional season `from_date`/`to_date` (`MM-dd`, inclusive, may wrap past new year) outside of which it is skipped — seasonal rules go first in `rules.yaml`. The pseudo-category `season` resolves to the current meteorological season's folder (`spring`/`summer`/`fall`/`winter`, by month; `AutoSendService.SeasonFor`), so year-round mixes include it instead of being copied per season.
- **Time zone** — clock, calendar and schedule use `TimeZone` from `appsettings.json` (`Europe/Vienna`) via an injected `TimeProvider`, not the host TZ; never use `DateTime.Now` directly. The Dockerfile copies tzdata into the chiseled image for this.
- **`rules.yaml` is validated at startup** (`AutoSendConfig.Validate`); a bad rule stops the app with a list of errors. `enabled_on_startup` sets the auto-send state after a restart.
- **The matrix is regularly powered off** (presence-switched socket) and boots to a black screen. `PicoStatus` tracks online/offline; auto-send skips sends while offline, and `AutoSendBackgroundService` probes the Pico every `Pico:ProbeSeconds` (60) with `GET /status` (`HttpPicoProbe`, timeout `Pico:ProbeTimeoutSeconds`, 5) and, when it comes back, re-sends the active rule's content or the last thing shown. Any HTTP answer counts as reachable (old firmware 404s `/status`); a bare TCP connect isn't enough, since a wedged server still accepted connections. Don't probe with `GET /` — it fills the display with a random colour. The nav shows the state (`Display/Status`); manual sends always try the matrix regardless of state, only auto-send pauses. Clicking the nav indicator opens a "Last send" panel: `PicoStatus.LastAttempt` (recorded by `PixelSenderService` for every POST, successful or not: time, content, the Pico's status line and body or why there was none, duration) comes with `Display/Status`, and `Display/LastSent` renders that attempt's payload back to a 32×32 PNG (`PixelSenderService.FromHex`), i.e. exactly what the matrix was asked to show.
- **The Pico recovers by itself**: `main.py` arms the hardware watchdog (8 s, fed once per poll loop) after WiFi/server start, and resets after 10 consecutive `server.poll()` errors or a WiFi drop. Reason: a send aborted mid-transfer by the client used to wedge the HTTP server (still accepting TCP, so the probe saw it as online, but resetting every POST) until the next power cycle. The cluster deployment also raises `Pico__TimeoutSeconds` to 20, since the first image after a power-up can take >5 s.
- **`GET /status` on the Pico** is side-effect-free JSON (uptime, free memory, request/error counters, last error, `reset_reason`, and the last `restart()` reason, kept across resets in `microcontroller.nvm`). The app's probe uses it; old firmware answers it with 404, which still means the server is responding. The POST handler decodes the hex with `binascii.unhexlify` (one allocation, also rejects non-hex with 400) and runs `gc.collect()` after each image; WiFi power management is switched off where the CircuitPython build allows it (idle power saving made the first connect after ~2 min take >5 s).
- **Pico URL** is configured under `Pico:Url` in `MatrixClient/appsettings.json` (override with env var `Pico__Url`), with `Pico:TimeoutSeconds` for the send timeout.
- **No `secrets.py` in source control** — create it from `Matrix/secrets.py.example` before deploying to the Pico.

## Dependencies

- **MatrixClient**: `SixLabors.ImageSharp 3.1.11` for image decoding/pixel access, `YamlDotNet` for `rules.yaml` (declared in `MatrixClient/MatrixClient.csproj`).
- **Matrix**: `adafruit_httpserver`, `rgbmatrix` (CircuitPython libraries; installed on the device, not via pip).
