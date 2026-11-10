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

## Как пересобрать изображения

Изображения построены скриптом `tools/render_diagrams.py` (Python + Pillow, шрифт DejaVu Sans с поддержкой кириллицы, 300 dpi):

```bash
python tools/render_diagrams.py
```

Скрипт создаёт PNG в `docs/diagrams/img/` и документ `docs/Диаграммы.docx`. Требуется `pip install pillow python-docx`.
