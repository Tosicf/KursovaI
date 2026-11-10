"""Мини-рендерер UML-диаграмм (Pillow) для документации «СтройКонтроль»."""
import os
import math
from PIL import Image, ImageDraw, ImageFont

FONT = '/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf'
FONT_BOLD = '/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf'

INK = '#1f2a37'
LINE = '#4a5568'
BLUE = '#2b6cb0'
GREEN = '#2f855a'
ORANGE = '#c05621'
PURPLE = '#6b46c1'
RED = '#c53030'

FILL_BOUNDARY = '#e8f1fb'
FILL_CONTROL = '#e9f7ef'
FILL_ENTITY = '#fdf6df'
FILL_USECASE = '#f4f7fb'
FILL_ACTOR = '#ffffff'


class Canvas:
    def __init__(self, w, h, scale=2):
        self.s = scale
        self.w, self.h = w, h
        self.img = Image.new('RGB', (w * scale, h * scale), 'white')
        self.d = ImageDraw.Draw(self.img)
        self._fonts = {}

    # --- базовое ---
    def font(self, size, bold=False):
        key = (size, bold)
        if key not in self._fonts:
            self._fonts[key] = ImageFont.truetype(FONT_BOLD if bold else FONT, int(size * self.s))
        return self._fonts[key]

    def text(self, x, y, s, size=14, bold=False, anchor='mm', fill=INK):
        self.d.text((x * self.s, y * self.s), s, font=self.font(size, bold), fill=fill, anchor=anchor)

    def text_lines(self, x, y, lines, size=13, bold=False, gap=4, fill=INK):
        line_h = size + gap
        total = line_h * (len(lines) - 1)
        for i, line in enumerate(lines):
            self.text(x, y - total / 2 + i * line_h, line, size=size, bold=bold, fill=fill)

    def _dash(self, p, q, color, width, dash=10, gap=7):
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
                        fill=color, width=max(1, int(width * self.s)))
            t = t2 + gap

    def line(self, p, q, color=LINE, width=1.6, dash=False):
        if dash:
            self._dash(p, q, color, width)
        else:
            self.d.line([p[0] * self.s, p[1] * self.s, q[0] * self.s, q[1] * self.s],
                        fill=color, width=max(1, int(width * self.s)))

    def arrowhead(self, p_from, p_to, color, size=9):
        ang = math.atan2(p_to[1] - p_from[1], p_to[0] - p_from[0])
        for sign in (0.42, -0.42):
            x = p_to[0] - size * math.cos(ang - sign)
            y = p_to[1] - size * math.sin(ang - sign)
            self.d.line([x * self.s, y * self.s, p_to[0] * self.s, p_to[1] * self.s],
                        fill=color, width=max(1, int(1.6 * self.s)))

    def arrow(self, p, q, color=LINE, width=1.6, dash=False, head=True):
        self.line(p, q, color, width, dash)
        if head:
            self.arrowhead(p, q, color)

    def label(self, x, y, s, size=11, color=INK, pad=3, bold=False):
        f = self.font(size, bold)
        bbox = self.d.textbbox((0, 0), s, font=f)
        w = (bbox[2] - bbox[0]) / self.s
        h = (bbox[3] - bbox[1]) / self.s
        self.d.rectangle([(x - w / 2 - pad) * self.s, (y - h / 2 - pad) * self.s,
                          (x + w / 2 + pad) * self.s, (y + h / 2 + pad) * self.s], fill='white')
        self.text(x, y, s, size=size, fill=color, bold=bold)

    # --- фигуры ---
    def rect(self, x, y, w, h, radius=0, fill='white', outline=LINE, width=1.6, dash=False, shadow=False):
        box = [x * self.s, y * self.s, (x + w) * self.s, (y + h) * self.s]
        if shadow:
            off = 3 * self.s
            self.d.rounded_rectangle([box[0] + off, box[1] + off, box[2] + off, box[3] + off],
                                     radius=radius * self.s, fill='#e6e9ee')
        if dash:
            self.d.rectangle(box, fill=fill)
            pts = [(x, y), (x + w, y), (x + w, y + h), (x, y + h), (x, y)]
            for i in range(4):
                self._dash(pts[i], pts[i + 1], outline, width, dash=8, gap=6)
        else:
            self.d.rounded_rectangle(box, radius=radius * self.s, fill=fill,
                                     outline=outline, width=max(1, int(width * self.s)))

    def ellipse(self, cx, cy, rx, ry, fill=FILL_USECASE, outline=LINE, width=1.6, dash=False):
        box = [(cx - rx) * self.s, (cy - ry) * self.s, (cx + rx) * self.s, (cy + ry) * self.s]
        self.d.ellipse(box, fill=fill, outline=outline, width=max(1, int(width * self.s)))

    def diamond(self, cx, cy, w, h, fill='#fff4e6', outline=LINE, width=1.6):
        pts = [(cx, cy - h / 2), (cx + w / 2, cy), (cx, cy + h / 2), (cx - w / 2, cy)]
        self.d.polygon([(x * self.s, y * self.s) for x, y in pts], fill=fill,
                       outline=outline, width=max(1, int(width * self.s)))

    def circle(self, cx, cy, r, fill='#2d3748', outline='#2d3748'):
        self.d.ellipse([(cx - r) * self.s, (cy - r) * self.s, (cx + r) * self.s, (cy + r) * self.s],
                       fill=fill, outline=outline, width=max(1, int(1.6 * self.s)))

    def stick(self, cx, cy, name, color=INK, caption_size=12):
        """cx, cy — центр головы."""
        self.d.ellipse([(cx - 9) * self.s, (cy - 9) * self.s, (cx + 9) * self.s, (cy + 9) * self.s],
                       outline=color, width=max(1, int(1.8 * self.s)), fill='white')
        self.line((cx, cy + 9), (cx, cy + 34), color, 1.8)
        self.line((cx - 14, cy + 18), (cx + 14, cy + 18), color, 1.8)
        self.line((cx, cy + 34), (cx - 12, cy + 54), color, 1.8)
        self.line((cx, cy + 34), (cx + 12, cy + 54), color, 1.8)
        self.text(cx + 22, cy + 22, name, size=caption_size, anchor='lm', fill=color, bold=True)

    def save(self, path, dpi=300):
        out = self.img.resize((self.w, self.h), Image.LANCZOS)
        out.save(path, 'PNG', dpi=(dpi, dpi))


def box_anchor(cx, cy, w, h, tx, ty):
    """Точка на границе прямоугольника по направлению к (tx, ty)."""
    dx, dy = tx - cx, ty - cy
    if dx == 0 and dy == 0:
        return cx, cy
    sx = (w / 2) / abs(dx) if dx else float('inf')
    sy = (h / 2) / abs(dy) if dy else float('inf')
    k = min(sx, sy)
    return cx + dx * k, cy + dy * k


def ellipse_anchor(cx, cy, rx, ry, tx, ty):
    dx, dy = tx - cx, ty - cy
    if dx == 0 and dy == 0:
        return cx, cy
    k = 1.0 / math.sqrt((dx / rx) ** 2 + (dy / ry) ** 2)
    return cx + dx * k, cy + dy * k

# =====================================================================
#  Пути и сборка документа
# =====================================================================
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, 'docs', 'diagrams')
IMG = os.path.join(OUT, 'img')
os.makedirs(IMG, exist_ok=True)

def diagram_01():

    c = Canvas(1300, 950)
    c.text(650, 34, 'Диаграмма вариантов использования', size=20, bold=True)

    RX, RY = 142, 39
    C1, C2 = 560, 955
    ROWS = [152, 268, 384, 500, 616, 732]

    uc = {
        'auth':       (C1, ROWS[0], ['Авторизация в системе']),
        'catalog':    (C1, ROWS[1], ['Ведение справочников:', 'материалы, поставщики,', 'объекты']),
        'deliveries': (C1, ROWS[2], ['Оформление поступлений']),
        'usages':     (C1, ROWS[3], ['Оформление списаний', 'материалов']),
        'stock':      (C1, ROWS[4], ['Контроль остатков', 'и дефицита']),
        'users':      (C1, ROWS[5], ['Управление сотрудниками']),
        'rights':     (C2, ROWS[0], ['Проверка прав доступа']),
        'estimates':  (C2, ROWS[1], ['Работа со сметами', 'и позициями плана']),
        'deviation':  (C2, ROWS[2], ['Анализ отклонений', 'план — факт']),
        'export':     (C2, ROWS[3], ['Экспорт отчёта в CSV']),
        'settings':   (C2, ROWS[4], ['Настройка подключения', 'к SQL Server']),
    }

    actors = [
        ('Инженер ПТО', 100, 185, BLUE),
        ('Снабженец', 100, 345, GREEN),
        ('Прораб', 100, 505, ORANGE),
        ('Бухгалтер', 100, 660, PURPLE),
        ('Администратор', 100, 800, RED),
    ]
    colors = {n: col for n, _, _, col in actors}
    actor_y = {n: y for n, _, y, _ in actors}

    links = {
        'Инженер ПТО': ['auth', 'catalog', 'estimates', 'deliveries', 'usages', 'deviation', 'settings'],
        'Снабженец': ['auth', 'catalog', 'deliveries', 'deviation'],
        'Прораб': ['auth', 'deliveries', 'usages', 'stock'],
        'Бухгалтер': ['auth', 'deviation', 'export'],
        'Администратор': ['auth', 'users', 'settings'],
    }

    for actor, targets in links.items():
        ax, ay = 122, actor_y[actor] + 24
        for t in targets:
            cx, cy, _ = uc[t]
            c.arrow((ax, ay), (cx - RX + 4, cy), color=colors[actor], width=1.4)

    for key, (cx, cy, lines) in uc.items():
        c.ellipse(cx, cy, RX, RY, fill=FILL_USECASE)
        c.text_lines(cx, cy, lines, size=12)

    for name, x, y, color in actors:
        c.stick(x, y, '', color)
        c.text(x, y + 70, name, size=12, bold=True, fill=color)

    c.rect(300, 90, 960, 800, radius=6, fill=None, outline='#8fa3bf', width=1.8, dash=True)
    c.text(312, 110, 'СтройКонтроль', size=14, bold=True, anchor='lm', fill='#44546a')

    p1 = (uc['auth'][0] + RX - 4, uc['auth'][1] - 8)
    p2 = (uc['rights'][0] - RX + 4, uc['rights'][1] - 8)
    c.arrow(p1, p2, color='#4a5568', width=1.4, dash=True)
    c.label((p1[0] + p2[0]) / 2, p1[1] - 16, '«include»', size=11)

    p1 = (uc['deviation'][0] + RX - 30, uc['deviation'][1] + 26)
    p2 = (uc['export'][0] - RX + 30, uc['export'][1] - 26)
    c.arrow(p1, p2, color='#4a5568', width=1.4, dash=True)
    c.label(p1[0] + 30, (p1[1] + p2[1]) / 2 + 4, '«extend»', size=11)

    c.text(650, 928, 'Сплошная линия — ассоциация «актор — вариант использования»; пунктир — зависимости «include» / «extend»', size=11, fill='#5a6b82')
    c.save(os.path.join(IMG, '01-use-case.png'))
    print('ok')

def diagram_02():

    c = Canvas(1380, 880)
    c.text(690, 34, 'Диаграмма анализа: границы, управления и сущности', size=20, bold=True)

    def cls_box(cx, cy, w, h, stereo, name, members, fill):
        x, y = cx - w / 2, cy - h / 2
        c.rect(x, y, w, h, radius=6, fill=fill, outline='#425466', width=1.6, shadow=True)
        c.text(cx, y + 20, f'«{stereo}»', size=11, fill='#5b6b7d')
        c.text(cx, y + 39, name, size=13, bold=True)
        c.line((x + 6, y + 52), (x + w - 6, y + 52), '#8b9aab', 1.0)
        for i, m in enumerate(members):
            c.text(x + 10, y + 70 + i * 18, m, size=10.5, anchor='lm', fill='#33404d')

    bw, bh = 250, 100
    cw, ch = 270, 118
    ew, eh = 195, 150

    boxes = [
        (215, 150, bw, bh, 'boundary', 'LoginWindow', ['+ ShowDialog() bool', '+ ErrorText : string'], FILL_BOUNDARY),
        (690, 150, bw, bh, 'boundary', 'MainWindow', ['+ NavigationItems', '+ ShowPageAsync(page)'], FILL_BOUNDARY),
        (1140, 150, bw, bh, 'boundary', 'EditorWindow', ['+ ShowDialog() bool', '+ Validate() string'], FILL_BOUNDARY),
        (210, 380, cw, ch, 'control', 'AuthService', ['+ SignInAsync(login, password)', '+ SignOut()'], FILL_CONTROL),
        (560, 380, cw, ch, 'control', 'SessionService', ['+ CurrentUser : User', '+ CanEditEstimates bool', '+ CanViewFinance bool'], FILL_CONTROL),
        (910, 380, cw, ch, 'control', 'InventoryService', ['+ CreateDeliveryAsync(d) Delivery', '+ CreateUsageAsync(u) MaterialUsage'], FILL_CONTROL),
        (1240, 380, cw, ch, 'control', 'DeviationService', ['+ BuildReportAsync(projectId)', '  : DeviationReport'], FILL_CONTROL),
        (110, 690, ew, eh, 'entity', 'User', ['+ Login : string', '+ PasswordHash', '+ Role : UserRole', '+ IsActive : bool'], FILL_ENTITY),
        (320, 690, ew, eh, 'entity', 'Material', ['+ Code : string', '+ Name : string', '+ StockQuantity', '+ MinStock : decimal'], FILL_ENTITY),
        (530, 690, ew, eh, 'entity', 'Delivery', ['+ Number : string', '+ Date : DateTime', '+ Items : List'], FILL_ENTITY),
        (740, 690, ew, eh, 'entity', 'MaterialUsage', ['+ Quantity : decimal', '+ UnitPrice : decimal', '+ DocumentNumber'], FILL_ENTITY),
        (950, 690, ew, eh, 'entity', 'Estimate', ['+ Number : string', '+ ProjectId : int', '+ Items : List'], FILL_ENTITY),
        (1180, 690, ew, eh, 'entity', 'Project', ['+ Name : string', '+ ContractSum', '+ ProgressPercent : int'], FILL_ENTITY),
    ]

    edges = [
        ((215, 200), (210, 321), 'запрос на вход', (128, 268)),
        ((690, 200), (560, 321), 'права роли', (722, 250)),
        ((1140, 200), (910, 321), 'сохранение документа', (1112, 250)),
        ((210, 439), (110, 615), 'проверка пароля', (92, 500)),
        ((910, 439), (320, 615), 'изменение остатка', (592, 492)),
        ((910, 439), (530, 615), None, None),
        ((910, 439), (740, 615), None, None),
        ((1240, 439), (950, 615), 'план / факт', (1092, 466)),
        ((1240, 439), (1180, 615), None, None),
    ]
    for p, q, _, _ in edges:
        c.arrow(p, q, color='#7b8794', width=1.5)

    for cx, cy, w, h, st, name, members, fill in boxes:
        cls_box(cx, cy, w, h, st, name, members, fill)

    for p, q, text, pos in edges:
        if text:
            c.label(pos[0], pos[1], text, size=10, color='#41505f')

    c.text(690, 850, 'Границы (boundary) — окна WPF; управления (control) — сервисы; сущности (entity) — модель EF Core',
           size=11, fill='#5a6b82')
    c.save(os.path.join(IMG, '02-analysis-classes.png'))
    print('ok 2')

def diagram_03():

    c = Canvas(1360, 900)
    c.text(680, 32, 'Диаграмма последовательности: вход в систему', size=20, bold=True)

    parts = [
        ('Пользователь', 95, 'actor'),
        ('LoginWindow', 275, 'obj'),
        ('LoginViewModel', 445, 'obj'),
        ('AuthService', 615, 'obj'),
        ('UserRepository', 800, 'obj'),
        ('SQL Server', 985, 'db'),
        ('SessionService', 1150, 'obj'),
        ('MainWindow', 1295, 'obj'),
    ]
    TOP, BOT = 96, 150
    c.text(680, 62, '', size=1)

    for name, x, kind in parts:
        w = max(112, len(name) * 8.2 + 24)
        fill = '#eef4fb' if kind == 'obj' else ('#eaf5ee' if kind == 'db' else '#ffffff')
        c.rect(x - w / 2, TOP, w, 46, radius=5, fill=fill, outline='#4a5568', width=1.5, shadow=True)
        c.text(x, TOP + 23, name, size=12.5, bold=True)

    for name, x, kind in parts:
        c.line((x, TOP + 46), (x, 852), '#b3bfcc', 1.2, dash=True)

    X = {name: x for name, x, _ in parts}

    msgs = [
        ('Пользователь', 'LoginWindow', 'логин и пароль', 'sync', None),
        ('LoginWindow', 'LoginViewModel', 'LoginCommand', 'sync', None),
        ('LoginViewModel', 'AuthService', 'SignInAsync(login, password)', 'sync', None),
        ('AuthService', 'UserRepository', 'FindByLoginAsync(login)', 'sync', None),
        ('UserRepository', 'SQL Server', 'SELECT … FROM dbo.users', 'sync', None),
        ('SQL Server', 'UserRepository', 'строка пользователя', 'ret', None),
        ('UserRepository', 'AuthService', 'User', 'ret', None),
        ('AuthService', 'AuthService', 'Verify(password, salt, hash)', 'self', None),
        ('AuthService', 'LoginViewModel', '«Неверный логин или пароль»', 'ret', '[пароль неверен]'),
        ('AuthService', 'UserRepository', 'UpdateLastLoginAsync(now)', 'sync', '[пароль верен]'),
        ('UserRepository', 'SQL Server', 'UPDATE dbo.users', 'sync', None),
        ('AuthService', 'SessionService', 'SignIn(user)', 'sync', None),
        ('AuthService', 'LoginViewModel', 'User', 'ret', None),
        ('LoginViewModel', 'LoginWindow', 'DialogResult = true', 'ret', None),
        ('LoginWindow', 'MainWindow', 'открыть главное окно', 'sync', None),
    ]

    y = 186
    STEP = 44
    for src, dst, text, kind, guard in msgs:
        if kind == 'self':
            x = X[src]
            c.line((x, y - 6), (x + 62, y - 6), color='#4a5568', width=1.5)
            c.line((x + 62, y - 6), (x + 62, y + 12), color='#4a5568', width=1.5)
            c.arrow((x + 62, y + 12), (x + 4, y + 12), color='#4a5568', width=1.5)
            c.text(x + 70, y - 14, text, size=11, anchor='lm', fill='#33404d')
            y += STEP + 8
            continue

        x1, x2 = X[src], X[dst]
        dashed = kind == 'ret'
        c.arrow((x1, y), (x2, y), color='#42546a', width=1.5, dash=dashed)
        mid = (x1 + x2) / 2
        c.label(mid, y - 12, text, size=10.5, color='#2c3a4a')
        if guard:
            c.text(min(x1, x2) + 4, y + 12, guard, size=10, anchor='lm', fill='#7a4b00')
        y += STEP

    c.text(680, 878,
           'Сплошная стрелка — синхронный вызов, пунктирная — возврат значения; штриховая рамка на стрелке — условие ветвления',
           size=10.5, fill='#5a6b82')
    c.save(os.path.join(IMG, '03-sequence-login.png'))
    print('ok 3')

def diagram_04():

    c = Canvas(1120, 1290)
    c.text(560, 34, 'Диаграмма деятельности: оформление списания материала', size=20, bold=True)

    def act(cx, cy, w, h, lines, fill='#f6f9fc'):
        c.rect(cx - w / 2, cy - h / 2, w, h, radius=16, fill=fill, outline='#42546a', width=1.6, shadow=True)
        c.text_lines(cx, cy, lines, size=12)

    def dec(cx, cy, w, h, lines):
        c.diamond(cx, cy, w, h, fill='#fff6e8')
        c.text_lines(cx, cy, lines, size=11.5)

    def final(cx, cy):
        c.circle(cx, cy, 13, fill='#ffffff', outline='#2d3748')
        c.circle(cx, cy, 7, fill='#2d3748')

    # --- узлы ---
    c.circle(400, 76, 11)

    act(400, 155, 340, 54, ['Открыть раздел «Списания»'])
    dec(400, 272, 260, 104, ['Право', 'CanEditUsages?'])

    act(135, 272, 210, 78, ['Кнопка «Добавить»', 'неактивна, команда', 'не выполняется'], fill='#f2f2f2')
    final(135, 380)

    act(400, 400, 400, 84, ['Заполнить акт: объект, материал,', 'количество, цена, дата,', 'номер документа'])
    dec(400, 540, 260, 104, ['Данные', 'корректны?'])
    act(880, 540, 240, 66, ['Сообщение', 'об ошибке'], fill='#fdecec')

    act(400, 664, 330, 54, ['Начать транзакцию'])
    act(400, 748, 400, 54, ['Уменьшить остаток материала'])
    act(400, 832, 330, 54, ['Сохранить списание'])
    dec(400, 948, 260, 104, ['Сохранено?'])
    act(880, 948, 240, 66, ['Откат', 'транзакции'], fill='#fdecec')

    act(400, 1064, 330, 54, ['Зафиксировать изменения'])
    act(400, 1148, 400, 54, ['Обновить таблицу, остатки и KPI'])
    final(400, 1230)

    # --- связи ---
    c.arrow((400, 87), (400, 128))
    c.arrow((400, 182), (400, 220))

    c.arrow((270, 272), (242, 272))          # нет
    c.label(300, 256, 'нет', size=11)
    c.arrow((400, 324), (400, 358))          # да
    c.label(424, 340, 'да', size=11)
    c.arrow((135, 311), (135, 366))

    c.arrow((400, 442), (400, 488))
    c.arrow((530, 540), (758, 540))          # нет → ошибка
    c.label(610, 522, 'нет', size=11)
    c.arrow((400, 592), (400, 637))
    c.label(424, 612, 'да', size=11)

    # возврат из «Сообщение об ошибке» в форму
    c.line((880, 507), (880, 470), color='#42546a', width=1.5)
    c.line((880, 470), (620, 470), color='#42546a', width=1.5)
    c.line((620, 470), (620, 400), color='#42546a', width=1.5)
    c.arrow((620, 400), (604, 400))

    c.arrow((400, 691), (400, 721))
    c.arrow((400, 775), (400, 805))
    c.arrow((400, 859), (400, 896))

    c.arrow((400, 1000), (400, 1037))        # да
    c.label(424, 1018, 'да', size=11)
    c.arrow((530, 948), (758, 948))          # нет
    c.label(612, 930, 'нет', size=11)

    c.arrow((880, 915), (880, 576), color='#42546a', width=1.5)

    c.arrow((400, 1091), (400, 1121))
    c.arrow((400, 1175), (400, 1216))

    c.text(560, 1272,
           'Ромбы — точки ветвления; из блока ошибки управление возвращается в форму ввода; изменение остатка и сохранение идут одной транзакцией',
           size=10.5, fill='#5a6b82')

    c.save(os.path.join(IMG, '04-activity-usage.png'))
    print('ok 4')

def diagram_05():

    c = Canvas(1240, 1000)
    c.text(620, 34, 'Диаграмма деятельности варианта использования «Работа со сметой»', size=19, bold=True)

    def act(cx, cy, w, h, lines, fill='#f6f9fc'):
        c.rect(cx - w / 2, cy - h / 2, w, h, radius=16, fill=fill, outline='#42546a', width=1.6, shadow=True)
        c.text_lines(cx, cy, lines, size=11.5)

    def dec(cx, cy, w, h, lines):
        c.diamond(cx, cy, w, h, fill='#fff6e8')
        c.text_lines(cx, cy, lines, size=11)

    def final(cx, cy):
        c.circle(cx, cy, 13, fill='#ffffff', outline='#2d3748')
        c.circle(cx, cy, 7, fill='#2d3748')

    c.circle(430, 74, 11)

    act(430, 150, 300, 52, ['Ввод логина и пароля'])
    dec(430, 252, 260, 96, ['Учётные данные', 'верны?'])

    c.line((300, 252), (170, 252), color='#42546a', width=1.5)
    c.line((170, 252), (170, 150), color='#42546a', width=1.5)
    c.line((170, 150), (276, 150), color='#42546a', width=1.5)
    c.arrow((196, 150), (278, 150))
    c.label(228, 234, 'нет', size=10.5)
    c.label(470, 214, 'да', size=10.5)

    act(430, 356, 320, 52, ['Открыть раздел «Сметы»'])
    dec(430, 458, 300, 100, ['Право', 'CanEditEstimates?'])

    act(880, 458, 250, 62, ['Доступен только', 'просмотр'], fill='#f2f2f2')
    final(880, 556)

    dec(430, 610, 270, 116, ['Выбор действия'])

    acts = [
        (150, 'Добавить смету', 'Сохранение сметы', '#eaf5ee'),
        (380, 'Изменить смету', 'Сохранение изменений', '#eaf5ee'),
        (610, 'Просмотреть смету', None, '#f6f9fc'),
        (840, 'Удалить смету', 'Удаление сметы и позиций', '#fdecec'),
    ]
    for x, title, save, fill in acts:
        act(x, 772, 210, 62, [title], fill=fill)
        c.arrow((430, 668), (x, 741), color='#42546a', width=1.4)
        if save:
            act(x, 872, 210, 62, [save], fill='#f6f9fc')
            c.arrow((x, 803), (x, 841), color='#42546a', width=1.4)

    act(1080, 772, 150, 62, ['Выход'])
    c.line((556, 610), (1080, 610), color='#42546a', width=1.4)
    c.arrow((1080, 610), (1080, 741), color='#42546a', width=1.4)
    final(1080, 866)

    c.label(250, 716, 'добавить', size=10)
    c.label(396, 712, 'изменить', size=10)
    c.label(556, 700, 'просмотреть', size=10)
    c.label(792, 700, 'удалить', size=10)
    c.label(700, 594, 'выход', size=10)

    # возврат к выбору действия — общая шина
    bus_y = 936
    for x, _, save, _ in acts:
        bottom = 903 if save else 803
        c.line((x, bottom), (x, bus_y), color='#7b8794', width=1.2, dash=True)
    c.line((150, bus_y), (840, bus_y), color='#7b8794', width=1.2, dash=True)
    c.line((725, bus_y), (725, 680), color='#7b8794', width=1.2, dash=True)
    c.arrow((725, 682), (725, 674), color='#7b8794', width=1.2)

    # «нет» по праву
    c.arrow((580, 458), (755, 458))
    c.label(650, 440, 'нет', size=10.5)
    c.arrow((430, 300), (430, 330))
    c.arrow((430, 382), (430, 408))
    c.arrow((430, 508), (430, 552))
    c.label(452, 528, 'да', size=10.5)
    c.arrow((880, 489), (880, 543))

    c.text(620, 982,
           'Ветвление «Выбор действия» повторяется, пока пользователь не выберет «Выход»; правка сметы доступна только роли с правом CanEditEstimates',
           size=10.5, fill='#5a6b82')

    c.save(os.path.join(IMG, '05-activity-estimate.png'))
    print('ok 5')

def diagram_06():

    c = Canvas(1340, 980)
    c.text(670, 34, 'Диаграмма автоматов: навигация по разделам приложения', size=20, bold=True)

    def state(cx, cy, w, h, label, fill='#eef4fb', bold=True):
        c.rect(cx - w / 2, cy - h / 2, w, h, radius=22, fill=fill, outline='#42546a', width=1.7, shadow=True)
        c.text(cx, cy, label, size=12.5, bold=bold)

    def final(cx, cy):
        c.circle(cx, cy, 13, fill='#ffffff', outline='#2d3748')
        c.circle(cx, cy, 7, fill='#2d3748')

    c.circle(670, 74, 10)
    state(670, 146, 300, 64, 'Инициализация приложения')
    c.rect(670 - 180, 470 - 43, 360, 86, radius=22, fill='#fdf6df', outline='#42546a', width=1.7, shadow=True)
    c.text(670, 470, 'Ожидание выбора пункта меню', size=12.5)

    left = [
        (200, 'Панель управления'),
        (200, 'Материалы'),
        (200, 'Поставщики'),
        (200, 'Объекты'),
        (200, 'Сметы'),
    ]
    right = [
        (1140, 'Поступления'),
        (1140, 'Списания'),
        (1140, 'Анализ отклонений'),
        (1140, 'Сотрудники'),
        (1140, 'Настройки'),
    ]
    ys = [200, 320, 440, 560, 680]
    LW, LH = 250, 66

    c.arrow((670, 85), (670, 113))
    c.arrow((670, 178), (670, 426))
    c.label(700, 300, 'загружено главное окно', size=10)

    for (x, name), y in zip(left + right, ys + ys):
        fill = '#e9f7ef' if name == 'Сотрудники' else '#eef4fb'
        state(x, y, LW, LH, name, fill=fill, bold=False)
        if x < 670:
            start, end = (490, 470 - 18), (x + LW / 2 + 4, y)
            c.line(start, end, color='#42546a', width=1.4)
            c.arrowhead(start, end, '#42546a')
            back_s = (start[0], start[1] + 16)
            back_e = (end[0], end[1] + 14)
            c.arrow(back_e, back_s, color='#8d9aa8', width=1.2, dash=True)
            c.label((start[0] + end[0]) / 2 - 6, (start[1] + end[1]) / 2 + 12, f'«{name}»', size=9.5)
        else:
            start, end = (850, 470 - 18), (x - LW / 2 - 4, y)
            c.line(start, end, color='#42546a', width=1.4)
            c.arrowhead(start, end, '#42546a')
            back_s = (start[0], start[1] + 16)
            back_e = (end[0], end[1] + 14)
            c.arrow(back_e, back_s, color='#8d9aa8', width=1.2, dash=True)
            c.label((start[0] + end[0]) / 2 + 6, (start[1] + end[1]) / 2 + 12, f'«{name}»', size=9.5)

    c.arrow((670, 513), (670, 852))
    c.label(690, 700, 'выход из учётной записи', size=10)
    final(670, 872)

    c.text(670, 930,
           'Сплошная стрелка — переход selectMenuItem(«раздел») из меню, пунктир — возврат closeWindow(); жёлтым выделено состояние ожидания выбора',
           size=10.5, fill='#5a6b82')
    c.text(670, 952, 'Состояние «Сотрудники» доступно только роли Администратор (CanManageUsers)', size=10.5, fill='#5a6b82')

    c.save(os.path.join(IMG, '06-state-navigation.png'))
    print('ok 6')


def build_docx(path):
    """Сводный документ: изображения + подписи по ГОСТ («Рисунок N — ...»)."""
    from docx import Document
    from docx.enum.text import WD_ALIGN_PARAGRAPH
    from docx.shared import Cm, Pt

    doc = Document()
    section = doc.sections[0]
    section.left_margin = Cm(2.5)
    section.right_margin = Cm(2)
    section.top_margin = Cm(2)
    section.bottom_margin = Cm(2)

    normal = doc.styles['Normal']
    normal.font.name = 'Times New Roman'
    normal.font.size = Pt(12)
    normal.paragraph_format.space_after = Pt(6)
    normal.paragraph_format.line_spacing = 1.15

    def para(text, *, size=12, bold=False, italic=False, align=WD_ALIGN_PARAGRAPH.JUSTIFY, before=0, after=6):
        p = doc.add_paragraph()
        p.alignment = align
        p.paragraph_format.space_before = Pt(before)
        p.paragraph_format.space_after = Pt(after)
        run = p.add_run(text)
        run.font.name = 'Times New Roman'
        run.font.size = Pt(size)
        run.bold = bold
        run.italic = italic
        return p

    para('ДИАГРАММЫ ПРОЕКТА', size=18, bold=True, align=WD_ALIGN_PARAGRAPH.CENTER, before=0, after=4)
    para('«СтройКонтроль»', size=18, bold=True, align=WD_ALIGN_PARAGRAPH.CENTER, after=14)
    para('Сетевое программное средство управления строительными материалами '
         'и анализа отклонений от сметы на строительном объекте',
         size=12, italic=True, align=WD_ALIGN_PARAGRAPH.CENTER, after=4)
    para('Технологии: C# 12, .NET 8, WPF (MVVM), EF Core 8, Microsoft SQL Server',
         size=11, align=WD_ALIGN_PARAGRAPH.CENTER, after=18)

    items = [
        ('img/01-use-case.png', 'Диаграмма вариантов использования',
         'Пять акторов соответствуют ролям системы. Авторизация и проверка прав доступа общие для всех; '
         'смету редактируют только инженер ПТО и администратор, прораб работает с поступлениями и списаниями, '
         'бухгалтер формирует отчёты.'),
        ('img/02-analysis-classes.png', 'Диаграмма анализа: границы, управления и сущности',
         'Границы (boundary) — окна WPF, управления (control) — сервисы слоёв Core и Data, '
         'сущности (entity) — классы модели EF Core, отображаемые в таблицы базы данных.'),
        ('img/03-sequence-login.png', 'Диаграмма последовательности: вход в систему',
         'Показан обмен сообщениями между окном входа, моделью представления, сервисом аутентификации '
         'и репозиторием при обращении к SQL Server. Пароль проверяется по схеме PBKDF2-HMAC-SHA256 '
         '(100 000 итераций), в базе хранятся только хеш и соль.'),
        ('img/04-activity-usage.png', 'Диаграмма деятельности: оформление списания материала',
         'Списание материала и уменьшение складского остатка выполняются в одной транзакции, '
         'поэтому при сбое сохранения данные остаются согласованными.'),
        ('img/05-activity-estimate.png', 'Диаграмма деятельности варианта использования «Работа со сметой»',
         'Сценарий повторяется в цикле, пока пользователь не выберет «Выход». Ветвление по праву '
         'CanEditEstimates отделяет инженера ПТО от ролей, которым доступен только просмотр сметы.'),
        ('img/06-state-navigation.png', 'Диаграмма автоматов: навигация по разделам приложения',
         'Из состояния ожидания выбора пользователь переходит в один из десяти разделов и возвращается обратно. '
         'Раздел «Сотрудники» доступен только администратору.'),
    ]

    for index, (image, caption, note) in enumerate(items, start=1):
        if index > 1:
            doc.add_page_break()
        para(f'{index}. {caption}', size=14, bold=True, align=WD_ALIGN_PARAGRAPH.LEFT, before=0, after=8)
        pic = doc.add_paragraph()
        pic.alignment = WD_ALIGN_PARAGRAPH.CENTER
        pic.paragraph_format.space_after = Pt(8)
        pic.add_run().add_picture(os.path.join(OUT, image), width=Cm(16))
        para(f'Рисунок {index} — {caption}', size=12, align=WD_ALIGN_PARAGRAPH.CENTER, after=10)
        para(note, size=12, align=WD_ALIGN_PARAGRAPH.JUSTIFY)

    doc.save(path)
    print('docx:', path)


def main():
    diagram_01()
    diagram_02()
    diagram_03()
    diagram_04()
    diagram_05()
    diagram_06()
    build_docx(os.path.join(ROOT, 'docs', 'Диаграммы.docx'))


if __name__ == '__main__':
    main()
