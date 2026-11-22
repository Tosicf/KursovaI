"""Чёрно-белые схемы для пояснительной записки «СтройКонтроль» (упрощённые).

Принципы, чтобы схемы читались «как из draw.io» и не выглядели перегруженными:
  * без заголовков внутри картинки — название задаёт подпись рисунка;
  * короткие подписи в блоках (1–3 слова), крупный кегль, много воздуха;
  * минимум ветвлений: только существенные решения;
  * одинаковые размеры однотипных блоков, ровная сетка;
  * линии прокладываются по свободным каналам — Canvas.check() проверяет,
    что они не пересекаются и не выходят за холст.

Запуск:  python tools/render_pz_diagrams.py [имена функций]
Результат: docs/pz/img/*.png
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from pzcanvas import Canvas  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, 'docs', 'pz', 'img')
os.makedirs(OUT, exist_ok=True)

FS = 14        # подписи в блоках
SM = 12        # подписи на стрелках
CANVASES = []


def _done(c, name):
    path = c.save(os.path.join(OUT, name))
    CANVASES.append((name, c))
    return c


# =====================================================================
#  01. Контекстная диаграмма IDEF0
# =====================================================================
def dia_01():
    c = Canvas(1120, 620)
    c._owner_ctx('idef0')

    bx, by, bw, bh = 400, 250, 340, 150
    c.rect(bx, by, bw, bh, width=1.8)
    c.block(bx + bw / 2, by + bh / 2 - 8,
            'Управление материалами\nи анализ отклонений\nот сметы', bw - 60, size=15, bold=True)
    c.text(bx + bw - 14, by + bh - 16, 'A0', size=12, anchor='rm', bold=True)

    for y, label in ((280, 'Сметы'), (320, 'Накладные'),
                     (360, 'Акты списания'), (400, 'Логин и пароль')):
        c.arrow((250, y), (bx, y))
        c.text(bx - 16, y - 14, label, size=FS, anchor='rm')

    for x, label in ((450, 'Нормативы'), (570, 'Права доступа'), (690, 'Методика')):
        c.arrow((x, 195), (x, by))
        c.text(x, 172, label, size=12.5)

    for x, label in ((450, 'Клиент WPF'), (570, 'Бизнес-логика'), (690, 'SQL Server')):
        c.arrow((x, by + bh + 55), (x, by + bh))
        c.text(x, by + bh + 78, label, size=12.5)

    for y, label in ((300, 'Отчёт «план — факт»'), (350, 'Остатки и дефицит'),
                     (400, 'Печатные формы')):
        c.arrow((bx + bw, y), (bx + bw + 110, y))
        c.text(bx + bw + 124, y, label, size=FS, anchor='lm')

    return _done(c, '01-context.png')


# =====================================================================
#  02. Бизнес-актёры и их задачи
# =====================================================================
def dia_02():
    c = Canvas(1040, 540)
    c._owner_ctx('actors')

    roles = [
        ('Бухгалтер', 'бюджет и отчёты по объектам'),
        ('Инженер ПТО', 'сметы и анализ отклонений'),
        ('Снабженец', 'поставщики и поступления'),
        ('Прораб', 'списание материалов'),
        ('Администратор', 'пользователи и справочники'),
    ]
    y0, step = 96, 84
    for i, (role, duty) in enumerate(roles):
        y = y0 + i * step
        c.actor(70, y - 30, role)
        c.rect(300, y - 24, 340, 48, radius=8)
        c.block(470, y, duty, 316, size=SM + 0.5)
        c.arrow((640, y), (740, y))

    c.rect(740, 70, 270, 400)
    c.block(875, 120, 'Информационная\nсистема\n«СтройКонтроль»', 235, size=15, bold=True)
    for i, line in enumerate(['Клиент WPF', 'Бизнес-логика', 'SQL Server']):
        c.text(875, 250 + i * 34, line, size=13)
    return _done(c, '02-actors.png')


# =====================================================================
#  03. Деятельность «Оформление поступления»
# =====================================================================
def dia_03():
    c = Canvas(700, 640)
    c._owner_ctx('activity-delivery')
    X = 280
    c.shape('term', X, 60, 150, 42, 'Начало', size=FS)
    c.arrow((X, 81), (X, 110))
    c.shape('rect', X, 136, 300, 52, 'Открыть «Поступления»', size=FS)
    c.arrow((X, 162), (X, 198))
    c.shape('diamond', X, 240, 260, 84, 'Право есть?', size=FS)
    c.arrow((X, 282), (X, 314))
    c.text(X - 16, 306, 'да', size=SM, anchor='rm')
    c.arrow((X + 130, 240), (465, 240))
    c.text(420, 226, 'нет', size=SM)
    c.shape('rect', 570, 240, 210, 52, 'Отказ в доступе', size=SM)
    c.arrow((570, 266), (570, 302))
    c.shape('term', 570, 322, 170, 40, 'Конец', size=FS)
    c.shape('rect', X, 340, 320, 56, 'Внести накладную\nи строки', size=FS)
    c.arrow((X, 368), (X, 400))
    c.shape('rect', X, 426, 320, 52, 'Сохранить в транзакции', size=12)
    c.arrow((X, 452), (X, 484))
    c.shape('rect', X, 510, 300, 48, 'Записать в журнал', size=FS)
    c.arrow((X, 534), (X, 566))
    c.shape('term', X, 588, 150, 42, 'Конец', size=FS)
    return _done(c, '03-activity-delivery.png')


def dia_04():
    c = Canvas(540, 740)
    c._owner_ctx('activity-deviation')
    X = 300
    c.shape('term', X, 60, 150, 42, 'Начало', size=FS)
    c.arrow((X, 81), (X, 110))
    c.shape('rect', X, 136, 320, 52, 'Выбрать объект и период', size=FS)
    c.arrow((X, 162), (X, 192))
    c.shape('rect', X, 220, 340, 56, 'Загрузить план и факт', size=FS)
    c.arrow((X, 248), (X, 278))
    c.shape('rect', X, 306, 400, 56, 'Рассчитать отклонения\nпо позиции', size=SM + 0.5)
    c.arrow((X, 334), (X, 370))
    c.shape('diamond', X, 412, 260, 84, 'Есть ещё?', size=FS)
    c.path([(X - 130, 412), (95, 412), (95, 306), (X - 200, 306)])
    c.text(120, 430, 'да', size=SM)
    c.arrow((X, 454), (X, 488))
    c.text(X - 16, 470, 'нет', size=SM, anchor='rm')
    c.shape('rect', X, 516, 380, 56, 'Сформировать таблицу', size=FS)
    c.arrow((X, 544), (X, 580))
    c.shape('rect', X, 606, 340, 52, 'Сохранить в XLSX или PDF', size=12)
    c.arrow((X, 632), (X, 666))
    c.shape('term', X, 688, 150, 42, 'Конец', size=FS)
    return _done(c, '04-activity-deviation.png')


# =====================================================================
#  05. Варианты использования для инженера ПТО
# =====================================================================
def dia_05():
    c = Canvas(840, 580)
    c._owner_ctx('usecase-pto')
    c.actor(80, 250, 'Инженер ПТО', label_side='below')

    c.rect(280, 50, 510, 480, node=False)
    c.text(535, 76, 'СтройКонтроль', size=14, bold=True)

    ucs = [(130, 'Вход в систему'), (215, 'Сметы и позиции'),
           (300, 'Подтверждение сметы'), (385, 'Анализ отклонений'),
           (470, 'Отчёты и экспорт')]
    CX, BUS = 490, 210
    for y, label in ucs:
        c.ellipse(CX, y, 140, 34, label, size=SM + 0.5)
        c.arrow((BUS, y), (CX - 140, y))

    c.line((BUS, ucs[0][0]), (BUS, ucs[-1][0]))
    c.line((110, 262), (BUS, 262))

    c.path([(CX + 140, 470), (710, 470), (710, 385), (CX + 140, 385)], dash=True)
    c.text(724, 428, '«include»', size=11, anchor='lm')
    return _done(c, '05-usecase-pto.png')


# =====================================================================
#  06. Варианты использования для склада
# =====================================================================
def dia_06():
    c = Canvas(900, 580)
    c._owner_ctx('usecase-store')
    c.actor(80, 150, 'Снабженец', label_side='below')
    c.actor(80, 400, 'Прораб', label_side='below')

    c.rect(300, 40, 550, 480, node=False)
    c.text(575, 66, 'СтройКонтроль', size=14, bold=True)

    ucs = [(120, 'Поставщики'), (200, 'Поступления'),
           (280, 'Закупка дефицита'), (380, 'Списание материалов'),
           (460, 'Просмотр отчётов')]
    CX, BUS = 530, 225
    for y, label in ucs:
        c.ellipse(CX, y, 130, 32, label, size=SM + 0.5)
        c.arrow((BUS, y), (CX - 130, y))

    c.line((BUS, ucs[0][0]), (BUS, ucs[-1][0]))
    c.line((110, 162), (BUS, 162))
    c.line((110, 412), (BUS, 412))
    return _done(c, '06-usecase-store.png')


def dia_07():
    c = Canvas(1000, 610)
    c._owner_ctx('resources')

    bands = [(60, 'Клиент (WPF)', ['Представления', 'Модели представления', 'Диалоги']),
             (250, 'Бизнес-логика', ['Расчёты', 'Правила доступа', 'Журнал']),
             (440, 'Данные (SQL Server)', ['Репозитории', 'Таблицы БД', 'Резервные копии'])]
    for y, label, items in bands:
        c.rect(50, y, 900, 130, node=False)
        c.text(70, y + 22, label, size=13, bold=True, anchor='lm')
        for i, item in enumerate(items):
            x = 70 + i * 294
            c.rect(x, y + 44, 278, 72, radius=6)
            c.block(x + 139, y + 80, item, 252, size=FS)

    for y in (250, 440):
        for x in (300, 700):
            c.arrow((x, y - 60), (x, y))
    c.text(314, 220, 'команды', size=11, anchor='lm')
    c.text(714, 410, 'запросы', size=11, anchor='lm')
    return _done(c, '07-resources.png')


def dia_08():
    c = Canvas(900, 560)
    c._owner_ctx('sequence')
    lifes = [(120, 'Пользователь'), (360, 'Клиент WPF'),
             (600, 'Бизнес-логика'), (820, 'SQL Server')]
    for x, name in lifes:
        c.rect(x - 75, 60, 150, 40, radius=6)
        c.text(x, 80, name, size=SM, bold=True)
        c.line((x, 100), (x, 500), dash=True, width=1.2)

    msgs = [(120, 360, '1. логин и пароль', 144, False),
            (360, 600, '2. проверка входа', 184, False),
            (600, 820, '3. выборка пользователя', 224, False),
            (820, 600, '4. хеш и роль', 264, True),
            (360, 120, '5. сессия и права', 306, True),
            (120, 360, '6. открыть «Сметы»', 360, False),
            (360, 600, '7. запрос списка', 400, False),
            (600, 820, '8. выборка смет', 440, False),
            (600, 360, '9. таблица для окна', 480, True)]
    for x1, x2, label, y, dashed in msgs:
        c.arrow((x1, y), (x2, y), dash=dashed)
        if x1 < x2:
            c.text(x1 + 12, y - 13, label, size=11, anchor='lm')
        else:
            c.text(x1 - 12, y - 13, label, size=11, anchor='rm')
    return _done(c, '08-sequence.png')


# =====================================================================
#  09. Инфологическая модель базы данных
# =====================================================================
def _entity(c, key, cx, cy, name, rows, w=210, h=86):
    c.rect(cx - w / 2, cy - h / 2, w, h)
    c.text(cx, cy - h / 2 + 16, name, size=12.5, bold=True)
    for i, row in enumerate(rows):
        c.text(cx - w / 2 + 12, cy - h / 2 + 44 + i * 20, row, size=10.5, anchor='lm')
    return (cx, cy, w, h)


def dia_09():
    c = Canvas(1180, 700)
    c._owner_ctx('er')

    b = {}
    b['project'] = _entity(c, 'project', 150, 110, 'Project', ['Id — PK'])
    b['supplier'] = _entity(c, 'supplier', 700, 110, 'Supplier', ['Id — PK'])
    b['delivery'] = _entity(c, 'delivery', 950, 110, 'Delivery', ['Id — PK', 'SupplierId — FK'])
    b['estimate'] = _entity(c, 'estimate', 150, 350, 'Estimate', ['Id — PK', 'ProjectId — FK'])
    b['item'] = _entity(c, 'item', 430, 350, 'EstimateItem', ['Id — PK', 'EstimateId — FK'])
    b['material'] = _entity(c, 'material', 700, 350, 'Material', ['Id — PK', 'SupplierId — FK'])
    b['ditem'] = _entity(c, 'ditem', 950, 350, 'DeliveryItem', ['Id — PK', 'DeliveryId — FK'])
    b['usage'] = _entity(c, 'usage', 700, 590, 'MaterialUsage', ['Id — PK', 'ProjectId — FK'])
    b['user'] = _entity(c, 'user', 420, 590, 'User', ['Id — PK', 'Role'])

    def P(k, side):
        cx, cy, w, h = b[k]
        return {'l': (cx - w / 2, cy), 'r': (cx + w / 2, cy),
                't': (cx, cy - h / 2), 'b': (cx, cy + h / 2)}[side]

    def link(points):
        c.path(points, arrow=False)
        c.mult_labels(points, '1', 'N')

    link([P('project', 'b'), P('estimate', 't')])
    link([P('estimate', 'r'), P('item', 'l')])
    link([P('item', 'r'), P('material', 'l')])
    link([P('supplier', 'b'), P('material', 't')])
    link([P('material', 'r'), P('ditem', 'l')])
    link([P('material', 'b'), P('usage', 't')])
    link([P('delivery', 'b'), P('ditem', 't')])
    c.path([P('supplier', 'r'), P('delivery', 'l')], arrow=False)
    c.mult(816, 98, '1')
    c.mult(834, 98, 'N')
    link([P('project', 'l'), (25, 110), (25, 660), (700, 660), P('usage', 'b')])
    c.path([P('user', 'r'), P('usage', 'l')], arrow=False)
    c.mult(556, 578, '1')
    c.mult(596, 578, 'N')
    return _done(c, '09-er.png')


# =====================================================================
#  10. Даталогическая модель
# =====================================================================
def _table(c, cx, cy, name, fields, w=220):
    h = 40 + 20 * len(fields)
    c.rect(cx - w / 2, cy - h / 2, w, h)
    c.text(cx, cy - h / 2 + 18, name, size=12, bold=True)
    for i, f in enumerate(fields):
        c.text(cx - w / 2 + 12, cy - h / 2 + 40 + i * 20, f, size=10, anchor='lm')
    return (cx, cy, w, h)


def dia_10():
    c = Canvas(1100, 620)
    c._owner_ctx('db')
    b = {}
    b['project'] = _table(c, 140, 110, 'projects', ['Id — PK', 'ProgressPercent'])
    b['estimate'] = _table(c, 140, 330, 'estimates', ['Id — PK', 'ProjectId — FK'])
    b['item'] = _table(c, 420, 330, 'estimate_items', ['Id — PK', 'EstimateId — FK', 'MaterialId — FK'])
    b['material'] = _table(c, 700, 330, 'materials', ['Id — PK', 'SupplierId — FK'])
    b['supplier'] = _table(c, 700, 110, 'suppliers', ['Id — PK', 'Name'])

    b['delivery'] = _table(c, 960, 110, 'deliveries', ['Id — PK', 'SupplierId — FK'])
    b['ditem'] = _table(c, 960, 330, 'delivery_items', ['Id — PK', 'DeliveryId — FK', 'MaterialId — FK'])
    b['usage'] = _table(c, 700, 540, 'material_usages', ['Id — PK', 'ProjectId — FK',
                                                       'CreatedByUserId — FK'])
    b['users'] = _table(c, 420, 540, 'users', ['Id — PK', 'Login, Role'])

    def P(k, side):
        cx, cy, w, h = b[k]
        return {'l': (cx - w / 2, cy), 'r': (cx + w / 2, cy),
                't': (cx, cy - h / 2), 'b': (cx, cy + h / 2)}[side]

    def link(points):
        c.path(points, arrow=False)
        c.mult_labels(points, '1', 'N')

    link([P('project', 'b'), P('estimate', 't')])
    link([P('estimate', 'r'), P('item', 'l')])
    link([P('item', 'r'), P('material', 'l')])
    link([P('supplier', 'b'), P('material', 't')])
    link([P('material', 'r'), P('ditem', 'l')])
    link([P('material', 'b'), P('usage', 't')])
    link([P('delivery', 'b'), P('ditem', 't')])
    link([P('supplier', 't'), (700, 40), (960, 40), P('delivery', 't')])
    link([P('users', 'r'), P('usage', 'l')])
    return _done(c, '10-db-model.png')


# =====================================================================
#  11. Структура классов
# =====================================================================
def _group(c, x, w, title, root, children):
    c.rect(x, 40, w, 356, node=False)
    c.text(x + w / 2, 62, title, size=13, bold=True)
    c.rect(x + 20, 84, w - 40, 46, radius=4)
    c.text(x + w / 2, 107, root, size=12, bold=True)

    bus = x + 42
    cy_last = 180 + 56 * (len(children) - 1)
    c.line((bus, 130), (bus, cy_last), width=1.3)
    for i, child in enumerate(children):
        cy = 180 + 56 * i
        c.rect(x + 68, cy - 22, w - 88, 44, radius=4)
        c.block(x + 68 + (w - 88) / 2, cy, child, w - 110, size=SM)
        c.arrow((bus, cy), (x + 68, cy), head='hollow')


def dia_11():
    c = Canvas(1140, 420)
    c._owner_ctx('classes')
    _group(c, 20, 350, 'Модель данных', 'EntityBase',
           ['Project', 'Estimate', 'Material', 'User'])
    _group(c, 390, 350, 'Доступ к данным', 'RepositoryBase',
           ['ProjectRepository', 'EstimateRepository', 'MaterialRepository'])
    _group(c, 760, 350, 'Представление', 'ViewModelBase',
           ['MainViewModel', 'EstimatesViewModel', 'ArchiveViewModel'])
    return _done(c, '11-classes.png')


# =====================================================================
#  12–16. Алгоритмы
# =====================================================================
def dia_12():
    c = Canvas(650, 720)
    c._owner_ctx('algo-calc')
    X = 260
    c.shape('term', X, 50, 140, 40, 'Начало', size=FS)
    c.arrow((X, 70), (X, 96))
    c.shape('parallelogram', X, 122, 320, 52, 'Qплан, Pплан, Qфакт, Sфакт', size=12)
    c.arrow((X, 148), (X, 180))
    c.shape('diamond', X, 222, 220, 80, 'Qфакт = 0?', size=SM + 0.5)
    c.arrow((X, 262), (X, 292))
    c.text(X - 14, 286, 'нет', size=SM, anchor='rm')
    c.arrow((X + 110, 222), (465, 222))
    c.text(420, 208, 'да', size=SM)
    c.shape('rect', 540, 222, 150, 48, 'Pфакт = 0', size=SM)
    c.path([(540, 246), (540, 320), (420, 320)])
    c.shape('rect', X, 318, 320, 48, 'Pфакт = Sфакт / Qфакт', size=12)
    c.arrow((X, 342), (X, 370))
    c.shape('rect', X, 402, 340, 60, 'Расчёт ΔQ, ΔP, ΔS', size=FS)
    c.arrow((X, 432), (X, 462))
    c.shape('diamond', X, 506, 220, 80, 'Sплан = 0?', size=SM + 0.5)
    c.arrow((X, 546), (X, 576))
    c.text(X - 14, 570, 'нет', size=SM, anchor='rm')
    c.arrow((X + 110, 506), (465, 506))
    c.text(420, 492, 'да', size=SM)
    c.shape('rect', 540, 506, 150, 48, 'δ = 0', size=SM)
    c.path([(540, 530), (540, 600), (430, 600)])
    c.shape('rect', X, 600, 340, 52, 'Оценка отклонения', size=FS)
    c.arrow((X, 626), (X, 656))
    c.shape('term', X, 678, 140, 40, 'Конец', size=FS)
    return _done(c, '12-algo-calc.png')


def dia_13():
    c = Canvas(760, 700)
    c._owner_ctx('algo-delete')
    X = 270
    c.shape('term', X, 50, 140, 40, 'Начало', size=FS)
    c.arrow((X, 70), (X, 96))
    c.shape('rect', X, 122, 300, 48, 'Выбрать запись', size=FS)
    c.arrow((X, 146), (X, 172))
    c.shape('diamond', X, 214, 240, 80, 'Право есть?', size=SM + 0.5)
    c.arrow((X, 254), (X, 288))
    c.text(X - 16, 280, 'да', size=SM, anchor='rm')
    c.arrow((X + 120, 214), (480, 214))
    c.text(430, 200, 'нет', size=SM)
    c.shape('rect', 580, 214, 200, 48, 'Отказ в доступе', size=SM)
    c.arrow((580, 238), (580, 274))
    c.shape('term', 580, 294, 170, 40, 'Конец', size=FS)
    c.shape('rect', X, 314, 320, 48, 'Проверить связи', size=FS)
    c.arrow((X, 338), (X, 364))
    c.shape('diamond', X, 406, 260, 80, 'Связи есть?', size=SM + 0.5)
    c.arrow((X, 446), (X, 480))
    c.text(X - 16, 472, 'нет', size=SM, anchor='rm')
    c.arrow((X + 130, 406), (480, 406))
    c.text(430, 392, 'да', size=SM)
    c.shape('rect', 580, 406, 200, 48, 'Сообщение', size=SM)
    c.arrow((580, 430), (580, 466))
    c.shape('term', 580, 486, 170, 40, 'Конец', size=FS)
    c.shape('rect', X, 508, 300, 48, 'Удалить запись', size=FS)
    c.arrow((X, 532), (X, 558))
    c.shape('rect', X, 584, 300, 48, 'Записать в журнал', size=FS)
    c.arrow((X, 608), (X, 640))
    c.shape('term', X, 662, 140, 40, 'Конец', size=FS)
    return _done(c, '13-algo-delete.png')


def dia_14():
    c = Canvas(720, 680)
    c._owner_ctx('algo-auth')
    X = 250
    c.shape('term', X, 50, 140, 40, 'Начало', size=FS)
    c.arrow((X, 70), (X, 96))
    c.shape('parallelogram', X, 122, 300, 48, 'Логин и пароль', size=12)
    c.arrow((X, 146), (X, 178))
    c.shape('diamond', X, 220, 280, 84, 'Пользователь активен?', size=11.5)
    c.arrow((X, 262), (X, 296))
    c.text(X - 16, 288, 'да', size=SM, anchor='rm')
    c.arrow((X + 140, 220), (460, 220))
    c.text(410, 206, 'нет', size=SM)
    c.shape('rect', 560, 220, 200, 48, 'Ошибка входа', size=SM)
    c.arrow((560, 244), (560, 280))
    c.shape('term', 560, 300, 170, 40, 'Конец', size=FS)
    c.shape('rect', X, 322, 320, 52, 'Хеш PBKDF2', size=FS)
    c.arrow((X, 348), (X, 378))
    c.shape('diamond', X, 420, 260, 84, 'Хеш совпал?', size=SM + 0.5)
    c.arrow((X, 462), (X, 496))
    c.text(X - 16, 488, 'да', size=SM, anchor='rm')
    c.arrow((X + 130, 420), (460, 420))
    c.text(410, 406, 'нет', size=SM)
    c.shape('rect', 560, 420, 200, 48, 'Отказ во входе', size=SM)
    c.arrow((560, 444), (560, 480))
    c.shape('term', 560, 500, 170, 40, 'Конец', size=FS)
    c.shape('rect', X, 524, 320, 48, 'Создать сессию', size=FS)
    c.arrow((X, 548), (X, 576))
    c.shape('rect', X, 602, 320, 48, 'Открыть главное окно', size=12)
    return _done(c, '14-algo-auth.png')


def dia_15():
    c = Canvas(760, 660)
    c._owner_ctx('algo-user')
    X = 270
    c.shape('term', X, 50, 140, 40, 'Начало', size=FS)
    c.arrow((X, 70), (X, 96))
    c.shape('rect', X, 122, 320, 48, 'Открыть карточку', size=FS)
    c.arrow((X, 146), (X, 176))
    c.shape('rect', X, 202, 340, 48, 'Ввести данные', size=FS)
    c.arrow((X, 226), (X, 258))
    c.shape('diamond', X, 300, 260, 84, 'Данные верны?', size=SM + 0.5)
    c.arrow((X, 342), (X, 372))
    c.text(X - 16, 366, 'да', size=SM, anchor='rm')
    c.arrow((X + 130, 300), (505, 300))
    c.text(455, 286, 'нет', size=SM)
    c.shape('rect', 600, 300, 190, 48, 'Сообщение', size=SM)
    c.path([(600, 276), (600, 202), (440, 202)])
    c.shape('rect', X, 398, 320, 48, 'Хеш пароля', size=FS)
    c.arrow((X, 422), (X, 450))
    c.shape('rect', X, 476, 320, 48, 'Сохранить в БД', size=FS)
    c.arrow((X, 500), (X, 528))
    c.shape('rect', X, 554, 320, 48, 'Записать в журнал', size=FS)
    c.arrow((X, 578), (X, 606))
    c.shape('term', X, 628, 140, 40, 'Конец', size=FS)
    return _done(c, '15-algo-add-user.png')


def dia_16():
    c = Canvas(820, 690)
    c._owner_ctx('algo-estimate')
    X = 290
    c.shape('term', X, 50, 140, 40, 'Начало', size=FS)
    c.arrow((X, 70), (X, 96))
    c.shape('rect', X, 122, 340, 48, 'Команда «Подтвердить»', size=12)
    c.arrow((X, 146), (X, 178))
    c.shape('diamond', X, 222, 300, 88, 'Черновик и есть позиции?', size=11.5)
    c.arrow((X, 266), (X, 300))
    c.text(X - 16, 292, 'да', size=SM, anchor='rm')
    c.arrow((X + 150, 222), (520, 222))
    c.text(470, 208, 'нет', size=SM)
    c.shape('rect', 620, 222, 200, 48, 'Сообщение', size=SM)
    c.arrow((620, 246), (620, 282))
    c.shape('term', 620, 302, 170, 40, 'Конец', size=FS)
    c.shape('rect', X, 326, 340, 48, 'Статус «Утверждена»', size=12)
    c.arrow((X, 350), (X, 378))
    c.shape('rect', X, 404, 340, 48, 'Блокировка правки', size=FS)
    c.arrow((X, 428), (X, 456))
    c.shape('rect', X, 482, 340, 48, 'Запись в журнал', size=FS)
    c.arrow((X, 506), (X, 534))
    c.shape('rect', X, 560, 340, 48, 'Статус «Архив»', size=12)
    c.arrow((X, 584), (X, 616))
    c.shape('term', X, 638, 140, 40, 'Конец', size=FS)
    return _done(c, '16-algo-estimate.png')


DIAGRAMS = [dia_01, dia_02, dia_03, dia_04, dia_05, dia_06, dia_07, dia_08,
            dia_09, dia_10, dia_11, dia_12, dia_13, dia_14, dia_15, dia_16]


def main():
    only = sys.argv[1:] or None
    for func in DIAGRAMS:
        if only and func.__name__ not in only:
            continue
        func()
    bad = 0
    for name, c in CANVASES:
        issues = c.check()
        bad += len(issues)
        print(f'{name}: фигур {len(c.nodes)}, линий {len(c.segments)}, '
              f'{"ок" if not issues else "ПРОБЛЕМЫ"}')
        for issue in issues[:10]:
            print('   -', issue)
    print(f'итого замечаний: {bad}')


if __name__ == '__main__':
    main()
