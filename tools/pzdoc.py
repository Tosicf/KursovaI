"""Оформление пояснительной записки: стили, заголовки, рисунки, таблицы, листинги.

Повторяет оформление примера ПЗ: Times New Roman 14 пт, интервал 1,5, абзацный
отступ 1,25 см, поля 3 / 1,5 / 2 / 2 см, номера страниц внизу по центру,
подписи «Рисунок 2.1 – …» по центру, «Таблица 2.1 – …» слева.
"""
import os

from docx import Document
from docx.enum.table import WD_ROW_HEIGHT_RULE, WD_TABLE_ALIGNMENT
from docx.enum.text import WD_ALIGN_PARAGRAPH, WD_BREAK
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from docx.shared import Cm, Pt, RGBColor

FONT = 'Times New Roman'
MONO = 'Courier New'

ALIGN = {'justify': WD_ALIGN_PARAGRAPH.JUSTIFY,
         'left': WD_ALIGN_PARAGRAPH.LEFT,
         'center': WD_ALIGN_PARAGRAPH.CENTER,
         'right': WD_ALIGN_PARAGRAPH.RIGHT}


def _font(style, size=14, bold=False, italic=False):
    style.font.name = FONT
    style.font.size = Pt(size)
    style.font.bold = bold
    style.font.italic = italic
    rpr = style.element.get_or_add_rPr()
    rfonts = rpr.find(qn('w:rFonts'))
    if rfonts is None:
        rfonts = OxmlElement('w:rFonts')
        rpr.append(rfonts)
    for attr in ('w:ascii', 'w:hAnsi', 'w:cs', 'w:eastAsia'):
        rfonts.set(qn(attr), FONT)


class PZ:
    def __init__(self, img_dir):
        self.img = img_dir
        self.doc = Document()
        self._setup()

    # ---------------------------------------------------------------- база
    def _setup(self):
        doc = self.doc
        s = doc.sections[0]
        s.page_width, s.page_height = Cm(21), Cm(29.7)
        s.left_margin, s.right_margin = Cm(3), Cm(1.5)
        s.top_margin, s.bottom_margin = Cm(2), Cm(2)

        normal = doc.styles['Normal']
        _font(normal)
        pf = normal.paragraph_format
        pf.line_spacing = Pt(18)
        pf.space_before = Pt(0)
        pf.space_after = Pt(0)
        pf.first_line_indent = Cm(1.25)
        pf.alignment = ALIGN['justify']

        for name, before, after in (('Heading 1', 0, 12), ('Heading 2', 12, 6),
                                    ('Heading 3', 12, 6)):
            st = doc.styles[name]
            _font(st, 14, bold=True)
            st.font.color.rgb = RGBColor(0, 0, 0)
            pf = st.paragraph_format
            pf.first_line_indent = Cm(1.25)
            pf.line_spacing = Pt(18)
            pf.space_before = Pt(before)
            pf.space_after = Pt(after)
            pf.keep_with_next = True
            pf.alignment = ALIGN['center'] if name == 'Heading 1' else ALIGN['left']

        h1pf = doc.styles['Heading 1'].paragraph_format
        h1pf.page_break_before = True
        h1pf.space_before = Pt(0)

        s.different_first_page_header_footer = True
        footer = s.footer.paragraphs[0]
        footer.alignment = ALIGN['center']
        footer.paragraph_format.first_line_indent = Cm(0)
        fld = OxmlElement('w:fldSimple')
        fld.set(qn('w:instr'), 'PAGE')
        run = OxmlElement('w:r')
        rpr = OxmlElement('w:rPr')
        rfonts = OxmlElement('w:rFonts')
        for attr in ('w:ascii', 'w:hAnsi', 'w:cs'):
            rfonts.set(qn(attr), FONT)
        rpr.append(rfonts)
        sz = OxmlElement('w:sz')
        sz.set(qn('w:val'), '24')
        rpr.append(sz)
        run.append(rpr)
        t = OxmlElement('w:t')
        t.text = '1'
        run.append(t)
        fld.append(run)
        footer._p.append(fld)

        settings = doc.settings.element
        upd = OxmlElement('w:updateFields')
        upd.set(qn('w:val'), 'true')
        settings.append(upd)

    # ---------------------------------------------------------------- текст
    def p(self, text, indent=True, align='justify', size=14, bold=False, italic=False,
          before=0, after=0, spacing=None):
        par = self.doc.add_paragraph()
        par.alignment = ALIGN[align]
        pf = par.paragraph_format
        pf.first_line_indent = Cm(1.25) if indent else Cm(0)
        pf.space_before = Pt(before)
        pf.space_after = Pt(after)
        pf.line_spacing = Pt(18) if spacing is None else spacing
        run = par.add_run(text)
        run.font.name = FONT
        run.font.size = Pt(size)
        run.bold = bold
        run.italic = italic
        return par

    def _heading(self, text, style):
        par = self.doc.add_paragraph(text, style=style)
        for run in par.runs:
            run.font.name = FONT
            run.font.size = Pt(14)
            run.bold = True
        return par

    def h1(self, text):
        return self._heading(text, 'Heading 1')

    def h2(self, text):
        return self._heading(text, 'Heading 2')

    def h3(self, text):
        return self._heading(text, 'Heading 3')

    def bullets(self, items, size=14):
        for item in items:
            par = self.doc.add_paragraph()
            pf = par.paragraph_format
            pf.first_line_indent = Cm(1.25)
            pf.line_spacing = Pt(18)
            pf.space_after = Pt(0)
            par.alignment = ALIGN['justify']
            run = par.add_run('– ' + item)
            run.font.name = FONT
            run.font.size = Pt(size)

    def numbered(self, items, size=14):
        for i, item in enumerate(items, start=1):
            par = self.doc.add_paragraph()
            pf = par.paragraph_format
            pf.first_line_indent = Cm(1.25)
            pf.line_spacing = Pt(18)
            pf.space_after = Pt(0)
            par.alignment = ALIGN['justify']
            run = par.add_run(f'{i}) {item}')
            run.font.name = FONT
            run.font.size = Pt(size)

    # ---------------------------------------------------------------- рисунки
    def fig(self, name, number, title, width_cm=16.0):
        """Рисунок с подписью «Рисунок 2.1 – Название»."""
        path = os.path.join(self.img, name)
        par = self.doc.add_paragraph()
        par.alignment = ALIGN['center']
        pf = par.paragraph_format
        pf.first_line_indent = Cm(0)
        pf.space_before = Pt(10)
        pf.space_after = Pt(4)
        pf.keep_with_next = True
        par.add_run().add_picture(path, width=Cm(width_cm))
        cap = self.doc.add_paragraph()
        cap.alignment = ALIGN['center']
        pf = cap.paragraph_format
        pf.first_line_indent = Cm(0)
        pf.space_before = Pt(0)
        pf.space_after = Pt(10)
        pf.line_spacing = Pt(18)
        run = cap.add_run(f'Рисунок {number} – {title}')
        run.font.name = FONT
        run.font.size = Pt(14)

    def fig_placeholder(self, number, title, height_cm=7.0, hint=''):
        """Рамка «место для рисунка» с подписью «Рисунок N – Название».

        Используется там, где нужен скриншот работающей программы: подпись и
        номер рисунка проставлены, а изображение вставляется в рамку вручную.
        """
        tbl = self.doc.add_table(rows=1, cols=1)
        tbl.style = 'Table Grid'
        tbl.alignment = WD_TABLE_ALIGNMENT.CENTER
        tbl.autofit = False
        row = tbl.rows[0]
        row.height = Cm(height_cm)
        row.height_rule = WD_ROW_HEIGHT_RULE.AT_LEAST
        cell = row.cells[0]
        cell.width = Cm(16.5)
        par = cell.paragraphs[0]
        par.alignment = ALIGN['center']
        pf = par.paragraph_format
        pf.first_line_indent = Cm(0)
        pf.line_spacing = 1.0
        pf.space_before = Pt(6)
        pf.space_after = Pt(6)
        run = par.add_run(f'(место для рисунка {number} – {title})')
        run.font.name = FONT
        run.font.size = Pt(12)
        run.italic = True
        if hint:
            run.add_break()
            run2 = par.add_run(hint)
            run2.font.name = FONT
            run2.font.size = Pt(10)
            run2.italic = True
        spacer = self.doc.add_paragraph()
        spacer.paragraph_format.first_line_indent = Cm(0)
        spacer.paragraph_format.line_spacing = 1.0
        spacer.paragraph_format.space_after = Pt(0)

        cap = self.doc.add_paragraph()
        cap.alignment = ALIGN['center']
        pf = cap.paragraph_format
        pf.first_line_indent = Cm(0)
        pf.space_before = Pt(0)
        pf.space_after = Pt(10)
        pf.line_spacing = Pt(18)
        run = cap.add_run(f'Рисунок {number} – {title}')
        run.font.name = FONT
        run.font.size = Pt(14)

    # ---------------------------------------------------------------- таблицы
    def _cell(self, cell, text, size=12, bold=False, align='left'):
        cell.text = ''
        par = cell.paragraphs[0]
        par.alignment = ALIGN[align]
        pf = par.paragraph_format
        pf.first_line_indent = Cm(0)
        pf.line_spacing = 1.0
        pf.space_before = Pt(2)
        pf.space_after = Pt(2)
        run = par.add_run(text)
        run.font.name = FONT
        run.font.size = Pt(size)
        run.bold = bold

    def tab(self, number, title, headers, rows, widths=None, size=12, align='left',
            header_align='center'):
        cap = self.doc.add_paragraph()
        cap.alignment = ALIGN['left']
        pf = cap.paragraph_format
        pf.first_line_indent = Cm(0)
        pf.space_before = Pt(10)
        pf.space_after = Pt(4)
        pf.line_spacing = Pt(18)
        pf.keep_with_next = True
        run = cap.add_run(f'Таблица {number} – {title}')
        run.font.name = FONT
        run.font.size = Pt(14)

        tbl = self.doc.add_table(rows=1, cols=len(headers))
        tbl.style = 'Table Grid'
        tbl.alignment = WD_TABLE_ALIGNMENT.CENTER
        tbl.autofit = False
        layout = OxmlElement('w:tblLayout')
        layout.set(qn('w:type'), 'fixed')
        tbl._tbl.tblPr.append(layout)
        for i, header in enumerate(headers):
            self._cell(tbl.rows[0].cells[i], header, size=size, bold=True, align=header_align)
        for row in rows:
            cells = tbl.add_row().cells
            for i, value in enumerate(row):
                self._cell(cells[i], str(value), size=size, align=align)
        if widths:
            for i, w in enumerate(widths):
                tbl.columns[i].width = Cm(w)
            for row in tbl.rows:
                for i, w in enumerate(widths):
                    row.cells[i].width = Cm(w)
        spacer = self.doc.add_paragraph()
        spacer.paragraph_format.space_after = Pt(6)
        spacer.paragraph_format.line_spacing = 1.0
        spacer.paragraph_format.first_line_indent = Cm(0)
        return tbl

    # ---------------------------------------------------------------- листинги
    @staticmethod
    def _wrap_code(line, limit=84):
        """Переносит слишком длинные строки листинга по пробелам."""
        if len(line) <= limit:
            return [line]
        indent = len(line) - len(line.lstrip(' '))
        pad = ' ' * (min(indent + 4, 20))
        out, cur, first = [], line, True
        while len(cur) > limit:
            cut = cur.rfind(' ', 0, limit)
            if cut <= 0:
                cut = limit
            out.append(cur[:cut])
            rest = cur[cut:].lstrip(' ')
            cur = (pad if not first else pad) + rest
            first = False
        out.append(cur)
        return out

    def listing(self, number, title, code):
        cap = self.doc.add_paragraph()
        cap.alignment = ALIGN['left']
        pf = cap.paragraph_format
        pf.first_line_indent = Cm(0)
        pf.space_before = Pt(8)
        pf.space_after = Pt(2)
        pf.line_spacing = Pt(18)
        pf.keep_with_next = True
        run = cap.add_run(f'Листинг {number} – {title}')
        run.font.name = FONT
        run.font.size = Pt(14)
        for raw in code.strip('\n').split('\n'):
            for line in self._wrap_code(raw):
                par = self.doc.add_paragraph()
                pf = par.paragraph_format
                pf.first_line_indent = Cm(0)
                pf.left_indent = Cm(0.3)
                pf.line_spacing = 1.0
                pf.space_before = Pt(0)
                pf.space_after = Pt(0)
                run = par.add_run(line if line.strip() else ' ')
                run.font.name = MONO
                run.font.size = Pt(9.5)
        spacer = self.doc.add_paragraph()
        spacer.paragraph_format.space_after = Pt(6)
        spacer.paragraph_format.line_spacing = 1.0
        spacer.paragraph_format.first_line_indent = Cm(0)

    # ---------------------------------------------------------------- титул и оглавление
    def title_page(self, lines):
        """lines — список (текст, размер, жирный, отступ_после_pt)."""
        for text, size, bold, after in lines:
            par = self.doc.add_paragraph()
            par.alignment = ALIGN['center']
            pf = par.paragraph_format
            pf.first_line_indent = Cm(0)
            pf.space_before = Pt(0)
            pf.space_after = Pt(after)
            pf.line_spacing = 1.0
            run = par.add_run(text)
            run.font.name = FONT
            run.font.size = Pt(size)
            run.bold = bold

    def toc(self, title='СОДЕРЖАНИЕ'):
        par = self.doc.add_paragraph()
        par.alignment = ALIGN['center']
        pf = par.paragraph_format
        pf.first_line_indent = Cm(0)
        pf.space_after = Pt(12)
        run = par.add_run(title)
        run.font.name = FONT
        run.font.size = Pt(14)
        run.bold = True

        par = self.doc.add_paragraph()
        par.paragraph_format.first_line_indent = Cm(0)
        run = par.add_run()
        begin = OxmlElement('w:fldChar')
        begin.set(qn('w:fldCharType'), 'begin')
        instr = OxmlElement('w:instrText')
        instr.set(qn('xml:space'), 'preserve')
        instr.text = 'TOC \\o "1-3" \\h \\z \\u'
        sep = OxmlElement('w:fldChar')
        sep.set(qn('w:fldCharType'), 'separate')
        hint = OxmlElement('w:t')
        hint.text = 'Оглавление обновится при открытии документа (Ctrl+A, затем F9).'
        end = OxmlElement('w:fldChar')
        end.set(qn('w:fldCharType'), 'end')
        for el in (begin, instr, sep, hint, end):
            run._r.append(el)

    def page_break(self):
        self.doc.add_paragraph().add_run().add_break(WD_BREAK.PAGE)

    def save(self, path):
        self.doc.save(path)
        return path
