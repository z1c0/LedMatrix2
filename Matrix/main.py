import displayio
import board
import rgbmatrix
import ipaddress
import wifi
import socketpool
import random
import time
import microcontroller
from microcontroller import watchdog
from watchdog import WatchDogMode
from adafruit_httpserver import Server, Request, Response, POST
import secrets


MATRIX_DIM = 32
PAYLOAD_LEN = MATRIX_DIM * MATRIX_DIM * 6

# The RP2040 watchdog maxes out at ~8.3 s. It's fed once per loop iteration, so it only fires
# if the loop itself hangs (e.g. stuck in a request after the client gave up mid-transfer).
WATCHDOG_SECONDS = 8
# server.poll() raising this many times in a row means the server is wedged, not one bad request.
MAX_CONSECUTIVE_ERRORS = 10

displayio.release_displays()

# Create an instance of the RGBMatrix object
matrix = rgbmatrix.RGBMatrix(
    width=MATRIX_DIM, height=MATRIX_DIM, bit_depth=6,
    rgb_pins=[board.GP0, board.GP1, board.GP2, board.GP3, board.GP4, board.GP5],
    addr_pins=[board.GP6, board.GP7, board.GP8, board.GP9],
    clock_pin=board.GP16, latch_pin=board.GP12, output_enable_pin=board.GP13)
framebuffer = memoryview(matrix)

def restart(reason):
    print("restarting:", reason)
    time.sleep(1)
    microcontroller.reset()

def fill(col):
    for x in range(MATRIX_DIM):
        for y in range(MATRIX_DIM):
            # Convert the RGB color to RGB565 format (16-bit)
            color565 = (col[0] & 0xF8) << 8 | (col[1] & 0xFC) << 3 | col[2] >> 3
            framebuffer[y * MATRIX_DIM + x] = color565
    matrix.refresh()

def fill_pixels(pixels_text):
    for x in range(MATRIX_DIM):
        for y in range(MATRIX_DIM):
            start_index = (y * MATRIX_DIM + x) * 6

            hex_color = pixels_text[start_index:start_index+6]

            # Convert hex to integers for each color component
            r = int(hex_color[0:2], 16)
            g = int(hex_color[2:4], 16)
            b = int(hex_color[4:6], 16)

            color565 = (r & 0xF8) << 8 | (g & 0xFC) << 3 | b >> 3
            framebuffer[y * MATRIX_DIM + x] = color565
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
pool = socketpool.SocketPool(wifi.radio)
print("IP address: ", wifi.radio.ipv4_address)
server = Server(pool, "/static", debug=True)
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

@server.route("/", POST)
def base(request: Request):
    pixels_text = request.body.decode('utf-8')
    if len(pixels_text) != PAYLOAD_LEN:
        return Response(request, webpage(f"expected {PAYLOAD_LEN} chars, got {len(pixels_text)}"),
                        content_type='application/json', status=(400, "Bad Request"))
    fill_pixels(pixels_text)
    return Response(request, webpage(), content_type='application/json')

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
        consecutive_errors += 1
        if consecutive_errors >= MAX_CONSECUTIVE_ERRORS:
            restart(f"{consecutive_errors} errors in a row")
