#!/usr/bin/env bash
# Uploads main.py to the Pico over WiFi (CircuitPython web workflow, port 8080). CircuitPython
# auto-reloads after the write; the script then waits for the matrix server's GET /status.
#
#   PICO_WEB_PASSWORD=... Matrix/deploy.sh [file ...]     (default: main.py)
#
# The password is CIRCUITPY_WEB_API_PASSWORD from the Pico's settings.toml (kept out of this
# public repo). Writes fail with 409 while the CIRCUITPY drive is mounted over USB.
set -euo pipefail

host="${PICO_HOST:-192.168.1.225}"
: "${PICO_WEB_PASSWORD:?set PICO_WEB_PASSWORD}"
cd "$(dirname "$0")"
files=("$@")
[ ${#files[@]} -eq 0 ] && files=(main.py)

for f in "${files[@]}"; do
    code=$(curl -s -m 20 -u ":$PICO_WEB_PASSWORD" -o /dev/null -w '%{http_code}' -T "$f" "http://$host:8080/fs/$(basename "$f")")
    case "$code" in
        201|204) echo "uploaded $f" ;;
        409)     echo "upload of $f refused (409): CIRCUITPY is mounted over USB" >&2; exit 1 ;;
        *)       echo "upload of $f failed: HTTP $code" >&2; exit 1 ;;
    esac
done

# Give the reload a moment to stop the old server, then wait for the new one
sleep 3
for _ in $(seq 1 30); do
    if status=$(curl -s -m 3 "http://$host/status"); then
        echo "matrix is back: $status"
        exit 0
    fi
    sleep 1
done
echo "matrix did not answer GET /status within ~30 s; check the serial console" >&2
exit 1
