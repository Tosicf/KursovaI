# Диаграммы проекта «СтройКонтроль»

Набор проектных диаграмм: исходники в Mermaid (GitHub отрисовывает их автоматически), готовые изображения — в каталоге [`img/`](img), сводный документ — [`../Диаграммы.docx`](../Диаграммы.docx).

| № | Диаграмма | Файл | Изображение |
|---|---|---|---|
| 1 | Варианты использования | [01-use-case.md](01-use-case.md) | [png](img/01-use-case.png) |
| 2 | Анализа: границы, управления, сущности | [02-analysis-classes.md](02-analysis-classes.md) | [png](img/02-analysis-classes.png) |
| 3 | Последовательности: вход в систему | [03-sequence-login.md](03-sequence-login.md) | [png](img/03-sequence-login.png) |
| 4 | Деятельности: оформление списания | [04-activity-usage.md](04-activity-usage.md) | [png](img/04-activity-usage.png) |
| 5 | Деятельности: «Работа со сметой» | [05-activity-estimate.md](05-activity-estimate.md) | [png](img/05-activity-estimate.png) |
| 6 | Автоматов: навигация по разделам | [06-state-navigation.md](06-state-navigation.md) | [png](img/06-state-navigation.png) |
| 7 | Последовательности: смета и минимальный запас (loop / alt / opt) | — | [png](img/07-sequence-estimate.png) |
| 8 | Кооперации: вход в систему | — | [png](img/08-cooperation-login.png) |
| 9 | Кооперации: отчёт об отклонениях | — | [png](img/09-cooperation-deviation.png) |
| 10 | Состояний: жизненный цикл сметы | — | [png](img/10-state-estimate.png) |
| 11 | Классов проектирования и шаблоны | — | [png](img/11-classes-design.png) |
| 12 | Компонентов | — | [png](img/12-components.png) |
| 13 | Размещения | — | [png](img/13-deployment.png) |

Диаграммы 7–13 построены отдельным скриптом `tools/render_report_diagrams.py` и входят в пояснительную записку
[`../Отчет_СтройКонтроль.docx`](../Отчет_СтройКонтроль.docx); они дополняют набор из шести диаграмм до полного
комплекса UML, требуемого методичкой (две диаграммы последовательности, две кооперации, два автомата,
диаграммы классов, компонентов и размещения).

## Как пересобрать изображения

Изображения построены скриптом `tools/render_diagrams.py` (Python + Pillow, шрифт DejaVu Sans с поддержкой кириллицы, 300 dpi):

```bash
python tools/render_diagrams.py
python tools/render_report_diagrams.py
```

Скрипты создают PNG в `docs/diagrams/img/`; первый дополнительно собирает сводный документ `docs/Диаграммы.docx`.
Требуется `pip install pillow python-docx`.

## Пояснительная записка

Документ `docs/Отчет_СтройКонтроль.docx` собирается скриптом `tools/build_report.py`
(структура — по методичке и образцу оглавления: введение, глава 1 — предметная область,
глава 2 — комплекс UML-моделей, глава 3 — генерация программного кода, заключение,
список источников, приложения):

```bash
python tools/build_report.py
```

Оглавление вставлено как поле Word (TOC): при открытии документа оно обновляется
автоматически, при необходимости — `Ctrl+A`, затем `F9` → «Обновить целиком».
