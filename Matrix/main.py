import displayio
import board
import rgbmatrix
import ipaddress
import wifi
import socketpool
import random
from adafruit_httpserver import Server, Request, Response, POST


MATRIX_DIM = 32

displayio.release_displays()

# Create an instance of the RGBMatrix object
matrix = rgbmatrix.RGBMatrix(
    width=MATRIX_DIM, height=MATRIX_DIM, bit_depth=6,
    rgb_pins=[board.GP0, board.GP1, board.GP2, board.GP3, board.GP4, board.GP5],
    addr_pins=[board.GP6, board.GP7, board.GP8, board.GP9],
    clock_pin=board.GP16, latch_pin=board.GP12, output_enable_pin=board.GP13)
framebuffer = memoryview(matrix)

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

def webpage():
    json = f"""
    { "result" : "OK" }
    """

fill((0, 0, 0))
print("Connecting to WiFi")
wifi.radio.connect('<ssid>', '<password>')
print("Connected to WiFi")
pool = socketpool.SocketPool(wifi.radio)
print("IP address: ", wifi.radio.ipv4_address)
pool = socketpool.SocketPool(wifi.radio)
server = Server(pool, "/static", debug=True)
print("starting server..")
try:
    server.start(str(wifi.radio.ipv4_address), 80)
    print("Listening on http://%s:80" % wifi.radio.ipv4_address)
    #  if the server fails to begin, restart the pico w
except OSError:
    time.sleep(5)
    print("restarting ...")
    microcontroller.reset()

@server.route("/")
def base(request: Request):
    r = random.randint(0, 256)
    g = random.randint(0, 256)
    b = random.randint(0, 256)
    fill((r, g, b))    
    return Response(request, f"{webpage()}", content_type='application/json')

@server.route("/", POST)
def base(request: Request):
    fill_pixels(request.body.decode('utf-8'))
    return Response(request, f"{webpage()}", content_type='application/json')

while True:
    try:
        # poll the server for incoming/outgoing requests
        server.poll()
    except Exception as e:
        print(e)
        continue