import displayio
import board
import rgbmatrix
import ipaddress
import wifi
import socketpool
import random
import time
import gc
import binascii
import microcontroller
from microcontroller import watchdog
from watchdog import WatchDogMode
from adafruit_httpserver import Server, Request, Response, JSONResponse, POST, BAD_REQUEST_400
import secrets


MATRIX_DIM = 32
PAYLOAD_LEN = MATRIX_DIM * MATRIX_DIM * 6

# The RP2040 watchdog maxes out at ~8.3 s. It's fed once per loop iteration, so it only fires
# if the loop itself hangs (e.g. stuck in a request after the client gave up mid-transfer).
WATCHDOG_SECONDS = 8
# server.poll() raising this many times in a row means the server is wedged, not one bad request.
MAX_CONSECUTIVE_ERRORS = 10
# The server stops reading a request after this long without new data (library default: 1 s).
# Over the office WiFi a retransmitted segment can take longer, which cut the 6,144-byte body
# off at 1460/2920/4380 bytes (2026-10-13 test). Must stay well below WATCHDOG_SECONDS.
SOCKET_TIMEOUT_SECONDS = 3

# Last restart() reason survives the reset in nvm; shown by GET /status. A watchdog reset
# doesn't write it, but then cpu.reset_reason is WATCHDOG. The prefix tells it apart from
# whatever was in nvm before (it isn't erased on a fresh board).
NVM_MAGIC = b"RR:"
NVM_REASON_LEN = 64

started = time.monotonic()
requests_ok = 0
requests_bad = 0
last_error = ""

def read_restart_reason():
    raw = bytes(microcontroller.nvm[0:NVM_REASON_LEN])
    if not raw.startswith(NVM_MAGIC):
        return ""
    return raw[len(NVM_MAGIC):].rstrip(b"\x00").decode("utf-8", "replace")

print("reset reason:", microcontroller.cpu.reset_reason, "| last restart():", read_restart_reason())

displayio.release_displays()

# Create an instance of the RGBMatrix object
matrix = rgbmatrix.RGBMatrix(
    width=MATRIX_DIM, height=MATRIX_DIM, bit_depth=6,
    rgb_pins=[board.GP0, board.GP1, board.GP2, board.GP3, board.GP4, board.GP5],
    addr_pins=[board.GP6, board.GP7, board.GP8, board.GP9],
    clock_pin=board.GP11, latch_pin=board.GP12, output_enable_pin=board.GP13)
framebuffer = memoryview(matrix)

def restart(reason):
    print("restarting:", reason)
    data = (NVM_MAGIC + reason.encode("utf-8"))[:NVM_REASON_LEN]
    microcontroller.nvm[0:NVM_REASON_LEN] = data + b"\x00" * (NVM_REASON_LEN - len(data))
    time.sleep(1)
    microcontroller.reset()

def fill(col):
    for x in range(MATRIX_DIM):
        for y in range(MATRIX_DIM):
            # Convert the RGB color to RGB565 format (16-bit)
            color565 = (col[0] & 0xF8) << 8 | (col[1] & 0xFC) << 3 | col[2] >> 3
            framebuffer[y * MATRIX_DIM + x] = color565
    matrix.refresh()

# rgb: 3 bytes per pixel, row-major. Decoding the hex in one unhexlify call instead of
# int(slice, 16) per channel avoids ~3,000 small allocations per image (heap fragmentation).
def fill_pixels(rgb):
    for i in range(MATRIX_DIM * MATRIX_DIM):
        r = rgb[3 * i]
        g = rgb[3 * i + 1]
        b = rgb[3 * i + 2]
        framebuffer[i] = (r & 0xF8) << 8 | (g & 0xFC) << 3 | b >> 3
    matrix.refresh()

def webpage(result="OK"):
    return f'{{ "result" : "{result}" }}'

fill((0, 0, 0))
print("Connecting to WiFi")
try:
    wifi.radio.connect(secrets.WIFI_SSID, secrets.WIFI_PASSWORD)
except ConnectionError as e:
    restart(f"WiFi connect failed: {e}")
print("Connected to WiFi")
# WiFi power saving made the first connect after ~2 min idle take >5 s (2026-10-13 test).
# Only on CircuitPython builds that expose it.
try:
    wifi.radio.power_management = wifi.PowerManagement.NONE
    print("WiFi power management off")
except AttributeError:
    print("WiFi power management not configurable on this build")
pool = socketpool.SocketPool(wifi.radio)
print("IP address: ", wifi.radio.ipv4_address)
server = Server(pool, "/static", debug=True)
server.socket_timeout = SOCKET_TIMEOUT_SECONDS
print("starting server..")
try:
    server.start(str(wifi.radio.ipv4_address), 80)
    print("Listening on http://%s:80" % wifi.radio.ipv4_address)
    #  if the server fails to begin, restart the pico w
except OSError:
    time.sleep(5)
    restart("server failed to start")

@server.route("/")
def base(request: Request):
    r = random.randint(0, 255)
    g = random.randint(0, 255)
    b = random.randint(0, 255)
    fill((r, g, b))
    return Response(request, webpage(), content_type='application/json')

def bad_request(request, message):
    global requests_bad, last_error
    requests_bad += 1
    last_error = message
    return Response(request, webpage(message), content_type='application/json', status=BAD_REQUEST_400)

@server.route("/", POST)
def base(request: Request):
    global requests_ok
    body = request.body
    if len(body) != PAYLOAD_LEN:
        return bad_request(request, f"expected {PAYLOAD_LEN} chars, got {len(body)}")
    try:
        rgb = binascii.unhexlify(body)
    except ValueError:
        return bad_request(request, "payload is not hex")
    fill_pixels(rgb)
    rgb = None
    gc.collect()
    requests_ok += 1
    return Response(request, webpage(), content_type='application/json')

# ap_info raises NotImplementedError on CircuitPython 9.2.7 (Pico 2 W); then RSSI is null.
def wifi_rssi():
    try:
        return wifi.radio.ap_info.rssi
    except (NotImplementedError, AttributeError):
        return None

# Side-effect-free health check for the client's probe (GET / paints the display).
@server.route("/status")
def status(request: Request):
    return JSONResponse(request, {
        "result": "OK",
        "uptime_s": int(time.monotonic() - started),
        "mem_free": gc.mem_free(),
        "rssi": wifi_rssi(),
        "requests_ok": requests_ok,
        "requests_bad": requests_bad,
        "poll_errors": consecutive_errors,
        "last_error": last_error,
        "reset_reason": str(microcontroller.cpu.reset_reason),
        "last_restart": read_restart_reason(),
    })

# Only now: WiFi connect and server start can take longer than the watchdog allows.
watchdog.timeout = WATCHDOG_SECONDS
watchdog.mode = WatchDogMode.RESET

consecutive_errors = 0
while True:
    watchdog.feed()
    if not wifi.radio.connected:
        restart("WiFi lost")
    try:
        # poll the server for incoming/outgoing requests
        server.poll()
        consecutive_errors = 0
    except Exception as e:
        print(e)
        last_error = f"poll: {e}"[:80]
        consecutive_errors += 1
        if consecutive_errors >= MAX_CONSECUTIVE_ERRORS:
            restart(f"{consecutive_errors} errors in a row")
