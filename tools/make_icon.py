# -*- coding: utf-8 -*-
"""
Laocoon 像素图标生成器

设计意图（16x16 母版，所有尺寸按最近邻整数倍放大，边缘永远不模糊）：

    深蓝贴片   —— 2px 像素切角的底，任何桌面背景上都能立住
    四角括号   —— 瞄准镜的四个角，正卡住一个东西的边缘
    小窗口     —— 被卡住的那个窗口：标题栏 + 窗身 + 一道暗底边
    红点       —— 全图唯一的高饱和色，视线落点："这窗口有点不对劲"

整个图标只有 6 个颜色，16 格对齐，不用任何抗锯齿。

用法：python tools/make_icon.py
产出：src/Laocoon.ico（8 个尺寸）与 .tmp/icon_sheet.png（预览对照图）
"""

import io
import os
import struct

from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)

# ---------- 调色板 ----------
CLEAR = (0, 0, 0, 0)
BG = (23, 35, 59, 255)        # #17233B 贴片底
MINT = (72, 226, 192, 255)    # #48E2C0 准星括号
TITLE = (110, 134, 168, 255)  # #6E86A8 窗口标题栏
BODY = (233, 241, 251, 255)   # #E9F1FB 窗口窗身
RED = (255, 92, 92, 255)      # #FF5C5C 可疑红点
EDGE = (44, 61, 89, 255)      # #2C3D59 窗底暗边

G = 16  # 母版网格边长

grid = [[CLEAR] * G for _ in range(G)]


def put(x, y, c):
    if 0 <= x < G and 0 <= y < G:
        grid[y][x] = c


def rect(x0, y0, x1, y1, c):
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            put(x, y, c)


# ---------- 1) 底：2px 像素切角 ----------
INSET = {0: 2, 1: 1, G - 2: 1, G - 1: 2}
for y in range(G):
    i = INSET.get(y, 0)
    rect(i, y, G - 1 - i, y, BG)

# ---------- 2) 四角瞄准括号（臂长 4，与窗口留 2px 呼吸） ----------
ARM = 4
for cx, cy, dx, dy in ((2, 2, 1, 1), (G - 3, 2, -1, 1),
                       (2, G - 3, 1, -1), (G - 3, G - 3, -1, -1)):
    for k in range(ARM):
        put(cx + dx * k, cy, MINT)   # 横臂
        put(cx, cy + dy * k, MINT)   # 竖臂

# ---------- 3) 被框住的窗口（6x6，居中，四周留 2px 暗色呼吸） ----------
rect(5, 5, 10, 5, TITLE)      # 标题栏
rect(5, 6, 10, 9, BODY)       # 窗身
rect(5, 10, 10, 10, EDGE)     # 底边暗线，给一点厚度
rect(7, 7, 8, 8, RED)         # 可疑红点


def render(size):
    """按最近邻放大到目标尺寸，绝不插值。"""
    img = Image.new("RGBA", (size, size), CLEAR)
    px = img.load()
    for gy in range(G):
        y0 = round(gy * size / G)
        y1 = round((gy + 1) * size / G)
        for gx in range(G):
            c = grid[gy][gx]
            if c[3] == 0:
                continue
            x0 = round(gx * size / G)
            x1 = round((gx + 1) * size / G)
            for y in range(y0, y1):
                for x in range(x0, x1):
                    px[x, y] = c
    return img


def dib(im):
    """32 位 BMP 条目：小尺寸用 DIB 最稳，老 shell 也认。"""
    s = im.size[0]
    px = im.load()
    xor = bytearray()
    for y in range(s - 1, -1, -1):        # BMP 是自下而上
        for x in range(s):
            r, g, b, a = px[x, y]
            xor += struct.pack("<BBBB", b, g, r, a)
    stride = ((s + 31) // 32) * 4         # AND 掩码每行 4 字节对齐
    mask = b"\x00" * (stride * s)         # 全 0 = 全部不透明
    hdr = struct.pack("<IiiHHIIiiII", 40, s, s * 2, 1, 32, 0,
                      len(xor) + len(mask), 0, 0, 0, 0)
    return hdr + bytes(xor) + mask


def png(im):
    b = io.BytesIO()
    im.save(b, "PNG", optimize=True)
    return b.getvalue()


def write_ico(path, sizes):
    # 48 及以上用 PNG 压缩（Vista 起支持），小尺寸仍用 DIB —— 老 shell 对小图标更认位图
    blobs = [(s, png(im) if s >= 48 else dib(im))
             for s, im in ((s, render(s)) for s in sizes)]
    head = struct.pack("<HHH", 0, 1, len(blobs))
    off = 6 + 16 * len(blobs)
    entries, body = b"", b""
    for s, data in blobs:
        w = 0 if s >= 256 else s
        entries += struct.pack("<BBBBHHII", w, w, 0, 0, 1, 32, len(data),
                               off + len(body))
        body += data
    with open(path, "wb") as f:
        f.write(head + entries + body)


def sheet(path):
    """预览：亮底、暗底各排一行，从 16 到 256 一次看全。"""
    sizes = [16, 20, 24, 32, 48, 64, 128, 256]
    pad, gap, band = 24, 26, 320
    w = pad * 2 + sum(sizes) + gap * (len(sizes) - 1)
    img = Image.new("RGB", (w, band * 2), (243, 244, 246))
    d = ImageDraw.Draw(img)
    d.rectangle([0, band, w, band * 2], fill=(17, 19, 23))
    for i in range(2):
        top = i * band
        fg = (60, 66, 78) if i == 0 else (170, 178, 190)
        x = pad
        for s in sizes:
            im = render(s)
            img.paste(im, (x, top + (band - s) // 2 - 8), im)
            d.text((x + s // 2 - 6, top + band - 34), str(s), fill=fg)
            x += s + gap
    img.save(path)


os.makedirs(os.path.join(ROOT, ".tmp"), exist_ok=True)
ICO = os.path.join(ROOT, "src", "Laocoon.ico")
write_ico(ICO, [16, 20, 24, 32, 48, 64, 128, 256])
sheet(os.path.join(ROOT, ".tmp", "icon_sheet.png"))

# 控制台打一张 ASCII 网格，改设计时先在这里看一眼
CH = {CLEAR: ".", BG: "#", MINT: "B", TITLE: "T", BODY: "o", RED: "R", EDGE: "_"}
for row in grid:
    print("".join(CH[c] for c in row))
print("ico:", os.path.getsize(ICO), "bytes")
