"""Диаграммы для пояснительной записки «СтройКонтроль» (UML, Pillow + DejaVu Sans).

Дополняет tools/render_diagrams.py: строит диаграммы, которых не было в наборе
документации (вторая диаграмма последовательности с фрагментами, две диаграммы
кооперации, вторая диаграмма состояний, диаграмма классов проектирования,
компонентов и размещения).

Запуск:  python tools/render_report_diagrams.py
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from PIL import Image  # noqa: E402

from render_diagrams import Canvas, IMG  # noqa: E402

INK = '#1f2a37'
LINE = '#4a5568'
MUTED = '#5a6b82'
FRAG = '#8a94a6'

HEADER_FILL = '#eef4fb'
DB_FILL = '#eaf5ee'
SELF_FILL = '#fdf6e3'
STATE_FILL = '#f3f0fb'


def text_width(c, s, size, bold=False):
    bbox = c.d.textbbox((0, 0), s, font=c.font(size, bold))
    return (bbox[2] - bbox[0]) / c.s


def dash_rect(c, x, y, w, h, color=FRAG, width=1.3):
    pts = [(x, y), (x + w, y), (x + w, y + h), (x, y + h), (x, y)]
    for i in range(4):
        c._dash(pts[i], pts[i + 1], color, width, dash=9, gap=6)


def triangle(c, tip, direction, size=14, color='#2f855a'):
    """Пустой (незаполненный) треугольник обобщения/реализации."""
    x, y = tip
    if direction == 'left':
        pts = [(x, y), (x + size, y - size * 0.6), (x + size, y + size * 0.6)]
    elif direction == 'up':
        pts = [(x, y), (x - size * 0.6, y + size), (x + size * 0.6, y + size)]
    else:
        pts = [(x, y), (x - size * 0.6, y - size), (x + size * 0.6, y - size)]
    c.d.polygon([(px * c.s, py * c.s) for px, py in pts], fill='white', outline=color,
                width=max(1, int(1.4 * c.s)))


def crop_and_save(c, path, height):
    """Сохраняет изображение, обрезая лишнее поле снизу."""
    height = int(height)
    img = c.img.crop((0, 0, int(c.w * c.s), height * c.s))
    img = img.resize((c.w, height), Image.LANCZOS)
    img.save(path, 'PNG', dpi=(300, 300))


def fragment(c, x, y, w, h, kind, guard, guard2=None, sep_y=None):
    dash_rect(c, x, y, w, h)
    tab_w = 56
    pts = [(x, y), (x + tab_w, y), (x + tab_w, y + 20), (x + tab_w - 12, y + 30), (x, y + 30)]
    c.d.polygon([(px * c.s, py * c.s) for px, py in pts], fill='#eef1f6',
                outline=FRAG, width=max(1, int(1.2 * c.s)))
    c.text(x + tab_w / 2 - 4, y + 15, kind, size=11, bold=True, fill='#3d4a5c')
    c.text(x + tab_w + 10, y + 15, guard, size=11, anchor='lm', fill='#3d4a5c')
    if sep_y is not None:
        c._dash((x + 6, sep_y), (x + w - 6, sep_y), FRAG, 1.2, dash=9, gap=6)
    if guard2:
        c.text(x + 12, sep_y + 16, guard2, size=11, anchor='lm', fill='#3d4a5c')


# =====================================================================
#  07. Диаграмма последовательности: смета и минимальный запас
# =====================================================================
def diagram_07():
    c = Canvas(1480, 1500)
    c.text(740, 30, 'Диаграмма последовательности: формирование сметы',
           size=20, bold=True)
    c.text(740, 58, 'и автоматический пересчёт минимального запаса материалов',
           size=15, fill=MUTED)

    parts = [
        ('Инженер ПТО', 92, 'actor'),
        ('EstimatesView', 300, 'obj'),
        ('EstimatesViewModel', 520, 'obj'),
        ('EstimateRepository', 770, 'obj'),
        ('SQL Server', 980, 'db'),
        ('MinStockService', 1180, 'obj'),
        ('MaterialsViewModel', 1380, 'obj'),
    ]
    TOP = 96

    for name, x, kind in parts:
        w = max(120, len(name) * 8.0 + 26)
        fill = HEADER_FILL if kind == 'obj' else (DB_FILL if kind == 'db' else 'white')
        c.rect(x - w / 2, TOP, w, 44, radius=5, fill=fill, shadow=True)
        c.text(x, TOP + 22, name, size=12, bold=True)

    X = {name: x for name, x, _ in parts}

    msgs = [
        ('Инженер ПТО', 'EstimatesView', 'открыть раздел «Сметы»', 'sync'),
        ('EstimatesView', 'EstimatesViewModel', 'LoadAsync()', 'sync'),
        ('EstimatesViewModel', 'EstimateRepository', 'GetAllWithItemsAsync()', 'sync'),
        ('EstimateRepository', 'SQL Server', 'SELECT сметы + позиции + материалы', 'sync'),
        ('SQL Server', 'EstimateRepository', 'строки выборки', 'ret'),
        ('EstimateRepository', 'EstimatesViewModel', 'IReadOnlyList<Estimate>', 'ret'),
        ('EstimatesViewModel', 'EstimatesView', 'сметы для таблицы', 'ret'),
        ('Инженер ПТО', 'EstimatesView', 'правка позиции (F2 / Del)', 'sync'),
        ('EstimatesView', 'EstimatesViewModel', 'SaveCommand', 'sync'),
        ('EstimatesViewModel', 'EstimatesViewModel', 'PlannedCost = Qty × Price', 'self'),
        ('EstimatesViewModel', 'EstimateRepository', 'SaveItemAsync(item)', 'sync'),
        ('EstimateRepository', 'SQL Server', 'INSERT / UPDATE dbo.estimate_items', 'sync'),
        ('SQL Server', 'EstimateRepository', 'item.Id', 'ret'),
        ('EstimatesViewModel', 'MinStockService', 'BuildSuggestionsAsync(projectId)', 'sync'),
        ('MinStockService', 'SQL Server', 'SUM(PlannedQuantity) по сметам', 'sync'),
        ('SQL Server', 'MinStockService', 'агрегаты по материалам', 'ret'),
        ('MinStockService', 'EstimatesViewModel', 'MinStockSuggestion[]', 'ret'),
        ('EstimatesViewModel', 'EstimatesViewModel', 'работа — только в стоимости', 'self'),
        ('MinStockService', 'MaterialsViewModel', 'пометить «Ниже минимума»', 'sync'),
        ('EstimatesViewModel', 'EstimatesView', 'итоги и сводка', 'ret'),
    ]

    extra = {10: 46, 13: 46, 17: 42, 18: 46}
    ys = []
    y = 172
    for index, (_, _, _, kind) in enumerate(msgs):
        y += extra.get(index, 0)
        ys.append(y)
        y += 50 if kind == 'self' else 42

    for name, x, _ in parts:
        c.line((x, TOP + 44), (x, ys[-1] + 26), '#b3bfcc', 1.2, dash=True)

    for index, (src, dst, text, kind) in enumerate(msgs):
        y = ys[index]
        if kind == 'self':
            x = X[src]
            c.line((x, y - 6), (x + 62, y - 6), color=LINE, width=1.4)
            c.line((x + 62, y - 6), (x + 62, y + 12), color=LINE, width=1.4)
            c.arrow((x + 62, y + 12), (x + 4, y + 12), color=LINE, width=1.4)
            c.text(x + 74, y - 12, text, size=10.5, anchor='lm', fill='#33404d')
            continue
        x1, x2 = X[src], X[dst]
        c.arrow((x1, y), (x2, y), color='#42546a', width=1.4, dash=(kind == 'ret'))
        c.label((x1 + x2) / 2, y - 12, text, size=10.5, color='#2c3a4a')

    fragment(c, X['EstimatesViewModel'] - 66, ys[10] - 56,
             X['SQL Server'] - X['EstimatesViewModel'] + 150, ys[12] - ys[10] + 96,
             'loop', 'для каждой изменённой позиции сметы')

    alt_top = ys[13] - 56
    alt_bottom = ys[17] + 40
    sep = ys[17] - 36
    fragment(c, X['EstimatesViewModel'] - 66, alt_top,
             X['MinStockService'] - X['EstimatesViewModel'] + 150, alt_bottom - alt_top,
             'alt', '[позиция — материал]', '[позиция — работа]', sep_y=sep)

    fragment(c, X['MinStockService'] - 70, ys[18] - 56,
             X['MaterialsViewModel'] - X['MinStockService'] + 130, 96,
             'opt', '[остаток ниже минимального запаса]')

    foot = ys[-1] + 54
    c.text(740, foot,
           'Сплошная стрелка — синхронный вызов, пунктирная — возврат значения; '
           'loop / alt / opt — цикл, ветвление, необязательный шаг',
           size=10.5, fill=MUTED)
    c.text(740, foot + 24,
           'Предложения пересчитываются после сохранения любой позиции-материала '
           'и всегда соответствуют действующим сметам',
           size=10.5, fill=MUTED)

    crop_and_save(c, os.path.join(IMG, '07-sequence-estimate.png'), foot + 50)
    print('ok 7')


# =====================================================================
#  08. Диаграмма кооперации: вход в систему
# =====================================================================
def obj_box(c, cx, cy, w, h, name, cls, fill=HEADER_FILL):
    c.rect(cx - w / 2, cy - h / 2, w, h, radius=4, fill=fill, shadow=True)
    name_w = text_width(c, name, 12.5, True)
    c.text(cx, cy - h / 2 + 20, name, size=12.5, bold=True)
    c.line((cx - name_w / 2, cy - h / 2 + 27), (cx + name_w / 2, cy - h / 2 + 27),
           color=INK, width=1.2)
    c.text(cx, cy - h / 2 + 44, ':' + cls, size=11, fill='#3d4a5c')


def link(c, p, q, text, dash=False, color='#42546a', pos=0.5, offset=-12, size=10.5):
    c.arrow(p, q, color=color, width=1.4, dash=dash)
    mx = p[0] + (q[0] - p[0]) * pos
    my = p[1] + (q[1] - p[1]) * pos + offset
    c.label(mx, my, text, size=size, color='#2c3a4a')


def diagram_08():
    c = Canvas(1400, 800)
    c.text(700, 30, 'Диаграмма кооперации: вход пользователя в систему', size=20, bold=True)
    c.text(700, 58, 'Номера сообщений задают порядок обмена; стрелка направлена получателю',
           size=13, fill=MUTED)

    c.stick(70, 470, 'Пользователь', caption_size=11.5)
    obj_box(c, 340, 470, 250, 92, 'Окно входа', 'LoginWindow')
    obj_box(c, 640, 470, 270, 92, 'Модель входа', 'LoginViewModel')
    obj_box(c, 940, 320, 260, 92, 'Сервис доступа', 'AuthService')
    obj_box(c, 1180, 150, 260, 92, 'Сессия', 'SessionService')
    obj_box(c, 1180, 480, 260, 92, 'Репозиторий', 'UserRepository')
    obj_box(c, 1180, 700, 260, 92, 'База данных', 'SQLServer', fill=DB_FILL)

    c.text(1300, 610, 'таблица dbo.users', size=10.5, anchor='rm', fill=MUTED)

    link(c, (150, 470), (215, 470), '1: логин и пароль', offset=-74)
    link(c, (465, 470), (505, 470), '2: LoginCommand', offset=-74)
    c.arrow((770, 440), (880, 366), color='#42546a', width=1.4)
    c.text(900, 408, '3: SignInAsync(login, password)', size=10.5, anchor='lm', fill='#2c3a4a')
    link(c, (1070, 350), (1105, 434), '4: FindByLoginAsync(login)', offset=14)
    link(c, (1180, 526), (1180, 654), '5: SELECT … FROM dbo.users', offset=-16)
    link(c, (1030, 276), (1080, 196), '6: SignIn(user)', offset=16)
    c.arrow((860, 320), (720, 424), color='#42546a', width=1.4, dash=True)
    c.text(806, 358, '7: результат проверки', size=10.5, anchor='mm', fill='#2c3a4a')

    c.text(700, 756, 'Сообщение 5 — единственное обращение к серверу базы данных; '
                     'пароль сверяется по схеме PBKDF2-HMAC-SHA256 (100 000 итераций), '
                     'в таблице хранятся только хеш и соль', size=10.5, fill=MUTED)
    c.text(700, 778, 'Пунктирная стрелка 7 — возврат значения, '
                     'сплошные 1–6 — синхронные вызовы объектов', size=10.5, fill=MUTED)

    c.save(os.path.join(IMG, '08-cooperation-login.png'))
    print('ok 8')


# =====================================================================
#  09. Диаграмма кооперации: анализ отклонений
# =====================================================================
def diagram_09():
    c = Canvas(1440, 940)
    c.text(720, 30, 'Диаграмма кооперации: формирование отчёта об отклонениях',
           size=20, bold=True)
    c.text(720, 58, 'Представление, модель представления, сервис анализа, '
                    'калькулятор и экспортёр', size=13, fill=MUTED)

    c.stick(70, 500, 'Пользователь', caption_size=11.5)
    obj_box(c, 340, 500, 250, 92, 'Раздел отчёта', 'DeviationsView')
    obj_box(c, 660, 500, 280, 92, 'Модель раздела', 'DeviationsViewModel')
    obj_box(c, 1010, 500, 250, 92, 'Сервис анализа', 'DeviationService')
    obj_box(c, 1010, 220, 250, 92, 'База данных', 'SQLServer', fill=DB_FILL)
    obj_box(c, 1310, 500, 240, 92, 'Калькулятор', 'DeviationCalculator', fill=SELF_FILL)
    obj_box(c, 660, 780, 280, 92, 'Экспорт', 'XlsxReportExporter', fill=SELF_FILL)

    link(c, (150, 500), (215, 500), '1: «Сформировать»', offset=-60)
    link(c, (465, 500), (520, 500), '2: команда', offset=-60)
    link(c, (800, 500), (885, 500), '3: BuildReportAsync', offset=-60)
    c.arrow((1010, 454), (1010, 266), color='#42546a', width=1.4)
    c.text(1024, 330, '4: SELECT сметы и позиции', size=10.5, anchor='lm', fill='#2c3a4a')
    c.arrow((950, 266), (950, 454), color='#42546a', width=1.4, dash=True)
    c.text(936, 386, '5: строки выборки', size=10.5, anchor='rm', fill='#2c3a4a')
    link(c, (1135, 470), (1190, 470), '6: CreateRow', offset=-46)
    link(c, (885, 540), (800, 540), '7: DeviationReport', dash=True, offset=22)
    link(c, (660, 546), (660, 734), '8: Export(rows, имя файла)', offset=-16)

    c.text(720, 890, 'Сервис анализа агрегирует план по паре «объект — материал» и факт '
                     'по списаниям, расчёт отклонений вынесен в статический '
                     'DeviationCalculator,', size=10.5, fill=MUTED)
    c.text(720, 912, 'что позволяет покрыть формулу модульными тестами. '
                     'Экспортёр выбирается по расширению файла (паттерн «Стратегия»).',
           size=10.5, fill=MUTED)

    c.save(os.path.join(IMG, '09-cooperation-deviation.png'))
    print('ok 9')


# =====================================================================
#  10. Диаграмма состояний: жизненный цикл сметы
# =====================================================================
def diagram_10():
    c = Canvas(1260, 720)
    c.text(630, 30, 'Диаграмма состояний: жизненный цикл сметы', size=20, bold=True)
    c.text(630, 58, 'Состояния соответствуют перечислению EstimateStatus, переходы '
                    'разрешены ролям с правом CanEditEstimates', size=12.5, fill=MUTED)

    def state(cx, cy, label, w=220, h=80):
        c.rect(cx - w / 2, cy - h / 2, w, h, radius=14, fill=STATE_FILL, shadow=True)
        c.text(cx, cy - 8, label, size=13.5, bold=True)
        c.text(cx, cy + 18, 'EstimateStatus', size=10, fill=MUTED)

    c.circle(120, 150, 11)
    state(370, 150, 'Черновик')
    state(690, 150, 'На проверке')
    state(1010, 150, 'Утверждена')
    state(690, 400, 'Архив')

    # начальное состояние -> Черновик
    c.arrow((132, 150), (256, 150), color=LINE, width=1.6)
    c.label(196, 128, 'создать смету', size=10.5)

    c.arrow((480, 150), (578, 150), color=LINE, width=1.6)
    c.label(529, 126, 'отправить', size=10.5)
    c.text(529, 172, 'на проверку', size=10.5, fill=MUTED)

    c.arrow((800, 150), (898, 150), color=LINE, width=1.6)
    c.label(849, 126, 'утвердить', size=10.5)
    c.text(849, 172, '[администратор]', size=10, fill=MUTED)

    # возврат на доработку (дуга сверху)
    c.line((660, 110), (660, 88), color=LINE, width=1.5)
    c.line((660, 88), (400, 88), color=LINE, width=1.5)
    c.line((400, 88), (400, 106), color=LINE, width=1.5)
    c.arrowhead((400, 88), (400, 110), LINE)
    c.label(530, 88, 'вернуть на доработку', size=10.5)

    c.line((1010, 190), (1010, 300), color=LINE, width=1.5)
    c.line((1010, 300), (690, 300), color=LINE, width=1.5)
    c.arrow((690, 300), (690, 354), color=LINE, width=1.5)
    c.label(870, 276, 'архивировать', size=10.5)

    # Черновик -> Архив (аннулирование)
    c.line((370, 190), (370, 470), color='#b04a3a', width=1.4, dash=True)
    c.line((370, 470), (574, 470), color='#b04a3a', width=1.4, dash=True)
    c.line((574, 470), (574, 440), color='#b04a3a', width=1.4, dash=True)
    c.arrowhead((574, 470), (574, 438), '#b04a3a')
    c.label(470, 470, 'аннулировать [администратор]', size=10.5, color='#8c3a2c')

    # Архив -> конечное
    c.circle(690, 560, 14)
    c.circle(690, 560, 8, fill='white')
    c.arrow((690, 442), (690, 542), color=LINE, width=1.6)

    c.text(630, 640, 'Состояние «Архив» исключает смету из расчёта минимального запаса '
                     'и из анализа отклонений, но сохраняет историю документа',
           size=10.5, fill=MUTED)
    c.text(630, 664, 'Все переходы записываются в журнал изменений с указанием '
                     'пользователя и времени', size=10.5, fill=MUTED)

    c.save(os.path.join(IMG, '10-state-estimate.png'))
    print('ok 10')


# =====================================================================
#  11. Диаграмма классов проектирования (паттерны)
# =====================================================================
def cls_box(c, x, y, w, name, stereotype, attrs, ops, fill='#f7faff'):
    head = 24 if stereotype else 0
    head += 26
    h = head + 10 + len(attrs) * 17 + (10 + len(ops) * 17 if ops else 0) + 8

    c.rect(x, y, w, h, radius=3, fill=fill, shadow=True)
    ty = y + 16
    if stereotype:
        c.text(x + w / 2, ty, stereotype, size=10.5, fill=MUTED)
        ty += 20
    c.text(x + w / 2, ty, name, size=12.5, bold=True)
    c.line((x, y + head), (x + w, y + head), color=LINE, width=1.2)

    ly = y + head + 16
    for a in attrs:
        c.text(x + 10, ly, a, size=10.5, anchor='lm', fill='#2c3a4a')
        ly += 17
    ly += 4
    if ops:
        c.line((x, ly - 6), (x + w, ly - 6), color='#c8d2df', width=1.1)
        ly += 8
        for o in ops:
            c.text(x + 10, ly, o, size=10.5, anchor='lm', fill='#2c3a4a')
            ly += 17
    return h


def diagram_11():
    c = Canvas(1440, 1270)
    c.text(720, 30, 'Диаграмма классов проектирования: применённые шаблоны', size=20, bold=True)
    c.text(720, 58, 'MVVM, «Репозиторий», «Единица работы», «Фабрика», «Стратегия», '
                    '«Наблюдатель»', size=13, fill=MUTED)

    cls_box(c, 80, 110, 300, 'ViewModelBase', '«abstract»',
            ['# IsBusy: bool', '# Title: string', '# Error: string'],
            ['+ OnPropertyChanged(name)'], fill='#f3f0fb')
    cls_box(c, 440, 110, 320, 'EstimatesViewModel', '',
            ['- _repository: IEstimateRepository', '- _minStock: IMinStockService',
             '+ Estimates: ObservableCollection'],
            ['+ LoadAsync()', '+ SaveCommand: RelayCommand',
             '+ DeleteCommand: RelayCommand'], fill='#f3f0fb')
    cls_box(c, 860, 110, 250, 'RelayCommand', '«sealed»',
            ['- _execute: Action', '- _canExecute: Func<bool>'],
            ['+ Execute(o)', '+ CanExecute(o)'])

    cls_box(c, 80, 400, 300, 'IEstimateRepository', '«interface»',
            ['+ GetAllWithItemsAsync()', '+ SaveAsync(estimate)',
             '+ AddItemAsync(item)'], [])
    cls_box(c, 480, 400, 320, 'EstimateRepository', '',
            ['- _factory: IDbContextFactory', '- _db: AppDbContext'],
            ['+ GetAllWithItemsAsync()', '+ SaveAsync(estimate)'], fill='#eef7f1')
    cls_box(c, 880, 400, 250, 'AppDbContext', '«Unit of Work»',
            ['+ Estimates: DbSet', '+ EstimateItems: DbSet',
             '+ SaveChangesAsync()'], [], fill='#eef7f1')
    cls_box(c, 1180, 400, 230, 'AppDbContextFactory', '«Factory»',
            ['- _options: DbContextOptions'],
            ['+ CreateDbContext()'], fill='#eef7f1')

    cls_box(c, 80, 700, 300, 'IReportExporter', '«interface»',
            ['+ Format: string', '+ Export(rows, path)'], [])
    cls_box(c, 440, 860, 260, 'XlsxReportExporter', '«Strategy»',
            ['+ Format = "xlsx"'], ['+ Export(rows, path)'], fill='#fdf6e3')
    cls_box(c, 760, 860, 260, 'CsvReportExporter', '«Strategy»',
            ['+ Format = "csv"'], ['+ Export(rows, path)'], fill='#fdf6e3')
    cls_box(c, 1080, 860, 280, 'PdfReportExporter', '«Strategy»',
            ['+ Format = "pdf"'], ['+ Export(rows, path)'], fill='#fdf6e3')

    # обобщение: EstimatesViewModel -> ViewModelBase
    c.line((440, 180), (394, 180), color=LINE, width=1.5)
    triangle(c, (380, 180), 'left')

    # зависимость модели представления от контракта репозитория
    c.arrow((520, 268), (330, 396), color='#2b6cb0', width=1.3, dash=True)
    c.text(452, 330, '«use»', size=10.5, anchor='rm', fill='#2b6cb0')

    # реализация интерфейса репозитория
    c._dash((480, 460), (396, 460), '#2f855a', 1.5, dash=8, gap=5)
    triangle(c, (380, 460), 'left')
    c.text(430, 438, 'реализация', size=9.5, anchor='mm', fill='#2f855a')

    # использование единицы работы
    c.arrow((800, 470), (874, 470), color=LINE, width=1.3, dash=True)
    c.text(837, 448, 'EF Core', size=9.5, anchor='mm', fill=MUTED)

    # фабрика контекста
    c._dash((1295, 482), (1295, 600), '#4a5568', 1.2, dash=7, gap=6)
    c._dash((1295, 600), (640, 600), '#4a5568', 1.2, dash=7, gap=6)
    c.line((640, 600), (640, 544), color='#4a5568', width=1.2)
    c.arrowhead((640, 600), (640, 536), '#4a5568')
    c.text(970, 622, 'создаёт контекст по требованию', size=10, anchor='mm', fill=MUTED)

    # реализации экспортёров
    c._dash((230, 778), (230, 826), '#c07a2c', 1.4, dash=8, gap=5)
    c._dash((230, 826), (1230, 826), '#c07a2c', 1.4, dash=8, gap=5)
    triangle(c, (230, 778), 'up', color='#c07a2c')
    for x in (570, 890, 1230):
        c._dash((x, 826), (x, 856), '#c07a2c', 1.4, dash=8, gap=5)
        triangle(c, (x, 856), 'down', color='#c07a2c')
    c.text(660, 802, 'реализуют IReportExporter', size=10, anchor='mm', fill='#a8641f')

    c.text(720, 1012, 'Паттерны проектирования в модели:', size=13, bold=True)
    legend = [
        'MVVM — разделение разметки (XAML), модели представления и доменной модели; '
        'изменения передаются через ViewModelBase',
        '«Репозиторий» — доступ к данным скрыт за I*Repository, модель представления '
        'не зависит от EF Core',
        '«Единица работы» — AppDbContext накапливает изменения и сохраняет их одной транзакцией',
        '«Фабрика» — IDbContextFactory создаёт короткоживущий контекст на каждую операцию',
        '«Стратегия» — выбор формата экспорта (xlsx / csv / pdf) без изменения модели представления',
        '«Наблюдатель» — INotifyPropertyChanged и ObservableCollection обновляют интерфейс '
        'при изменении данных',
    ]
    y = 1046
    for line in legend:
        c.text(120, y, '•', size=12, anchor='lm')
        c.text(140, y, line, size=11, anchor='lm', fill='#3d4a5c')
        y += 32

    c.save(os.path.join(IMG, '11-classes-design.png'))
    print('ok 11')


# =====================================================================
#  12. Диаграмма компонентов
# =====================================================================
def component(c, x, y, w, h, name, items, fill=HEADER_FILL, stereotype='«component»'):
    c.rect(x, y, w, h, radius=4, fill=fill, shadow=True)
    c.text(x + w / 2, y + 20, name, size=13, bold=True)
    c.text(x + w / 2, y + 40, stereotype, size=10, fill=MUTED)
    c.line((x, y + 54), (x + w, y + 54), color='#c8d2df', width=1.1)
    ly = y + 74
    for item in items:
        c.text(x + 16, ly, '• ' + item, size=10.5, anchor='lm', fill='#2c3a4a')
        ly += 20


def diagram_12():
    c = Canvas(1360, 860)
    c.text(680, 30, 'Диаграмма компонентов программного средства', size=20, bold=True)
    c.text(680, 58, 'Три сборки решения и внешние библиотеки; '
                    'зависимости направлены от приложения к данным', size=13, fill=MUTED)

    component(c, 80, 110, 380, 210, 'StroyControl.App', [
        'Views — окна WPF (XAML)',
        'ViewModels — 14 моделей представления',
        'Converters, Controls, Behaviors',
        'Services — тема, диалоги, PDF',
    ])
    component(c, 80, 400, 380, 230, 'StroyControl.Core', [
        'Entities — 9 сущностей модели',
        'Interfaces — 16 контрактов',
        'Services — экспорт, аутентификация',
        'Enums, Dtos, Security, Exceptions',
    ], fill='#f3f0fb')
    component(c, 590, 400, 360, 230, 'StroyControl.Data', [
        'AppDbContext (EF Core 8)',
        'Repositories — 7 реализаций',
        'Services — Dashboard, MinStock',
        'Configurations, Seed, SchemaPatcher',
    ], fill='#eef7f1')
    component(c, 590, 110, 360, 210, 'Внешние библиотеки', [
        'ClosedXML — экспорт .xlsx',
        'PdfSharpCore — экспорт .pdf',
        'Microsoft.Data.SqlClient',
        'Microsoft.Extensions.DependencyInjection',
    ], fill='#fdf6e3')
    component(c, 1060, 400, 260, 150, 'SQL Server Express', [
        'БД StroyControl',
        '9 таблиц, схема dbo',
    ], fill=DB_FILL, stereotype='«database»')

    c.arrow((270, 320), (270, 394), color=LINE, width=1.6)
    c.label(340, 356, 'ссылается', size=10)

    c.arrow((584, 500), (466, 500), color='#2f855a', width=1.6, dash=True)
    c.label(525, 478, 'реализует', size=10, color='#2f855a')

    c.arrow((770, 320), (770, 394), color=LINE, width=1.6, dash=True)
    c.label(846, 356, 'использует', size=10)

    c.arrow((950, 500), (1054, 500), color=LINE, width=1.6)
    c.label(1002, 478, 'ADO.NET', size=10)

    c.arrow((460, 260), (584, 260), color=LINE, width=1.6, dash=True)
    c.label(522, 238, 'использует', size=10)

    c.text(680, 700, 'Сборка StroyControl.Core не ссылается на WPF и EF Core: '
                     'контракты и доменная модель не зависят от способа хранения данных,',
           size=10.5, fill=MUTED)
    c.text(680, 724, 'поэтому формулы минимального запаса и отклонений покрыты '
                     'модульными тестами без запуска интерфейса и базы данных.',
           size=10.5, fill=MUTED)
    c.text(680, 776, 'Запускаемая сборка — только StroyControl.App '
                     '(dotnet run --project src/StroyControl.App)', size=10.5, fill=MUTED)

    c.save(os.path.join(IMG, '12-components.png'))
    print('ok 12')


# =====================================================================
#  13. Диаграмма размещения
# =====================================================================
def node(c, x, y, w, h, stereotype, name):
    c.rect(x, y, w, h, radius=2, fill='#fbfcfe', outline='#7b8798', width=1.8, shadow=False)
    c.rect(x + 10, y - 14, 150, 26, radius=2, fill='white')
    c.text(x + 85, y - 1, stereotype, size=10.5, fill=MUTED)
    c.text(x + 16, y + 20, name, size=13, bold=True, anchor='lm')


def diagram_13():
    c = Canvas(1300, 780)
    c.text(650, 30, 'Диаграмма размещения системы', size=20, bold=True)
    c.text(650, 58, 'Клиентское рабочее место и сервер базы данных; '
                    'связь по протоколу TDS (порт 1433)', size=13, fill=MUTED)

    node(c, 60, 130, 560, 520, '«device»', 'Рабочая станция пользователя')
    node(c, 780, 130, 460, 420, '«device»', 'Сервер базы данных')

    c.rect(100, 200, 480, 400, radius=4, fill='#eef4fb', outline='#4a5568', width=1.5)
    c.text(340, 226, '«execution environment»', size=10.5, fill=MUTED)
    c.text(340, 248, 'Windows 10 / 11, .NET 8 Desktop Runtime', size=12, bold=True)

    c.rect(140, 300, 400, 110, radius=4, fill='white', shadow=True)
    c.text(340, 324, '«artifact» StroyControl.App.exe', size=12, bold=True)
    c.text(340, 350, 'WPF-приложение: XAML-разметка,', size=10.5, fill='#2c3a4a')
    c.text(340, 370, 'модели представления, ресурсы темы', size=10.5, fill='#2c3a4a')

    c.rect(140, 440, 400, 120, radius=4, fill='white', shadow=True)
    c.text(340, 464, '«artifact» appsettings.json', size=12, bold=True)
    c.text(340, 490, 'строка подключения к SQL Server,', size=10.5, fill='#2c3a4a')
    c.text(340, 510, 'параметры темы и журналирования', size=10.5, fill='#2c3a4a')

    c.rect(830, 200, 360, 290, radius=4, fill='#eef7f1', outline='#4a5568', width=1.5)
    c.text(1010, 226, '«execution environment»', size=10.5, fill=MUTED)
    c.text(1010, 248, 'Microsoft SQL Server Express', size=12, bold=True)

    c.rect(870, 300, 280, 150, radius=10, fill='white', shadow=True)
    c.text(1010, 330, '«artifact» БД StroyControl', size=12, bold=True)
    c.text(1010, 360, 'dbo.projects, dbo.estimates,', size=10.5, fill='#2c3a4a')
    c.text(1010, 380, 'dbo.materials, dbo.deliveries,', size=10.5, fill='#2c3a4a')
    c.text(1010, 400, 'dbo.material_usages, dbo.users', size=10.5, fill='#2c3a4a')
    c.text(1010, 425, '(экземпляр .\\SQLEXPRESS)', size=10, fill=MUTED)

    c.arrow((550, 356), (860, 356), color='#2b6cb0', width=1.7)
    c.label(705, 330, 'TCP 1433, TDS', size=10.5, color='#2b6cb0')
    c.text(705, 384, 'Microsoft.Data.SqlClient', size=10.5, fill=MUTED)
    c.text(690, 402, 'TrustServerCertificate=True', size=10.5, fill=MUTED)

    c.text(650, 690, 'Приложение и база данных могут быть размещены на одной машине '
                     '(учебный вариант .\\SQLEXPRESS) или на разных узлах сети',
           size=10.5, fill=MUTED)
    c.text(650, 714, 'Восстановление соединения выполняется стратегией '
                     'EnableRetryOnFailure; отсутствие сервера не блокирует запуск — '
                     'выводится диагностическое сообщение', size=10.5, fill=MUTED)

    c.save(os.path.join(IMG, '13-deployment.png'))
    print('ok 13')


def main():
    diagram_07()
    diagram_08()
    diagram_09()
    diagram_10()
    diagram_11()
    diagram_12()
    diagram_13()


if __name__ == '__main__':
    main()
