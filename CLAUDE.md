# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

LED pixel matrix controller. See [README.md](README.md) and the [blog post](https://wolfgang-ziegler.com/blog/remote-controlled-led-matrix) for background.

## Architecture

Two loosely coupled components communicating over HTTP:

| Component | Language / Runtime | Role |
|---|---|---|
| `Matrix/` | CircuitPython on Raspberry Pi Pico W | Hardware server — drives the 32×32 RGB LED matrix |
| `MatrixClient/` | C# ASP.NET Core MVC (.NET 10) | Web UI — selects images, converts pixels, POSTs to Pico |

**Data flow**: User selects a 32×32 PNG → `PixelController` converts it to a 6,144-char hex string (one 6-char RGB value per pixel) → HTTP POST to Pico W IP → `main.py` parses hex, converts to RGB565, writes to framebuffer.

## Build & Run

### MatrixClient (C#)
```bash
cd MatrixClient

# Run locally
dotnet run

# Build release
dotnet build -c Release

# Docker
docker build -f Dockerfile -t matrixclient .
docker run -p 8080:8080 matrixclient
```

CI (Woodpecker) builds via Kaniko and deploys with `kubectl rollout restart deployment/pixelmatrix -n pixelmatrix` on push to `main`.

### Matrix (CircuitPython)
No build step. Copy `Matrix/main.py` and a `secrets.py` (from `Matrix/secrets.py.example`) to the Pico W's filesystem. The device auto-runs `main.py` on boot and prints its IP to the serial console.

## Key Conventions

- **32×32 strictly required** — both the Pico and `PixelController` validate dimensions and reject other sizes.
- **Pixel format** — each pixel is 6 hex chars (`RRGGBB`); total payload is always 6,144 chars.
- **Transparency** — pixels with alpha < 50 are treated as black (`000000`), not skipped.
- **Images live in `MatrixClient/wwwroot/data/`** — `HomeController` reads that directory to populate the gallery; no DB or upload UI yet.
- **Pico IP is hardcoded** in `MatrixClient/Controllers/PixelController.cs` (`http://192.168.1.41/`). Update this constant when the Pico's IP changes.
- **No `secrets.py` in source control** — create it from `Matrix/secrets.py.example` before deploying to the Pico.

## Dependencies

- **MatrixClient**: `SixLabors.ImageSharp 3.1.11` for image decoding/pixel access (declared in `MatrixClient/MatrixClient.csproj`).
- **Matrix**: `adafruit_httpserver`, `rgbmatrix` (CircuitPython libraries; installed on the device, not via pip).
