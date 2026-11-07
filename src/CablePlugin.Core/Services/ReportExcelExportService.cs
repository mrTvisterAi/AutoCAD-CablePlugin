using System;
using System.Collections.Generic;
using System.IO;
using CablePlugin.Core.Reports;
using ClosedXML.Excel;

namespace CablePlugin.Core.Services
{
    /// <summary>
    /// Экспорт отчётов этапа 4 в Excel.
    /// </summary>
    public class ReportExcelExportService
    {
        public void ExportJournal(IList<JournalRow> rows, string filePath)
        {
            using (var wb = new XLWorkbook())
            {
                var ws = wb.Worksheets.Add("Кабельный журнал");
                var headers = new[]
                {
                    "№", "Группа", "От", "До", "Марка", "Жилы", "Сечение, мм²", "Кол-во",
                    "Длина, м", "Прокладка", "∅ трубы", "Ток, А", "ΔU, %", "АВ", "Примечание"
                };
                WriteHeader(ws, headers);

                int r = 2;
                foreach (var row in rows)
                {
                    ws.Cell(r, 1).Value = row.Number;
                    ws.Cell(r, 2).Value = row.GroupName;
                    ws.Cell(r, 3).Value = row.From;
                    ws.Cell(r, 4).Value = row.To;
                    ws.Cell(r, 5).Value = row.CableMark;
                    ws.Cell(r, 6).Value = row.Cores;
                    ws.Cell(r, 7).Value = row.Section;
                    ws.Cell(r, 8).Value = row.CableCount;
                    ws.Cell(r, 9).Value = row.Length;
                    ws.Cell(r, 10).Value = row.LayingMethods;
                    ws.Cell(r, 11).Value = row.PipeDiameters;
                    ws.Cell(r, 12).Value = row.Current;
                    ws.Cell(r, 13).Value = row.VoltageDropPercent;
                    ws.Cell(r, 14).Value = row.Breaker;
                    ws.Cell(r, 15).Value = row.Note;
                    r++;
                }

                Finish(ws, headers.Length);
                Save(wb, filePath);
            }
        }

        public void ExportBom(BillOfMaterials bom, string filePath)
        {
            using (var wb = new XLWorkbook())
            {
                var wsC = wb.Worksheets.Add("Кабели");
                WriteHeader(wsC, new[] { "Марка", "Жилы", "Сечение, мм²", "Кол-во линий", "Сумма длин, м" });
                int r = 2;
                foreach (var row in bom.Cables)
                {
                    wsC.Cell(r, 1).Value = row.Mark;
                    wsC.Cell(r, 2).Value = row.Cores;
                    wsC.Cell(r, 3).Value = row.Section;
                    wsC.Cell(r, 4).Value = row.LineCount;
                    wsC.Cell(r, 5).Value = row.TotalLength;
                    r++;
                }
                Finish(wsC, 5);

                var wsP = wb.Worksheets.Add("Трубы и гофра");
                WriteHeader(wsP, new[] { "Способ прокладки", "∅, мм", "Сумма длин, м", "Число участков" });
                r = 2;
                foreach (var row in bom.Pipes)
                {
                    wsP.Cell(r, 1).Value = row.LayingMethod;
                    if (row.Diameter.HasValue)
                        wsP.Cell(r, 2).Value = row.Diameter.Value;
                    wsP.Cell(r, 3).Value = row.TotalLength;
                    wsP.Cell(r, 4).Value = row.SegmentCount;
                    r++;
                }
                Finish(wsP, 4);

                Save(wb, filePath);
            }
        }

        public void ExportPanel(PanelReport panel, string filePath)
        {
            using (var wb = new XLWorkbook())
            {
                var ws = wb.Worksheets.Add("Щит");
                ws.Cell(1, 1).Value = $"Щит: {panel.PanelName}";
                ws.Cell(1, 1).Style.Font.SetBold().Font.FontSize = 14;
                ws.Cell(2, 1).Value = $"Коэффициент спроса: {panel.DemandFactor:0.###}";
                ws.Cell(3, 1).Value = $"Σ I = {panel.SumCurrent:0.##} А;  Iспрос = {panel.DemandCurrent:0.##} А";
                ws.Cell(4, 1).Value = $"Σ P = {panel.SumPowerKw:0.###} кВт;  Pспрос = {panel.DemandPowerKw:0.###} кВт";

                var headers = new[]
                {
                    "№", "Обозначение", "Куда", "Марка", "Жилы", "Сечение", "Длина, м",
                    "Ток, А", "АВ", "ΔU, %", "P, кВт"
                };
                for (int c = 0; c < headers.Length; c++)
                    ws.Cell(6, c + 1).Value = headers[c];
                StyleHeaderRow(ws, 6, headers.Length);

                int r = 7;
                foreach (var f in panel.Feeders)
                {
                    ws.Cell(r, 1).Value = f.Number;
                    ws.Cell(r, 2).Value = f.Designation;
                    ws.Cell(r, 3).Value = f.To;
                    ws.Cell(r, 4).Value = f.CableMark;
                    ws.Cell(r, 5).Value = f.Cores;
                    ws.Cell(r, 6).Value = f.Section;
                    ws.Cell(r, 7).Value = f.Length;
                    ws.Cell(r, 8).Value = f.Current;
                    ws.Cell(r, 9).Value = f.Breaker;
                    ws.Cell(r, 10).Value = f.VoltageDropPercent;
                    if (f.PowerKw.HasValue)
                        ws.Cell(r, 11).Value = f.PowerKw.Value;
                    r++;
                }

                ws.Columns().AdjustToContents(1, 40);
                Save(wb, filePath);
            }
        }

        private static void WriteHeader(IXLWorksheet ws, string[] headers)
        {
            for (int c = 0; c < headers.Length; c++)
                ws.Cell(1, c + 1).Value = headers[c];
            StyleHeaderRow(ws, 1, headers.Length);
        }

        private static void StyleHeaderRow(IXLWorksheet ws, int row, int colCount)
        {
            var range = ws.Range(row, 1, row, colCount);
            range.Style.Font.SetBold();
            range.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#4472C4"));
            range.Style.Font.SetFontColor(XLColor.White);
        }

        private static void Finish(IXLWorksheet ws, int colCount)
        {
            ws.Columns().AdjustToContents(1, 40);
            ws.SheetView.FreezeRows(1);
        }

        private static void Save(XLWorkbook wb, string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Путь к файлу не задан.");
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            wb.SaveAs(filePath);
        }
    }
}
