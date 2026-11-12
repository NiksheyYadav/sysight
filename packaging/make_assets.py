"""Generates the Sysight icon (app.ico) and MSIX tile assets. Run: python packaging/make_assets.py"""
import math, os
from PIL import Image, ImageDraw

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "packaging", "Assets")
os.makedirs(OUT, exist_ok=True)
STOPS = [(0.0, (245, 185, 66)), (0.33, (255, 122, 69)), (0.66, (106, 167, 255)), (1.0, (245, 185, 66))]

def grad(t):
    for (a, ca), (b, cb) in zip(STOPS, STOPS[1:]):
        if a <= t <= b:
            f = (t - a) / (b - a)
            return tuple(int(ca[i] + (cb[i] - ca[i]) * f) for i in range(3))
    return STOPS[-1][1]

def mark(size, plate=True):
    """Lens mark: conic-gradient ring with a bright pupil, optionally on a dark rounded plate."""
    S = size * 4
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    if plate:
        ImageDraw.Draw(img).rounded_rectangle([0, 0, S - 1, S - 1], radius=int(S * 0.22), fill=(18, 21, 29, 255))
    c = S / 2; ro = S * (0.36 if plate else 0.47); ri = ro * 0.62
    px = img.load()
    for y in range(S):
        for x in range(S):
            dx, dy = x - c, y - c; d = math.hypot(dx, dy)
            if ri <= d <= ro:
                t = (math.atan2(dy, dx) + math.pi) / (2 * math.pi)
                px[x, y] = grad((t + 0.55) % 1.0) + (255,)
            elif d < ri * 0.42:
                px[x, y] = (236, 240, 248, 255)
    return img.resize((size, size), Image.LANCZOS)

def tile(w, h, mark_frac=0.62):
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    m = mark(int(min(w, h) * mark_frac), plate=False)
    img.alpha_composite(m, ((w - m.width) // 2, (h - m.height) // 2))
    return img

for scale in (100, 200):
    f = scale / 100
    tile(int(150 * f), int(150 * f)).save(os.path.join(OUT, f"Square150x150Logo.scale-{scale}.png"))
    tile(int(310 * f), int(150 * f)).save(os.path.join(OUT, f"Wide310x150Logo.scale-{scale}.png"))
    tile(int(44 * f), int(44 * f), 0.9).save(os.path.join(OUT, f"Square44x44Logo.scale-{scale}.png"))
    mark(int(50 * f)).save(os.path.join(OUT, f"StoreLogo.scale-{scale}.png"))
for t in (16, 24, 32, 48, 256):
    m = tile(t, t, 0.94)
    m.save(os.path.join(OUT, f"Square44x44Logo.targetsize-{t}.png"))
    m.save(os.path.join(OUT, f"Square44x44Logo.targetsize-{t}_altform-unplated.png"))
# Plain names are what the manifest references; makeappx needs them without a resources.pri.
import shutil
for n in ("Square150x150Logo", "Wide310x150Logo", "Square44x44Logo", "StoreLogo"):
    shutil.copy(os.path.join(OUT, f"{n}.scale-200.png"), os.path.join(OUT, f"{n}.png"))
big = mark(256)
big.save(os.path.join(ROOT, "src", "Sysight", "app.ico"), sizes=[(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)])
mark(300).save(os.path.join(ROOT, "docs", "store-logo-300.png"))
print("assets written to", OUT)
