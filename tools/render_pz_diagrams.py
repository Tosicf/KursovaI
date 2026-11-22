"""Генерация чёрно-белых схем для пояснительной записки «СтройКонтроль».

Стиль — как в draw.io: белая заливка, тонкие чёрные линии, скруглённые блоки,
ромбы решений, овалы начала/конца, «палка-фигурки» актёров. Линии проводятся по
свободным «каналам», поэтому не пересекаются — это проверяет Canvas.check().

Запуск:  python tools/render_pz_diagrams.py [имена_функций]
Результат: docs/pz/img/*.png
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from pzcanvas import Canvas  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, 'docs', 'pz', 'img')
os.makedirs(OUT, exist_ok=True)

FS = 13        # кегль пояснений
FT = 12        # кегль текста в фигурах
CANVASES = []


def _done(c, name):
    path = c.save(os.path.join(OUT, name))
    CANVASES.append((name, c))
    return c


def title(c, text, w):
    c.text(w / 2, 30, text, size=19, bold=True)


def table_box(c, cx, cy, w, h, name, rows, size=11, header_size=12):
    """Таблица-сущность: заголовок на сером фоне + строки полей."""
    x, y = cx - w / 2, cy - h / 2
    c.rect(x, y, w, h, radius=0)
    c.d.rectangle([x * c.s, y * c.s, (x + w) * c.s, (y + 30) * c.s], fill='#e6e6e6',
                  outline='#000000', width=max(1, int(round(1.4 * c.s))))
    c.text(cx, y + 16, name, size=header_size, bold=True)
    for i, row in enumerate(rows):
        c.text(x + 10, y + 44 + i * 20, row, size=size, anchor='lm')


# =====================================================================
#  01. Контекстная диаграмма IDEF0
# =====================================================================
def dia_01():
    c = Canvas(1180, 660)
    c._owner_ctx('idef0')
    title(c, 'Контекстная диаграмма работы приложения (IDEF0, уровень A-0)', 1180)

    bx, by, bw, bh = 420, 290, 400, 180
    c.rect(bx, by, bw, bh, width=1.8)
    c.block(bx + bw / 2, by + bh / 2,
            'Управление материальными ресурсами\nи анализ отклонений от сметы\nна строительном объекте\n\nA0',
            bw - 30, size=15, bold=True)

    inputs = [
        (335, 'Сметы и позиции смет (план)'),
        (372, 'Накладные на поступление'),
        (409, 'Акты списания материалов'),
        (446, 'Учётные данные пользователя'),
    ]
    for y, label in inputs:
        c.arrow((240, y), (bx, y))
        c.text(bx - 14, y - 14, label, size=FT, anchor='rm')

    controls = [
        (470, 'Нормативы\nрасхода'),
        (620, 'Роли и права\nдоступа'),
        (765, 'Методика анализа\n«план — факт»'),
    ]
    for x, label in controls:
        c.arrow((x, 240), (x, by))
        c.block(x, 212, label, 140, size=FT)

    mechs = [
        (470, 'Клиент WPF\n(C#, .NET 8)'),
        (620, 'Серверная\nбизнес-логика'),
        (780, 'SQL Server\n(EF Core)'),
    ]
    for x, label in mechs:
        c.arrow((x, by + bh + 50), (x, by + bh))
        c.block(x, by + bh + 78, label, 150, size=FT)

    outputs = [
        (335, 'Отчёт об отклонениях\n«план — факт»'),
        (390, 'Ведомость остатков\nи дефицита'),
        (445, 'Печатные формы\n(смета, чек, отчёт)'),
    ]
    for y, label in outputs:
        c.arrow((bx + bw, y), (bx + bw + 150, y))
        c.block(bx + bw + 290, y, label, 210, size=FT)

    c.text(240, 306, 'Входы', size=FS, bold=True)
    c.text(620, 268, 'Управление', size=FS, bold=True)
    c.text(620, 500, 'Механизмы', size=FS, bold=True)
    c.text(970, 306, 'Выходы', size=FS, bold=True)
    return _done(c, '01-context.png')


# =====================================================================
#  02. Диаграмма бизнес-актёров и их функциональных обязанностей
# =====================================================================
def dia_02():
    c = Canvas(1230, 760)
    c._owner_ctx('actors')
    title(c, 'Диаграмма бизнес-актёров и их функциональных обязанностей', 1230)

    roles = [
        ('Директор', 'контроль бюджета объекта, утверждение смет,\nпросмотр аналитики и отчётов'),
        ('Инженер ПТО', 'формирование и подтверждение смет,\nанализ отклонений, экспорт отчётов'),
        ('Снабженец', 'ведение поставщиков, оформление поступлений,\nзакупка дефицитных материалов'),
        ('Прораб', 'приёмка материалов на объект,\nоформление актов списания'),
        ('Бухгалтер', 'просмотр финансовых разделов,\nвыгрузка отчётов в XLSX, CSV, PDF'),
        ('Администратор', 'управление учётными записями и ролями,\nведение справочников'),
    ]
    y0, step = 110, 100
    for i, (role, duties) in enumerate(roles):
        y = y0 + i * step
        c.actor(80, y - 34, role, size=FT, label_side='below')
        c.rect(190, y - 32, 350, 64, radius=8)
        c.block(365, y, duties, 322, size=11.5)

    sx, sy, sw, sh = 820, 80, 320, 600
    c.rect(sx, sy, sw, sh)
    c.block(sx + sw / 2, sy + 50, 'Информационная\nсистема «СтройКонтроль»', sw - 50, size=15, bold=True)
    for i, line in enumerate(['Клиентское приложение WPF',
                              'Серверная бизнес-логика',
                              'Microsoft SQL Server',
                              'Отчёты: XLSX, CSV, PDF',
                              'Журнал работы в консоли']):
        c.text(sx + sw / 2, sy + 210 + i * 32, line, size=12)

    for i in range(len(roles)):
        y = y0 + i * step
        c.arrow((540, y), (sx, y))

    c.text(365, 90, 'функциональные обязанности', size=11.5, bold=True)
    return _done(c, '02-actors.png')


# =====================================================================
#  03. Деятельность «Оформление поступления материалов»
# =====================================================================
def dia_03():
    c = Canvas(940, 1040)
    c._owner_ctx('activity-delivery')
    title(c, 'Диаграмма деятельности «Оформление поступления материалов»', 940)

    X = 300
    c.shape('term', X, 90, 170, 46, 'Начало', size=FT)
    c.arrow((X, 113), (X, 142))

    c.shape('rect', X, 176, 320, 60, 'Открыть раздел «Поступления»', size=FT)
    c.arrow((X, 206), (X, 242))

    c.shape('diamond', X, 300, 300, 96, 'Право\n«поступления»?', size=12)
    c.arrow((X, 348), (X, 384))
    c.text(X + 18, 380, 'да', size=11)
    c.arrow((X + 150, 300), (620, 300))
    c.text(510, 288, 'нет', size=11)
    c.shape('rect', 750, 300, 220, 58, 'Сообщение об отказе\nв доступе', size=12)
    c.arrow((750, 329), (750, 372))
    c.shape('term', 750, 398, 180, 46, 'Конец (отказ)', size=FT)

    c.shape('rect', X, 420, 340, 62, 'Выбрать поставщика, объект\nи дату документа', size=12)
    c.arrow((X, 451), (X, 486))

    c.shape('rect', X, 522, 340, 66, 'Добавить позиции: материал,\nколичество, цена', size=12)
    c.arrow((X, 555), (X, 594))

    c.shape('diamond', X, 652, 320, 100, 'Все строки\nзаполнены верно?', size=12)
    c.arrow((X, 702), (X, 742))
    c.text(X - 20, 736, 'да', size=11, anchor='rm')
    c.arrow((X + 160, 652), (560, 652))
    c.text(X + 220, 640, 'нет', size=11)
    c.shape('rect', 700, 652, 260, 58, 'Выделить ошибочную\nстроку', size=12)
    c.path([(700, 623), (700, 522), (X + 170, 522)])

    c.shape('rect', X, 776, 380, 64, 'Сохранить документ в транзакции:\nнакладная + остатки материалов', size=12)
    c.arrow((X, 808), (X, 846))

    c.shape('diamond', X, 900, 260, 92, 'Ошибка\nбазы данных?', size=12)
    c.arrow((X, 946), (X, 982))
    c.text(X - 18, 968, 'нет', size=11, anchor='rm')
    c.arrow((X + 130, 900), (560, 900))
    c.text(500, 888, 'да', size=11)
    c.shape('rect', 700, 900, 260, 58, 'Откатить изменения,\nпоказать уведомление', size=12)
    c.arrow((700, 929), (700, 972))
    c.shape('term', 700, 998, 200, 46, 'Конец (ошибка)', size=FT)

    c.shape('term', X, 1008, 170, 46, 'Конец', size=FT)
    return _done(c, '03-activity-delivery.png')


# =====================================================================
#  04. Деятельность «Анализ отклонений от сметы»
# =====================================================================
def dia_04():
    c = Canvas(960, 940)
    c._owner_ctx('activity-deviation')
    title(c, 'Диаграмма деятельности «Анализ отклонений от сметы»', 960)

    X = 340
    c.shape('term', X, 92, 170, 46, 'Начало', size=FT)
    c.arrow((X, 115), (X, 146))

    c.shape('rect', X, 176, 340, 56, 'Выбрать объект и период', size=FT)
    c.arrow((X, 204), (X, 236))

    c.shape('rect', X, 272, 400, 66, 'Загрузить позиции смет и фактические\nсписания материалов', size=12)
    c.arrow((X, 305), (X, 340))

    c.shape('rect', X, 378, 420, 66, 'Для очередной позиции: потребность\nΣ план, факт Σ списаний', size=12)
    c.arrow((X, 411), (X, 446))

    c.shape('rect', X, 484, 420, 66, 'Рассчитать ΔQ, ΔP, ΔS\nи относительное отклонение δ', size=12)
    c.arrow((X, 517), (X, 552))

    c.shape('rect', X, 590, 420, 66, 'Присвоить оценку по порогам\n5 % и 10 %', size=12)
    c.arrow((X, 623), (X, 656))

    c.shape('diamond', X, 712, 280, 96, 'Есть ещё\nпозиции?', size=12)
    c.path([(X - 140, 712), (90, 712), (90, 378), (X - 210, 378)])
    c.text(120, 728, 'да', size=11)
    c.arrow((X, 760), (X, 794))
    c.text(X + 18, 780, 'нет', size=11)

    c.shape('rect', X, 836, 430, 66, 'Сформировать таблицу «план — факт»,\nподытоги по разделам и объекту', size=12)
    c.arrow((X, 869), (X, 900))

    c.shape('diamond', X, 948, 300, 96, 'Нужен экспорт?', size=12)
    c.save(os.path.join(OUT, '04a.png'))
    return None


def dia_04():
    c = Canvas(980, 1000)
    c._owner_ctx('activity-deviation')
    title(c, 'Диаграмма деятельности «Анализ отклонений от сметы»', 980)

    X = 340
    c.shape('term', X, 92, 170, 46, 'Начало', size=FT)
    c.arrow((X, 115), (X, 146))

    c.shape('rect', X, 176, 340, 56, 'Выбрать объект и период', size=FT)
    c.arrow((X, 204), (X, 236))

    c.shape('rect', X, 272, 400, 66, 'Загрузить позиции смет и фактические\nсписания материалов', size=12)
    c.arrow((X, 305), (X, 340))

    c.shape('rect', X, 378, 420, 66, 'Для очередной позиции: потребность\nΣ план, факт Σ списаний', size=12)
    c.arrow((X, 411), (X, 446))

    c.shape('rect', X, 484, 420, 66, 'Рассчитать ΔQ, ΔP, ΔS\nи относительное отклонение δ', size=12)
    c.arrow((X, 517), (X, 552))

    c.shape('rect', X, 590, 420, 66, 'Присвоить оценку по порогам\n5 % и 10 %', size=12)
    c.arrow((X, 623), (X, 656))

    c.shape('diamond', X, 712, 280, 96, 'Есть ещё\nпозиции?', size=12)
    c.path([(X - 140, 712), (90, 712), (90, 378), (X - 210, 378)])
    c.text(120, 730, 'да', size=11)
    c.arrow((X, 760), (X, 794))
    c.text(X + 18, 782, 'нет', size=11)

    c.shape('rect', X, 836, 430, 66, 'Сформировать таблицу «план — факт»,\nподытоги по разделам и объекту', size=12)
    c.arrow((X, 869), (X, 902))

    c.shape('diamond', X, 950, 300, 96, 'Нужен экспорт?', size=12)
    c.arrow((X + 150, 950), (620, 950))
    c.text(520, 938, 'да', size=11)
    c.shape('rect', 760, 950, 250, 58, 'Выгрузить отчёт\nв XLSX, CSV или PDF', size=12)
    c.arrow((X, 998), (X, 1024)) if False else None
    c.path([(760, 979), (760, 1030), (X + 85, 1030)])
    return _done(c, '04-activity-deviation.png')


# =====================================================================
#  05. Варианты использования для инженера ПТО
# =====================================================================
def dia_05():
    c = Canvas(1000, 820)
    c._owner_ctx('usecase-pto')
    title(c, 'Диаграмма вариантов использования для инженера ПТО', 1000)

    c.actor(90, 380, 'Инженер\nПТО', size=12, label_side='below')

    bx, by, bw, bh = 300, 70, 500, 700
    c.rect(bx, by, bw, bh, node=False)
    c.text(bx + bw / 2, by + 26, 'СтройКонтроль', size=14, bold=True)

    ucs = [
        (150, 'Авторизация в системе'),
        (250, 'Просмотр смет и позиций'),
        (350, 'Ведение смет и позиций'),
        (450, 'Подтверждение сметы'),
        (550, 'Анализ отклонений\n«план — факт»'),
        (650, 'Просмотр архива смет'),
        (730, 'Формирование отчёта\nи экспорт'),
    ]
    CX = 470
    for y, label in ucs:
        c.ellipse(CX, y, 130, 38, label, size=11.5)

    for y, _ in ucs:
        c.arrow((126, 396), (CX - 130, y))

    # отношения включения/расширения — по правому каналу
    c.path([(CX + 130, 450), (650, 450), (650, 250), (CX + 130, 250)], dash=True)
    c.text(660, 350, '«include»', size=10.5, anchor='lm')
    c.path([(CX + 130, 730), (700, 730), (700, 550), (CX + 130, 550)], dash=True)
    c.text(710, 640, '«include»', size=10.5, anchor='lm')
    return _done(c, '05-usecase-pto.png')


# =====================================================================
#  06. Варианты использования для склада и администратора
# =====================================================================
def dia_06():
    c = Canvas(1100, 790)
    c._owner_ctx('usecase-store')
    title(c, 'Диаграмма вариантов использования для снабженца, прораба и администратора', 1100)

    c.actor(90, 170, 'Снабженец', size=12, label_side='below')
    c.actor(90, 400, 'Прораб', size=12, label_side='below')
    c.actor(90, 620, 'Администратор', size=12, label_side='below')

    bx, by, bw, bh = 320, 60, 540, 700
    c.rect(bx, by, bw, bh, node=False)
    c.text(bx + bw / 2, by + 26, 'СтройКонтроль', size=14, bold=True)

    ucs = [
        (140, 'Ведение справочника\nпоставщиков'),
        (250, 'Оформление поступления\nматериалов'),
        (360, 'Закупка дефицитных\nматериалов'),
        (480, 'Оформление списания\nматериалов'),
        (600, 'Управление пользователями\nи ролями'),
        (710, 'Просмотр отчётов\nи экспорт'),
    ]
    CX = 520
    for y, label in ucs:
        c.ellipse(CX, y, 140, 40, label, size=11.5)

    for y in (140, 250, 360):
        c.arrow((126, 186), (CX - 140, y))
    c.arrow((126, 416), (CX - 140, 480))
    for y in (600, 710):
        c.arrow((126, 636), (CX - 140, y))

    # включение: экспорт включает просмотр отчётов по смете
    c.path([(CX + 140, 710), (700, 710), (700, 480), (CX + 140, 480)], dash=True)
    c.text(712, 600, '«include»', size=10.5, anchor='lm')
    return _done(c, '06-usecase-store.png')


# =====================================================================
#  07. Структура и схема ресурсов системы
# =====================================================================
def dia_07():
    c = Canvas(1180, 800)
    c._owner_ctx('resources')
    title(c, 'Структура и схема ресурсов системы', 1180)

    bands = [
        (70, 'Клиентский уровень: приложение WPF (MVVM)',
         ['Представления (XAML)', 'Модели представления', 'Диалоги и уведомления',
          'Темы оформления', 'Экспорт отчётов']),
        (240, 'Уровень бизнес-логики: StroyControl.Core',
         ['Сущности предметной области', 'Сервисы расчётов', 'Интерфейсы репозиториев',
          'Правила прав доступа', 'Журнал серверной части']),
        (410, 'Уровень доступа к данным: StroyControl.Data',
         ['AppDbContext', 'Репозитории', 'Перехватчик SQL', 'Инициализация схемы',
          'Демонстрационные данные']),
        (580, 'Хранилище данных',
         ['Microsoft SQL Server', 'База StroyControl', 'Ограничения целостности', 'Резервное копирование']),
    ]
    H = 150
    for y, label, items in bands:
        c.rect(60, y, 1060, H, node=False)
        c.text(76, y + 18, label, size=13, bold=True, anchor='lm')
        n = len(items)
        w = (1000 - (n - 1) * 12) / n
        for i, item in enumerate(items):
            x = 76 + i * (w + 12)
            c.rect(x, y + 40, w, H - 60, radius=6)
            c.block(x + w / 2, y + 40 + (H - 60) / 2, item, w - 14, size=11)

    labels = ['команды и привязка данных', 'запросы LINQ', 'SQL через провайдер']
    for i, y in enumerate((240, 410, 580)):
        for x in (320, 840):
            c.arrow((x, y - 20), (x, y))
        c.text(340, y - 10, labels[i], size=10.5, anchor='lm')
    return _done(c, '07-resources.png')


# =====================================================================
#  08. Схема работы системы (диаграмма последовательности)
# =====================================================================
def dia_08():
    c = Canvas(1160, 1010)
    c._owner_ctx('sequence')
    title(c, 'Схема работы системы: вход в систему и подтверждение сметы', 1160)

    lifes = [(140, 'Пользователь'), (430, 'Клиент (WPF)'),
             (730, 'Серверная логика'), (1010, 'БД SQL Server')]
    top, bottom = 80, 960
    for x, name in lifes:
        c.rect(x - 80, top, 160, 44, radius=6)
        c.text(x, top + 22, name, size=12, bold=True)
        c.line((x, top + 44), (x, bottom), dash=True, width=1.2)

    rows = [
        ('msg', 140, 430, '1. Ввод логина и пароля', False),
        ('msg', 430, 730, '2. AuthenticateAsync', False),
        ('msg', 730, 1010, '3. SELECT users...', False),
        ('msg', 1010, 730, '4. запись: хеш, соль, роль', True),
        ('self', 730, 0, "5. PBKDF2: проверка хеша", False),
        ('msg', 730, 430, '6. сессия: роль и права', True),
        ('msg', 140, 430, '7. открыть «Сметы»', False),
        ('msg', 430, 730, '8. GetEstimatesAsync', False),
        ('msg', 730, 1010, '9. SELECT estimates + items', False),
        ('msg', 1010, 730, '10. набор сущностей', True),
        ('msg', 730, 430, '11. модели для таблицы', True),
        ('self', 430, 0, '12. привязка данных', False),
        ('msg', 140, 430, '13. «Подтвердить смету»', False),
        ('msg', 430, 730, '14. ConfirmAsync', False),
        ('msg', 730, 1010, '15. UPDATE status = Approved', False),
        ('msg', 1010, 730, '16. транзакция зафиксирована', True),
    ]
    y = 160
    for row in rows:
        kind = row[0]
        if kind == 'msg':
            _, x1, x2, label, dashed = row
            c.arrow((x1, y), (x2, y), dash=dashed)
            if x1 < x2:
                c.text(x1 + 12, y - 13, label, size=10.5, anchor='lm')
            else:
                c.text(x1 - 12, y - 13, label, size=10.5, anchor='rm')
        else:
            _, x, _, label, _ = row
            c.path([(x, y), (x + 55, y), (x + 55, y + 18), (x, y + 18)])
            c.text(x + 65, y + 9, label, size=10.5, anchor='lm')
        y += 50
    return _done(c, '08-sequence.png')


# =====================================================================
#  09. Инфологическая модель базы данных
# =====================================================================
def dia_09():
    c = Canvas(1460, 900)
    c._owner_ctx('er')
    title(c, 'Инфологическая модель базы данных', 1460)

    W, H = 240, 140
    boxes = {}

    def ent(key, cx, cy, name, rows):
        table_box(c, cx, cy, W, H, name, rows, size=10, header_size=11.5)
        boxes[key] = (cx, cy, W, H)

    ent('project', 160, 150, 'Project', ['Id (PK)', 'Код, наименование', 'Заказчик, договор', 'Готовность, %'])
    ent('supplier', 760, 150, 'Supplier', ['Id (PK)', 'Наименование', 'УНП, контакты'])
    ent('delivery', 1060, 150, 'Delivery', ['Id (PK)', 'SupplierId (FK)', 'ProjectId (FK)', 'Номер, дата, сумма'])
    ent('user', 1320, 150, 'User', ['Id (PK)', 'Логин, хеш, соль', 'Роль, активность'])

    ent('estimate', 160, 460, 'Estimate', ['Id (PK)', 'ProjectId (FK)', 'Номер, статус', 'Наценка, прибыль'])
    ent('estimate_item', 470, 460, 'EstimateItem', ['Id (PK)', 'EstimateId (FK)', 'MaterialId (FK)', 'Кол-во, цена, раздел'])
    ent('material', 760, 460, 'Material', ['Id (PK)', 'SupplierId (FK)', 'Код, цена, остаток', 'Минимум запаса'])
    ent('delivery_item', 1060, 460, 'DeliveryItem', ['Id (PK)', 'DeliveryId (FK)', 'MaterialId (FK)', 'Количество, цена'])
    ent('usage', 760, 770, 'MaterialUsage', ['Id (PK)', 'ProjectId (FK)', 'MaterialId (FK)', 'Количество, дата'])

    def L(k):
        cx, cy, w, h = boxes[k]
        return cx - w / 2, cy

    def R(k):
        cx, cy, w, h = boxes[k]
        return cx + w / 2, cy

    def T(k):
        cx, cy, w, h = boxes[k]
        return cx, cy - h / 2

    def B(k):
        cx, cy, w, h = boxes[k]
        return cx, cy + h / 2

    def link(points, m_from, m_to):
        c.path(points, arrow=False)
        c.mult_labels(points, m_from, m_to)

    # Project 1 — N Estimate (вертикально)
    link([(160, 220), (160, 390)], '1', 'N')
    # Estimate 1 — N EstimateItem
    link([(280, 460), (350, 460)], '1', 'N')
    # EstimateItem N — 1 Material
    link([(590, 460), (640, 460)], 'N', '1')
    # Material 1 — N Supplier (вертикально)
    link([(760, 390), (760, 220)], 'N', '1')
    # Material 1 — N DeliveryItem
    link([(880, 460), (940, 460)], '1', 'N')
    # Material 1 — N MaterialUsage
    link([(760, 530), (760, 700)], '1', 'N')
    # Delivery 1 — N DeliveryItem
    link([(1060, 220), (1060, 390)], '1', 'N')
    # User 1 — N Delivery
    link([(1200, 150), (1180, 150)], '1', 'N')
    # Project 1 — N Delivery (верхний канал)
    link([(160, 80), (160, 50), (1060, 50), (1060, 80)], '1', 'N')
    # Project 1 — N MaterialUsage (левый и нижний каналы)
    link([(40, 180), (20, 180), (20, 870), (760, 870), (760, 840)], '1', 'N')
    return _done(c, '09-er.png')


# =====================================================================
#  10. Даталогическая модель базы данных
# =====================================================================
def dia_10():
    c = Canvas(1500, 1120)
    c._owner_ctx('db')
    title(c, 'Даталогическая модель базы данных', 1500)

    boxes = {}

    def tbl(key, cx, cy, name, rows):
        w = 250
        h = 54 + 22 * len(rows)
        table_box(c, cx, cy, w, h, name, rows, size=10, header_size=11)
        boxes[key] = (cx, cy, w, h)

    tbl('project', 160, 180, 'projects', [
        'Id — int, PK', 'Code — nvarchar(20)', 'Name — nvarchar(150)',
        'ContractSum — decimal(18,2)', 'ProgressPercent — int'])
    tbl('supplier', 770, 180, 'suppliers', [
        'Id — int, PK', 'Name — nvarchar(150)', 'Unp — nvarchar(12)',
        'Phone, Email — nvarchar(60)'])
    tbl('delivery', 1090, 180, 'deliveries', [
        'Id — int, PK', 'SupplierId — int, FK', 'ProjectId — int, FK',
        'Number — nvarchar(20)', 'Date — datetime2', 'Total — decimal(18,2)'])
    tbl('user', 1350, 180, 'users', [
        'Id — int, PK', 'Login — nvarchar(50), UQ', 'PasswordHash — varbinary(64)',
        'Salt — varbinary(16)', 'Role — int', 'IsActive — bit'])

    tbl('estimate', 160, 560, 'estimates', [
        'Id — int, PK', 'ProjectId — int, FK', 'Number — nvarchar(20)', 'Status — int',
        'MarkupPercent — decimal(5,2)', 'ProfitPercent — decimal(5,2)'])
    tbl('estimate_item', 470, 560, 'estimate_items', [
        'Id — int, PK', 'EstimateId — int, FK', 'MaterialId — int, FK, NULL',
        'Section — nvarchar(120)', 'IsWork — bit', 'PlannedQuantity — decimal(18,3)',
        'PlannedUnitPrice — decimal(18,2)'])
    tbl('material', 770, 560, 'materials', [
        'Id — int, PK', 'SupplierId — int, FK', 'Code — nvarchar(30)',
        'Unit — nvarchar(10)', 'Price — decimal(18,2)',
        'StockQuantity — decimal(18,3)', 'MinStock — decimal(18,3)'])
    tbl('delivery_item', 1090, 560, 'delivery_items', [
        'Id — int, PK', 'DeliveryId — int, FK', 'MaterialId — int, FK',
        'Quantity — decimal(18,3)', 'UnitPrice — decimal(18,2)'])
    tbl('usage', 770, 900, 'material_usages', [
        'Id — int, PK', 'ProjectId — int, FK', 'MaterialId — int, FK',
        'Quantity — decimal(18,3)', 'UsedOn — datetime2', 'Purpose — nvarchar(200)'])

    def L(k):
        cx, cy, w, h = boxes[k]
        return cx - w / 2, cy

    def R(k):
        cx, cy, w, h = boxes[k]
        return cx + w / 2, cy

    def T(k):
        cx, cy, w, h = boxes[k]
        return cx, cy - h / 2

    def B(k):
        cx, cy, w, h = boxes[k]
        return cx, cy + h / 2

    def link(points):
        c.path(points, arrow=False)
        c.mult_labels(points, '1', 'N')

    link([(160, B('project')[1]), (160, T('estimate')[1])])
    link([(160 + 125, 560), (470 - 125, 560)])
    link([(470 + 125, 560), (770 - 125, 560)])
    link([(770, T('material')[1]), (770, B('supplier')[1])])
    link([(770 + 125, 560), (1090 - 125, 560)])
    link([(770, B('material')[1]), (770, T('usage')[1])])
    link([(1090, B('delivery')[1]), (1090, T('delivery_item')[1])])
    link([(1225, 180), (1215, 180)])
    link([(160, 100), (160, 45), (1090, 45), (1090, 80)])
    link([(35, 220), (22, 220), (22, 1060), (770, 1060), (770, 1000)])
    return _done(c, '10-db-model.png')


# =====================================================================
#  11. Структура классов
# =====================================================================
def _tree(c, root_x, root_name, children, cx, w=210, h=40, gap=60, root_w=170):
    """Дерево наследования: корень слева, наследники — в столбце справа."""
    y = 150
    c.rect(root_x - root_w / 2, y - 26, root_w, 52, radius=4)
    c.block(root_x, y, root_name, root_w - 12, size=10.5, bold=True)
    spine_top = y + 26
    last = spine_top + 24 + gap * (len(children) - 1)
    c.line((root_x, spine_top), (root_x, last), width=1.3)
    for i, child in enumerate(children):
        cy = spine_top + 24 + gap * i
        c.rect(cx - w / 2, cy - h / 2, w, h, radius=4)
        c.block(cx, cy, child, w - 12, size=10)
        c.arrow((root_x, cy), (cx - w / 2, cy), head='hollow')
    return last


def dia_11():
    c = Canvas(1400, 830)
    c._owner_ctx('classes')
    title(c, 'Структура классов программного средства', 1400)

    panels = [(40, 400, 'Модель данных (Core)'),
              (490, 400, 'Доступ к данным (Data)'),
              (940, 420, 'Представление (App)')]
    for x, w, label in panels:
        c.rect(x, 60, w, 720, node=False)
        c.text(x + 14, 82, label, size=13, bold=True, anchor='lm')

    # Core
    _tree(c, 130, 'EntityBase\n(abstract)', [
        'Project', 'Estimate', 'EstimateItem', 'Material', 'Supplier',
        'Delivery', 'DeliveryItem', 'MaterialUsage', 'User'], cx=330, w=200, gap=58)

    # Data
    _tree(c, 580, 'RepositoryBase<T>\n(abstract)', [
        'ProjectRepository', 'EstimateRepository', 'MaterialRepository',
        'SupplierRepository', 'DeliveryRepository', 'UserRepository'],
        cx=770, w=200, gap=64)

    # App
    _tree(c, 1030, 'ViewModelBase\n(abstract)', [
        'MainViewModel', 'EstimatesViewModel', 'ArchiveViewModel', 'MaterialsViewModel',
        'DeliveriesViewModel', 'UsagesViewModel', 'DashboardViewModel'],
        cx=1240, w=210, gap=60)
    return _done(c, '11-classes.png')


# =====================================================================
#  Алгоритмы (ГОСТ 19.701)
# =====================================================================
def algo_frame(c, w, title_text):
    title(c, title_text, w)


def dia_12():
    c = Canvas(900, 880)
    c._owner_ctx('algo-calc')
    algo_frame(c, 900, 'Алгоритм расчёта отклонений по позиции сметы')
    X = 300
    c.shape('term', X, 90, 160, 44, 'Начало', size=FT)
    c.arrow((X, 112), (X, 140))
    c.shape('parallelogram', X, 168, 340, 52, 'Qплан, Pплан, Qфакт, Sфакт', size=11.5)
    c.arrow((X, 194), (X, 226))
    c.shape('diamond', X, 274, 260, 88, 'Qфакт = 0?', size=12)
    c.arrow((X, 318), (X, 352))
    c.text(X - 18, 344, 'нет', size=11, anchor='rm')
    c.arrow((X + 130, 274), (560, 274))
    c.text(500, 262, 'да', size=11)
    c.shape('rect', 700, 274, 240, 52, 'Pфакт = 0', size=12)
    c.path([(700, 300), (700, 340), (X + 130, 340)])
    c.shape('rect', X, 378, 380, 52, 'Pфакт = Sфакт / Qфакт', size=11.5)
    c.arrow((X, 404), (X, 436))
    c.shape('rect', X, 470, 420, 66, 'ΔQ = Qфакт − Qплан\nΔP = Pфакт − Pплан,  ΔS = Sфакт − Sплан', size=11)
    c.arrow((X, 503), (X, 536))
    c.shape('diamond', X, 588, 260, 88, 'Sплан = 0?', size=12)
    c.arrow((X, 632), (X, 666))
    c.text(X - 18, 658, 'нет', size=11, anchor='rm')
    c.arrow((X + 130, 588), (560, 588))
    c.text(500, 576, 'да', size=11)
    c.shape('rect', 700, 588, 240, 52, 'δ = 0', size=12)
    c.path([(700, 614), (700, 654), (X + 130, 654)])
    c.shape('rect', X, 692, 380, 52, 'δ = ΔS / Sплан × 100 %', size=11.5)
    c.arrow((X, 718), (X, 750))
    c.shape('rect', X, 786, 420, 66, 'Оценка: |δ| ≥ 10 % — «критично»,\n|δ| ≥ 5 % — «внимание», иначе «норма»', size=11)
    c.arrow((X, 819), (X, 848))
    c.shape('term', X, 872, 160, 44, 'Конец', size=FT)
    return _done(c, '12-algo-calc.png')


def dia_13():
    c = Canvas(960, 940)
    c._owner_ctx('algo-delete')
    algo_frame(c, 960, 'Алгоритм удаления записи')
    X = 300
    c.shape('term', X, 90, 160, 44, 'Начало', size=FT)
    c.arrow((X, 112), (X, 140))
    c.shape('rect', X, 174, 340, 56, 'Выбрать запись в таблице', size=FT)
    c.arrow((X, 202), (X, 234))
    c.shape('diamond', X, 286, 300, 92, 'Есть право\nна удаление?', size=12)
    c.arrow((X, 332), (X, 366))
    c.text(X - 18, 358, 'да', size=11, anchor='rm')
    c.arrow((X + 150, 286), (620, 286))
    c.text(500, 274, 'нет', size=11)
    c.shape('rect', 760, 286, 240, 56, 'Сообщение об отказе\nв доступе', size=11.5)
    c.arrow((760, 314), (760, 356))
    c.shape('term', 760, 382, 190, 44, 'Конец (отказ)', size=FT)

    c.shape('diamond', X, 412, 320, 92, 'Смета\nподтверждена?', size=12)
    c.arrow((X, 458), (X, 492))
    c.text(X - 18, 484, 'нет', size=11, anchor='rm')
    c.arrow((X + 160, 412), (620, 412))
    c.text(500, 400, 'да', size=11)
    c.shape('rect', 760, 412, 250, 56, 'Изменение запрещено:\nсмета зафиксирована', size=11)
    c.arrow((760, 440), (760, 478))
    c.shape('term', 760, 504, 190, 44, 'Конец (отказ)', size=FT)

    c.shape('rect', X, 538, 380, 56, 'Проверить ссылки внешних ключей', size=11.5)
    c.arrow((X, 566), (X, 598))
    c.shape('diamond', X, 650, 320, 92, 'Есть зависимые\nзаписи?', size=12)
    c.arrow((X, 696), (X, 730))
    c.text(X - 18, 722, 'нет', size=11, anchor='rm')
    c.arrow((X + 160, 650), (620, 650))
    c.text(500, 638, 'да', size=11)
    c.shape('rect', 760, 650, 250, 56, 'Показать сообщение\nо связях', size=11.5)
    c.arrow((760, 678), (760, 716))
    c.shape('term', 760, 742, 190, 44, 'Конец (отказ)', size=FT)

    c.shape('rect', X, 776, 380, 56, 'Удалить запись в транзакции', size=FT)
    c.arrow((X, 804), (X, 836))
    c.shape('rect', X, 872, 380, 52, 'Записать операцию в журнал', size=11.5)
    c.arrow((X, 898), (X, 912))
    c.shape('term', X, 932, 160, 44, 'Конец', size=FT)
    return _done(c, '13-algo-delete.png')


def dia_14():
    c = Canvas(880, 900)
    c._owner_ctx('algo-auth')
    algo_frame(c, 880, 'Алгоритм авторизации пользователя')
    X = 290
    c.shape('term', X, 90, 160, 44, 'Начало', size=FT)
    c.arrow((X, 112), (X, 140))
    c.shape('parallelogram', X, 170, 320, 52, 'Логин и пароль', size=11.5)
    c.arrow((X, 196), (X, 228))
    c.shape('diamond', X, 282, 320, 96, 'Пользователь найден\nи активен?', size=11.5)
    c.arrow((X, 330), (X, 364))
    c.text(X - 18, 356, 'да', size=11, anchor='rm')
    c.arrow((X + 160, 282), (600, 282))
    c.text(470, 270, 'нет', size=11)
    c.shape('rect', 720, 282, 240, 56, 'Сообщение об ошибке\nвхода', size=11.5)
    c.arrow((720, 310), (720, 352))
    c.shape('term', 720, 378, 190, 44, 'Конец (отказ)', size=FT)

    c.shape('rect', X, 410, 400, 66, 'Хеш = PBKDF2(пароль, соль,\n100 000 итераций, SHA-256)', size=11)
    c.arrow((X, 443), (X, 476))
    c.shape('diamond', X, 530, 320, 92, 'Хеш совпадает\nс сохранённым?', size=11.5)
    c.arrow((X, 576), (X, 610))
    c.text(X - 18, 602, 'да', size=11, anchor='rm')
    c.arrow((X + 160, 530), (600, 530))
    c.text(470, 518, 'нет', size=11)
    c.shape('rect', 720, 530, 240, 56, 'Журнал: отказ\nво входе', size=11.5)
    c.arrow((720, 558), (720, 600))
    c.shape('term', 720, 626, 190, 44, 'Конец (отказ)', size=FT)

    c.shape('rect', X, 656, 400, 56, 'Создать сессию пользователя', size=11.5)
    c.arrow((X, 684), (X, 716))
    c.shape('rect', X, 752, 400, 56, 'Вычислить права по роли', size=11.5)
    c.arrow((X, 780), (X, 812))
    c.shape('rect', X, 848, 400, 52, 'Записать вход в журнал', size=11.5)
    c.arrow((X, 874), (X, 878))
    c.shape('term', X, 900, 160, 44, 'Конец', size=FT)
    return _done(c, '14-algo-auth.png')


def dia_15():
    c = Canvas(980, 980)
    c._owner_ctx('algo-user')
    algo_frame(c, 980, 'Алгоритм добавления нового пользователя')
    X = 300
    c.shape('term', X, 90, 160, 44, 'Начало', size=FT)
    c.arrow((X, 112), (X, 140))
    c.shape('rect', X, 172, 360, 52, 'Открыть карточку сотрудника', size=11.5)
    c.arrow((X, 198), (X, 230))
    c.shape('rect', X, 272, 400, 66, 'Ввести ФИО, логин, роль,\nпароль и подтверждение', size=11)
    c.arrow((X, 305), (X, 338))
    c.shape('diamond', X, 392, 300, 92, 'Логин\nуникален?', size=12)
    c.arrow((X, 438), (X, 472))
    c.text(X - 18, 464, 'да', size=11, anchor='rm')
    c.arrow((X + 150, 392), (620, 392))
    c.text(500, 380, 'нет', size=11)
    c.shape('rect', 770, 392, 260, 56, 'Подсветить поле,\nсообщение', size=11.5)
    c.path([(770, 364), (770, 272), (X + 200, 272)])

    c.shape('diamond', X, 520, 320, 96, 'Пароль не короче\n8 символов?', size=11.5)
    c.arrow((X, 568), (X, 602))
    c.text(X - 18, 594, 'да', size=11, anchor='rm')
    c.arrow((X + 160, 520), (620, 520))
    c.text(500, 508, 'нет', size=11)
    c.shape('rect', 770, 520, 260, 56, 'Сообщение о слабом\nпароле', size=11.5)
    c.path([(900, 520), (920, 520), (920, 239), (X + 195, 239)])

    c.shape('rect', X, 648, 420, 66, 'Сгенерировать соль,\nвычислить хеш PBKDF2', size=11)
    c.arrow((X, 681), (X, 714))
    c.shape('rect', X, 748, 420, 56, 'Сохранить пользователя в БД', size=11.5)
    c.arrow((X, 776), (X, 808))
    c.shape('diamond', X, 860, 280, 88, 'Ошибка БД?', size=12)
    c.arrow((X, 904), (X, 938))
    c.text(X - 18, 930, 'нет', size=11, anchor='rm')
    c.arrow((X + 140, 860), (620, 860))
    c.text(500, 848, 'да', size=11)
    c.shape('rect', 780, 860, 260, 56, 'Откат, сообщение\nоб ошибке', size=11.5)
    c.arrow((780, 888), (780, 928))
    c.shape('term', 780, 954, 190, 44, 'Конец (ошибка)', size=FT)
    c.shape('term', X, 968, 160, 44, 'Конец', size=FT)
    return _done(c, '15-algo-add-user.png')


def dia_16():
    c = Canvas(1090, 1080)
    c._owner_ctx('algo-estimate')
    algo_frame(c, 1090, 'Алгоритм фиксации и архивации сметы')
    X = 300
    c.shape('term', X, 90, 160, 44, 'Начало', size=FT)
    c.arrow((X, 112), (X, 140))
    c.shape('rect', X, 172, 380, 52, 'Открыть смету, команда «Подтвердить»', size=11)
    c.arrow((X, 198), (X, 232))

    c.shape('diamond', X, 286, 300, 96, 'Статус\n«Черновик»?', size=12)
    c.arrow((X, 334), (X, 366))
    c.text(X - 18, 358, 'да', size=11, anchor='rm')
    c.arrow((X + 150, 286), (585, 286))
    c.text(500, 274, 'нет', size=11)
    c.shape('rect', 700, 286, 230, 56, 'Сообщение: смета уже\nзафиксирована', size=11)
    c.arrow((700, 314), (700, 358))
    c.shape('term', 700, 384, 190, 44, 'Конец (запрет)', size=FT)

    c.shape('diamond', X, 414, 300, 96, 'Есть позиции?', size=12)
    c.arrow((X, 462), (X, 496))
    c.text(X - 18, 488, 'да', size=11, anchor='rm')
    c.arrow((X + 150, 414), (835, 414))
    c.text(520, 402, 'нет', size=11)
    c.shape('rect', 950, 414, 230, 56, 'Сообщение: добавьте\nпозиции сметы', size=11)
    c.arrow((950, 442), (950, 486))
    c.shape('term', 950, 512, 190, 44, 'Конец (запрет)', size=FT)

    c.shape('rect', X, 546, 420, 56, 'ConfirmAsync: статус «Утверждена»', size=11)
    c.arrow((X, 574), (X, 608))
    c.shape('rect', X, 644, 420, 66, 'Запретить правку позиций\nи удаление сметы', size=11)
    c.arrow((X, 677), (X, 712))
    c.shape('rect', X, 748, 420, 52, 'Записать операцию в журнал', size=11.5)
    c.arrow((X, 774), (X, 808))
    c.shape('diamond', X, 862, 320, 96, 'Работы завершены?\n(«В архив»)', size=11.5)
    c.arrow((X, 910), (X, 944))
    c.text(X - 18, 936, 'да', size=11, anchor='rm')
    c.arrow((X + 160, 862), (830, 862))
    c.text(520, 850, 'нет', size=11)
    c.shape('rect', 950, 862, 230, 56, 'Смета остаётся\nв основном списке', size=11)
    c.arrow((950, 890), (950, 934))
    c.shape('term', 950, 960, 190, 44, 'Конец (ожидание)', size=FT)

    c.shape('rect', X, 990, 430, 60, 'ArchiveAsync: статус «Архив»,\nсмета уходит во вкладку «Архив смет»', size=11)
    c.arrow((X, 1020), (X, 1046))
    c.shape('term', X, 1066, 160, 40, 'Конец', size=FT)
    return _done(c, '16-algo-estimate.png')


DIAGRAMS = [dia_01, dia_02, dia_03, dia_04, dia_05, dia_06, dia_07, dia_08,
            dia_09, dia_10, dia_11, dia_12, dia_13, dia_14, dia_15, dia_16]


def main():
    only = sys.argv[1:] or None
    for func in DIAGRAMS:
        if only and func.__name__ not in only:
            continue
        func()
    for name, c in CANVASES:
        issues = c.check()
        print(f'{name}: фигур {len(c.nodes)}, линий {len(c.segments)}, '
              f'{"пересечений нет" if not issues else "ПРОБЛЕМЫ"}')
        for issue in issues[:10]:
            print('   -', issue)


if __name__ == '__main__':
    main()
