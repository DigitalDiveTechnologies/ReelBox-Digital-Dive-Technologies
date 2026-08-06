"""Export the in-app SplashLogo brand mark (gradient disc + play) to a PNG master."""
from PIL import Image, ImageDraw

SIZE = 1024
# AppColors.brandOrangeDeep / brandPurpleDeep — AppGradients.brandMark
ORANGE = (255, 92, 51, 255)  # 0xFFFF5C33
PURPLE = (142, 45, 226, 255)  # 0xFF8E2DE2
WHITE = (255, 255, 255, 255)

img = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
px = img.load()

cx = cy = SIZE / 2
radius = SIZE / 2 - 2

for y in range(SIZE):
    for x in range(SIZE):
        dx = x + 0.5 - cx
        dy = y + 0.5 - cy
        if dx * dx + dy * dy > radius * radius:
            continue
        t = ((x / (SIZE - 1)) + (y / (SIZE - 1))) / 2.0
        t = max(0.0, min(1.0, t))
        rch = int(ORANGE[0] + (PURPLE[0] - ORANGE[0]) * t)
        gch = int(ORANGE[1] + (PURPLE[1] - ORANGE[1]) * t)
        bch = int(ORANGE[2] + (PURPLE[2] - ORANGE[2]) * t)
        px[x, y] = (rch, gch, bch, 255)

# Play triangle — Icons.play_arrow_rounded with slight left optical padding (SplashLogo).
play_h = SIZE * 0.42
play_w = SIZE * 0.36
ox = SIZE * 0.03
left = cx - play_w * 0.35 + ox
right = left + play_w
top = cy - play_h / 2
bottom = cy + play_h / 2
triangle = [(left, top), (right, cy), (left, bottom)]

overlay = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
ImageDraw.Draw(overlay).polygon(triangle, fill=WHITE)
img = Image.alpha_composite(img, overlay)

out = r"c:\Users\Star Laptop\Desktop\Social-Reel-Saver\mobile\assets\images\reelbox_logo.png"
img.save(out, "PNG")
print(f"wrote {out} {img.size}")
