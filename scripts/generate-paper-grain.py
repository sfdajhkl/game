#!/usr/bin/env python3
"""Generate the UI-only, seamless paper modulation texture (no illustration processing)."""
from pathlib import Path
import math
import random
import struct
import zlib

ROOT = Path(__file__).resolve().parents[1]
SIZE = 512

def hash_value(x, y, period):
    n = ((x % period) * 374761393 + (y % period) * 668265263 + 73) & 0xffffffff
    n = ((n ^ (n >> 13)) * 1274126177) & 0xffffffff
    return ((n ^ (n >> 16)) & 0xffff) / 65535

def noise(x, y, cell):
    x, y = x / cell, y / cell
    ix, iy = math.floor(x), math.floor(y)
    fx, fy = x - ix, y - iy
    fx, fy = fx * fx * (3 - 2 * fx), fy * fy * (3 - 2 * fy)
    n = SIZE // cell
    a = hash_value(ix, iy, n) * (1 - fx) + hash_value(ix + 1, iy, n) * fx
    b = hash_value(ix, iy + 1, n) * (1 - fx) + hash_value(ix + 1, iy + 1, n) * fx
    return a * (1 - fy) + b * fy - .5

def chunk(kind, data):
    return struct.pack('!I', len(data)) + kind + data + struct.pack('!I', zlib.crc32(kind + data) & 0xffffffff)

def main():
    rng = random.Random(4263)
    pixels = [max(230, min(255, round(248 + 18 * noise(x, y, 128) + 7 * noise(x, y, 32)
              + 4 * noise(x, y, 8) + rng.uniform(-1.8, 1.8)))) for y in range(SIZE) for x in range(SIZE)]
    # Short, low contrast fibers. No scratches, stains, or conspicuous flecks.
    for _ in range(2200):
        x, y, length = rng.randrange(SIZE), rng.randrange(SIZE), rng.randrange(2, 6)
        slant, delta = rng.choice([-1, 0, 1]), rng.choice([-4, -3, 3])
        for i in range(length):
            index = ((y + i // 3 * slant) % SIZE) * SIZE + (x + i) % SIZE
            pixels[index] = max(230, min(255, pixels[index] + delta))
    raw = b''.join(b'\0' + bytes(pixels[y * SIZE:(y + 1) * SIZE]) for y in range(SIZE))
    png = b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', struct.pack('!2I5B', SIZE, SIZE, 8, 0, 0, 0, 0)) + chunk(b'IDAT', zlib.compress(raw, 9)) + chunk(b'IEND', b'')
    for path in ['Unity/Arvito/Assets/Resources/Arvito/UI/paper-grain.png', 'Design/ParchmentEventUI/assets/paper-grain.png']:
        (ROOT / path).write_bytes(png)
    print('Paper texture:', SIZE, 'px, range', min(pixels), max(pixels), 'bytes', len(png))

if __name__ == '__main__':
    main()
