"""Мини-библиотека для чёрно-белых схем в стиле draw.io.

Используется генератором пояснительной записки (tools/render_pz_diagrams.py).
Все фигуры рисуются в условных единицах (1 ед. ≈ 1 pt), затем масштабируются.

Дополнительно ведётся проверка пересечений: каждая линия регистрируется в
Canvas.segments, а метод check() сообщает о линиях, которые пересекаются между
собой или проходят через прямоугольники узлов. Это позволяет держать схемы
аккуратными без ручной вычитки.
"""
import math
import os

from PIL import Image, ImageDraw, ImageFont

FONT_DIR = '/usr/share/fonts/truetype/dejavu'
FONT = os.path.join(FONT_DIR, 'DejaVuSans.ttf')
FONT_BOLD = os.path.join(FONT_DIR, 'DejaVuSans-Bold.ttf')
FONT_SERIF = os.path.join(FONT_DIR, 'DejaVuSerif.ttf')
FONT_SERIF_BOLD = os.path.join(FONT_DIR, 'DejaVuSerif-Bold.ttf')

INK = '#000000'
LINE = '#000000'
GREY = '#d9d9d9'
LIGHT = '#f2f2f2'


class Canvas:
    def __init__(self, w, h, scale=2):
        self.s = scale
        self.w, self.h = w, h
        self.img = Image.new('RGB', (int(w * scale), int(h * scale)), 'white')
        self.d = ImageDraw.Draw(self.img)
        self._fonts = {}
        self.segments = []      # (owner, (x1, y1), (x2, y2))
        self.nodes = []         # (owner, x, y, w, h)
        self._owner = 'diagram'
        self._node_id = 0
        self._edge_id = 0
        self.issues = []

    # ---------------------------------------------------------------- текст
    def font(self, size, bold=False):
        key = (size, bold)
        if key not in self._fonts:
            self._fonts[key] = ImageFont.truetype(FONT_BOLD if bold else FONT, int(round(size * self.s)))
        return self._fonts[key]

    def tw(self, s, size=13, bold=False):
        return self.d.textlength(s, font=self.font(size, bold)) / self.s

    def text(self, x, y, s, size=13, bold=False, anchor='mm', fill=INK, italic=False):
        self.d.text((x * self.s, y * self.s), s, font=self.font(size, bold), fill=fill, anchor=anchor)

    def wrap(self, s, maxw, size=13, bold=False):
        words = s.split()
        lines, cur = [], ''
        for word in words:
            probe = word if not cur else cur + ' ' + word
            if self.tw(probe, size, bold) <= maxw or not cur:
                cur = probe
            else:
                lines.append(cur)
                cur = word
        if cur:
            lines.append(cur)
        return lines

    def block(self, cx, cy, s, maxw, size=13, bold=False, gap=4, align='center', fill=INK):
        lines = []
        for part in s.split('\n'):
            lines.extend(self.wrap(part, maxw, size, bold))
        lh = size + gap
        total = lh * (len(lines) - 1)
        for i, line in enumerate(lines):
            y = cy - total / 2 + i * lh
            if align == 'center':
                self.text(cx, y, line, size=size, bold=bold, fill=fill)
            else:
                self.text(cx - maxw / 2, y, line, size=size, bold=bold, anchor='lm', fill=fill)
        return total + size

    # ---------------------------------------------------------------- фигуры
    def _owner_ctx(self, owner):
        self._owner = owner

    def rect(self, x, y, w, h, fill='white', outline=LINE, width=1.5, dash=False, radius=0, node=True):
        """Прямоугольник по левому верхнему углу."""
        box = [x * self.s, y * self.s, (x + w) * self.s, (y + h) * self.s]
        if dash:
            self.d.rectangle(box, fill=fill)
            pts = [(x, y), (x + w, y), (x + w, y + h), (x, y + h), (x, y)]
            for i in range(4):
                self._dashed(pts[i], pts[i + 1], outline, width)
        else:
            self.d.rounded_rectangle(box, radius=radius * self.s, fill=fill, outline=outline,
                                     width=max(1, int(round(width * self.s))))
        if node:
            self._node_id += 1
            self.nodes.append((f'{self._owner}:n{self._node_id}', x, y, w, h))
        return (x, y, w, h)

    def shape(self, kind, cx, cy, w, h, text, size=13, bold=False, pad=12, node=True):
        """Фигура по центру: 'rect', 'round', 'term', 'diamond', 'parallelogram', 'hex'."""
        x, y = cx - w / 2, cy - h / 2
        if kind == 'rect':
            self.rect(x, y, w, h, radius=0, node=node)
        elif kind == 'round':
            self.rect(x, y, w, h, radius=10, node=node)
        elif kind == 'term':
            self.rect(x, y, w, h, radius=h / 2, node=node)
        elif kind == 'hex':
            pts = [(x + 14, y), (x + w - 14, y), (x + w, cy), (x + w - 14, y + h), (x + 14, y + h), (x, cy)]
            self.d.polygon([(px * self.s, py * self.s) for px, py in pts], fill='white',
                           outline=LINE, width=max(1, int(round(1.5 * self.s))))
            if node:
                self._node_id += 1
                self.nodes.append((f'{self._owner}:n{self._node_id}', x + 14, y, w - 28, h))
        elif kind == 'diamond':
            pts = [(cx, y), (x + w, cy), (cx, y + h), (x, cy)]
            self.d.polygon([(px * self.s, py * self.s) for px, py in pts], fill='white',
                           outline=LINE, width=max(1, int(round(1.5 * self.s))))
            if node:
                self._node_id += 1
                self.nodes.append((f'{self._owner}:n{self._node_id}', x + w * 0.25, cy - h * 0.25, w * 0.5, h * 0.5))
        elif kind == 'parallelogram':
            k = 16
            pts = [(x + k, y), (x + w, y), (x + w - k, y + h), (x, y + h)]
            self.d.polygon([(px * self.s, py * self.s) for px, py in pts], fill='white',
                           outline=LINE, width=max(1, int(round(1.5 * self.s))))
            if node:
                self._node_id += 1
                self.nodes.append((f'{self._owner}:n{self._node_id}', x + k, y, w - k, h))
        if text:
            self.block(cx, cy, text, w - 2 * pad, size=size, bold=bold)

    def ellipse(self, cx, cy, rx, ry, text='', size=12, bold=False, fill='white', dash=False):
        box = [(cx - rx) * self.s, (cy - ry) * self.s, (cx + rx) * self.s, (cy + ry) * self.s]
        self.d.ellipse(box, fill=fill, outline=LINE, width=max(1, int(round(1.5 * self.s))))
        self._node_id += 1
        self.nodes.append((f'{self._owner}:n{self._node_id}', cx - rx * 0.72, cy - ry * 0.72, rx * 1.44, ry * 1.44))
        if text:
            self.block(cx, cy, text, rx * 1.55, size=size, bold=bold)

    def circle(self, cx, cy, r, filled=True):
        box = [(cx - r) * self.s, (cy - r) * self.s, (cx + r) * self.s, (cy + r) * self.s]
        self.d.ellipse(box, fill=INK if filled else 'white', outline=INK,
                       width=max(1, int(round(1.5 * self.s))))
        self._node_id += 1
        self.nodes.append((f'{self._owner}:n{self._node_id}', cx - r, cy - r, 2 * r, 2 * r))

    def actor(self, cx, cy, name, size=12, label_side='right'):
        """Палка-фигурка актёра: (cx, cy) — центр головы."""
        r = 9
        self.d.ellipse([(cx - r) * self.s, (cy - r) * self.s, (cx + r) * self.s, (cy + r) * self.s],
                       outline=INK, fill='white', width=max(1, int(round(1.4 * self.s))))
        def limb(p, q):
            self.d.line([p[0] * self.s, p[1] * self.s, q[0] * self.s, q[1] * self.s],
                        fill=INK, width=max(1, int(round(1.4 * self.s))))
        limb((cx, cy + r), (cx, cy + 34))
        limb((cx - 14, cy + 18), (cx + 14, cy + 18))
        limb((cx, cy + 34), (cx - 12, cy + 54))
        limb((cx, cy + 34), (cx + 12, cy + 54))
        if label_side == 'right':
            self.text(cx + 24, cy + 20, name, size=size, anchor='lm', bold=True)
        elif label_side == 'below':
            self.block(cx, cy + 76, name, 150, size=size, bold=True)
        else:
            self.text(cx - 24, cy + 20, name, size=size, anchor='rm', bold=True)
        self._node_id += 1
        self.nodes.append((f'{self._owner}:n{self._node_id}', cx - 16, cy - 10, 32, 66))

    # ---------------------------------------------------------------- линии
    def _dashed(self, p, q, color=LINE, width=1.3, dash=7, gap=5):
        x1, y1 = p
        x2, y2 = q
        length = math.hypot(x2 - x1, y2 - y1)
        if length == 0:
            return
        ux, uy = (x2 - x1) / length, (y2 - y1) / length
        t = 0.0
        while t < length:
            t2 = min(t + dash, length)
            self.d.line([(x1 + ux * t) * self.s, (y1 + uy * t) * self.s,
                         (x1 + ux * t2) * self.s, (y1 + uy * t2) * self.s],
                        fill=color, width=max(1, int(round(width * self.s))))
            t = t2 + gap

    def line(self, p, q, width=1.4, dash=False):
        if dash:
            self._dashed(p, q, LINE, width)
        else:
            self.d.line([p[0] * self.s, p[1] * self.s, q[0] * self.s, q[1] * self.s],
                        fill=LINE, width=max(1, int(round(width * self.s))))
        self._edge_id += 1
        self.segments.append((f'{self._owner}:e{self._edge_id}', p, q))

    def head(self, p_from, p_to, size=9):
        ang = math.atan2(p_to[1] - p_from[1], p_to[0] - p_from[0])
        for sign in (0.42, -0.42):
            x = p_to[0] - size * math.cos(ang - sign)
            y = p_to[1] - size * math.sin(ang - sign)
            self.d.line([x * self.s, y * self.s, p_to[0] * self.s, p_to[1] * self.s],
                        fill=INK, width=max(1, int(round(1.4 * self.s))))

    def hollow_head(self, p_from, p_to, size=11):
        """Полая треугольная стрелка — обобщение (наследование)."""
        ang = math.atan2(p_to[1] - p_from[1], p_to[0] - p_from[0])
        left = (p_to[0] - size * math.cos(ang - 0.38), p_to[1] - size * math.sin(ang - 0.38))
        right = (p_to[0] - size * math.cos(ang + 0.38), p_to[1] - size * math.sin(ang + 0.38))
        self.d.polygon([(p_to[0] * self.s, p_to[1] * self.s), (left[0] * self.s, left[1] * self.s),
                        (right[0] * self.s, right[1] * self.s)], fill='white', outline=INK,
                       width=max(1, int(round(1.3 * self.s))))

    def path(self, points, arrow=True, dash=False, width=1.4, head='open'):
        for i in range(len(points) - 1):
            self.line(points[i], points[i + 1], width=width, dash=dash)
        if arrow:
            if head == 'hollow':
                self.hollow_head(points[-2], points[-1])
            else:
                self.head(points[-2], points[-1])

    def arrow(self, p, q, arrow=True, dash=False, width=1.4, head='open'):
        self.path([p, q], arrow=arrow, dash=dash, width=width, head=head)

    def label(self, x, y, s, size=11, bold=False, pad=2):
        w = self.tw(s, size, bold)
        self.d.rectangle([(x - w / 2 - pad) * self.s, (y - size / 2 - pad) * self.s,
                          (x + w / 2 + pad) * self.s, (y + size / 2 + pad) * self.s], fill='white')
        self.text(x, y, s, size=size, bold=bold)

    def mult_labels(self, points, m_from, m_to):
        """Кратности связи у концов линии: снаружи блоков, вдоль линии."""
        self._mult_at(points[0], points[1], m_from)
        self._mult_at(points[-1], points[-2], m_to)

    def _mult_at(self, p, q, text):
        dx, dy = q[0] - p[0], q[1] - p[1]
        ln = math.hypot(dx, dy) or 1.0
        ux, uy = dx / ln, dy / ln
        x = p[0] + ux * 22 - uy * 11
        y = p[1] + uy * 22 + ux * 11
        self.text(x, y, text, size=11, bold=True, fill=INK)

    def mult(self, x, y, s, size=12):
        """Кратность связи (1, N) без подложки."""
        self.text(x, y, s, size=size, bold=True, fill=INK)

    # ---------------------------------------------------------------- проверка
    def check(self, tol=2.0):
        """Ищет пересечения линий между собой и проход линий через узлы."""
        issues = []
        segs = self.segments
        for i in range(len(segs)):
            owner_a, p1, p2 = segs[i]
            for j in range(i + 1, len(segs)):
                owner_b, q1, q2 = segs[j]
                if owner_a == owner_b:
                    continue
                if self._share_endpoint(p1, p2, q1, q2, tol):
                    continue
                if self._intersect(p1, p2, q1, q2, tol):
                    issues.append(f'пересечение линий {owner_a} и {owner_b} '
                                  f'({self._fmt(p1)}-{self._fmt(p2)} × {self._fmt(q1)}-{self._fmt(q2)})')
        for owner, x, y, w, h in self.nodes:
            for owner_s, p, q in segs:
                if owner_s == owner:
                    continue
                if self._enters(p, q, x, y, w, h, tol):
                    issues.append(f'линия {owner_s} проходит через узел {owner} '
                                  f'({self._fmt(p)}-{self._fmt(q)}; box {x:.0f},{y:.0f},{w:.0f},{h:.0f})')
        self.issues = issues
        return issues

    @staticmethod
    def _fmt(p):
        return f'{p[0]:.0f},{p[1]:.0f}'

    @staticmethod
    def _share_endpoint(p1, p2, q1, q2, tol):
        def near(a, b):
            return abs(a[0] - b[0]) <= tol and abs(a[1] - b[1]) <= tol
        return near(p1, q1) or near(p1, q2) or near(p2, q1) or near(p2, q2)

    @staticmethod
    def _intersect(a, b, c, d, tol):
        def orient(p, q, r):
            return (q[0] - p[0]) * (r[1] - p[1]) - (q[1] - p[1]) * (r[0] - p[0])
        o1, o2 = orient(a, b, c), orient(a, b, d)
        o3, o4 = orient(c, d, a), orient(c, d, b)
        if (o1 > tol and o2 < -tol or o1 < -tol and o2 > tol) and (o3 > tol and o4 < -tol or o3 < -tol and o4 > tol):
            return True
        return False

    @staticmethod
    def _enters(p, q, x, y, w, h, tol):
        """Проходит ли отрезок через внутреннюю область прямоугольника."""
        x1, y1, x2, y2 = x + tol, y + tol, x + w - tol, y + h - tol
        # пересечение с внутренней областью
        dx, dy = q[0] - p[0], q[1] - p[1]
        t0, t1 = 0.0, 1.0
        for pv, qv in ((-dx, p[0] - x1), (dx, x2 - p[0]), (-dy, p[1] - y1), (dy, y2 - p[1])):
            if abs(pv) < 1e-9:
                if qv < 0:
                    return False
            else:
                r = qv / pv
                if pv < 0:
                    if r > t1:
                        return False
                    if r > t0:
                        t0 = r
                else:
                    if r < t0:
                        return False
                    if r < t1:
                        t1 = r
        return t0 < t1 - 0.02

    def save(self, path, dpi=220):
        os.makedirs(os.path.dirname(path), exist_ok=True)
        self.img.save(path, 'PNG', dpi=(dpi, dpi))
        return path
