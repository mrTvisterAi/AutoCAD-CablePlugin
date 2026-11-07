using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CablePlugin.Core.Models;
using ClosedXML.Excel;

namespace CablePlugin.Core.Services
{
    /// <summary>
    /// Экспорт кабельных линий в Excel (ClosedXML, без установленного Excel).
    /// </summary>
    public class ExcelExportService
    {
        /// <summary>
        /// Экспорт в файл .xlsx.
        /// Лист «Линии» — сводка, лист «Участки» — разбивка по способам прокладки.
        /// </summary>
        public void Export(IEnumerable<CableLine> lines, string filePath)
        {
            if (lines == null)
                throw new ArgumentNullException(nameof(lines));
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Не указан путь к файлу.", nameof(filePath));

            var list = lines.Where(l => l != null).ToList();

            using (var wb = new XLWorkbook())
            {
                WriteLinesSheet(wb, list);
                WriteSegmentsSheet(wb, list);
                WriteSummarySheet(wb, list);

                var dir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                wb.SaveAs(filePath);
            }
        }

        private static void WriteLinesSheet(XLWorkbook wb, List<CableLine> list)
        {
            var ws = wb.Worksheets.Add("Линии");

            var headers = new[]
            {
                "№",
                "Группа",
                "От",
                "До",
                "Марка",
                "Жилы",
                "Сечение, мм²",
                "Кол-во кабелей",
                "Длина геом., м",
                "Запас, %",
                "Доп. длина, м",
                "Длина итог, м",
                "P, кВт",
                "cos φ",
                "U, В",
                "Фазы",
                "Ток, А",
                "ΔU, %",
                "АВ",
                "Handle"
            };

            for (int c = 0; c < headers.Length; c++)
                ws.Cell(1, c + 1).Value = headers[c];

            StyleHeader(ws, 1, headers.Length);

            int row = 2;
            int n = 1;
            foreach (var line in list)
            {
                var cable = line.Cable ?? new CableInfo();
                ws.Cell(row, 1).Value = n++;
                ws.Cell(row, 2).Value = line.GroupName ?? "";
                ws.Cell(row, 3).Value = line.From ?? "";
                ws.Cell(row, 4).Value = line.To ?? "";
                ws.Cell(row, 5).Value = cable.Mark ?? "";
                ws.Cell(row, 6).Value = cable.Cores;
                ws.Cell(row, 7).Value = cable.Section;
                ws.Cell(row, 8).Value = cable.CableCount;
                ws.Cell(row, 9).Value = line.GeometricLength;
                ws.Cell(row, 10).Value = line.ReservePercent;
                ws.Cell(row, 11).Value = line.AdditionalLength;
                ws.Cell(row, 12).Value = line.TotalLength;
                if (line.PowerKw.HasValue)
                    ws.Cell(row, 13).Value = line.PowerKw.Value;
                ws.Cell(row, 14).Value = line.CosPhi;
                ws.Cell(row, 15).Value = line.Voltage;
                ws.Cell(row, 16).Value = line.Phases;
                ws.Cell(row, 17).Value = line.Current;
                ws.Cell(row, 18).Value = line.VoltageDropPercent;
                ws.Cell(row, 19).Value = line.RecommendedBreaker ?? "";
                ws.Cell(row, 20).Value = line.PolylineHandle ?? "";
                row++;
            }

            ws.Columns().AdjustToContents(1, 40);
            ws.SheetView.FreezeRows(1);
        }

        private static void WriteSegmentsSheet(XLWorkbook wb, List<CableLine> list)
        {
            var ws = wb.Worksheets.Add("Участки");

            var headers = new[]
            {
                "№ линии",
                "Группа",
                "От",
                "До",
                "Марка",
                "Жилы×сечение",
                "Способ прокладки",
                "Длина участка, м",
                "∅ трубы, мм",
                "Авто ∅",
                "Примечание"
            };

            for (int c = 0; c < headers.Length; c++)
                ws.Cell(1, c + 1).Value = headers[c];

            StyleHeader(ws, 1, headers.Length);

            int row = 2;
            int lineNo = 1;
            foreach (var line in list)
            {
                var cable = line.Cable ?? new CableInfo();
                var segs = line.Segments ?? new List<CableSegment>();

                if (segs.Count == 0)
                {
                    ws.Cell(row, 1).Value = lineNo;
                    ws.Cell(row, 2).Value = line.GroupName ?? "";
                    ws.Cell(row, 3).Value = line.From ?? "";
                    ws.Cell(row, 4).Value = line.To ?? "";
                    ws.Cell(row, 5).Value = cable.Mark ?? "";
                    ws.Cell(row, 6).Value = cable.DisplayName;
                    ws.Cell(row, 7).Value = "—";
                    ws.Cell(row, 8).Value = line.TotalLength;
                    row++;
                }
                else
                {
                    foreach (var s in segs)
                    {
                        ws.Cell(row, 1).Value = lineNo;
                        ws.Cell(row, 2).Value = line.GroupName ?? "";
                        ws.Cell(row, 3).Value = line.From ?? "";
                        ws.Cell(row, 4).Value = line.To ?? "";
                        ws.Cell(row, 5).Value = cable.Mark ?? "";
                        ws.Cell(row, 6).Value = $"{cable.Cores}х{cable.Section}";
                        ws.Cell(row, 7).Value = s.LayingMethod ?? "";
                        ws.Cell(row, 8).Value = s.Length;
                        if (s.PipeDiameter.HasValue)
                            ws.Cell(row, 9).Value = s.PipeDiameter.Value;
                        ws.Cell(row, 10).Value = s.IsPipeDiameterAuto ? "да" : "нет";
                        ws.Cell(row, 11).Value = s.Note ?? "";
                        row++;
                    }
                }

                lineNo++;
            }

            ws.Columns().AdjustToContents(1, 40);
            ws.SheetView.FreezeRows(1);
        }

        private static void WriteSummarySheet(XLWorkbook wb, List<CableLine> list)
        {
            var ws = wb.Worksheets.Add("Сводка");

            // По марке + сечению
            ws.Cell(1, 1).Value = "Сводка по марке и сечению";
            ws.Range(1, 1, 1, 5).Merge().Style.Font.SetBold();

            var h2 = new[] { "Марка", "Жилы", "Сечение, мм²", "Кол-во линий", "Сумма длин, м" };
            for (int c = 0; c < h2.Length; c++)
                ws.Cell(2, c + 1).Value = h2[c];
            StyleHeader(ws, 2, h2.Length);

            var byCable = list
                .GroupBy(l => new
                {
                    Mark = l.Cable?.Mark ?? "",
                    Cores = l.Cable?.Cores ?? 0,
                    Section = l.Cable?.Section ?? 0
                })
                .OrderBy(g => g.Key.Mark)
                .ThenBy(g => g.Key.Cores)
                .ThenBy(g => g.Key.Section);

            int row = 3;
            foreach (var g in byCable)
            {
                ws.Cell(row, 1).Value = g.Key.Mark;
                ws.Cell(row, 2).Value = g.Key.Cores;
                ws.Cell(row, 3).Value = g.Key.Section;
                ws.Cell(row, 4).Value = g.Count();
                ws.Cell(row, 5).Value = g.Sum(x => x.TotalLength);
                row++;
            }

            row += 2;
            ws.Cell(row, 1).Value = "Сводка по способам прокладки (участки)";
            ws.Range(row, 1, row, 3).Merge().Style.Font.SetBold();
            row++;

            var h3 = new[] { "Способ прокладки", "Сумма длин участков, м", "Σ ∅ трубы (строк)" };
            for (int c = 0; c < h3.Length; c++)
                ws.Cell(row, c + 1).Value = h3[c];
            StyleHeader(ws, row, h3.Length);
            row++;

            var byMethod = list
                .SelectMany(l => l.Segments ?? Enumerable.Empty<CableSegment>())
                .GroupBy(s => s.LayingMethod ?? "")
                .OrderBy(g => g.Key);

            foreach (var g in byMethod)
            {
                ws.Cell(row, 1).Value = g.Key;
                ws.Cell(row, 2).Value = g.Sum(s => s.Length);
                ws.Cell(row, 3).Value = g.Count(s => s.PipeDiameter.HasValue);
                row++;
            }

            row += 2;
            ws.Cell(row, 1).Value = "Сводка по диаметрам труб/гофры";
            ws.Range(row, 1, row, 2).Merge().Style.Font.SetBold();
            row++;

            ws.Cell(row, 1).Value = "∅, мм";
            ws.Cell(row, 2).Value = "Сумма длин, м";
            StyleHeader(ws, row, 2);
            row++;

            var byPipe = list
                .SelectMany(l => l.Segments ?? Enumerable.Empty<CableSegment>())
                .Where(s => s.PipeDiameter.HasValue)
                .GroupBy(s => s.PipeDiameter.Value)
                .OrderBy(g => g.Key);

            foreach (var g in byPipe)
            {
                ws.Cell(row, 1).Value = g.Key;
                ws.Cell(row, 2).Value = g.Sum(s => s.Length);
                row++;
            }

            ws.Columns().AdjustToContents(1, 40);
        }

        private static void StyleHeader(IXLWorksheet ws, int row, int colCount)
        {
            var range = ws.Range(row, 1, row, colCount);
            range.Style.Font.SetBold();
            range.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#4472C4"));
            range.Style.Font.SetFontColor(XLColor.White);
            range.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        }
    }
}
