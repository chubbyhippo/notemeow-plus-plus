// Copyright (C) 2026 Chubby Hippo
//
// This program is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the Free
// Software Foundation, either version 3 of the License, or (at your option)
// any later version.
//
// This program is distributed in the hope that it will be useful, but WITHOUT
// ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
// FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for
// more details.
//
// You should have received a copy of the GNU General Public License along
// with this program. If not, see <https://www.gnu.org/licenses/>.
//
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Collections.Generic;
using Notemeow.Core;

namespace Notemeow.Plugin
{
    internal static class WhichKeyOverlay
    {
        private sealed class Cell(string text, int x, int y, int color)
        {
            public string Text { get; } = text;
            public int X { get; } = x;
            public int Y { get; } = y;
            public int Color { get; } = color;
        }

        private sealed class RowMetrics(int lineHeight, int[] keyWidths, int[] labelWidths)
        {
            public int LineHeight { get; } = lineHeight;
            public int[] KeyWidths { get; } = keyWidths;
            public int[] LabelWidths { get; } = labelWidths;
        }

        private sealed class ColumnFit(int columns, int rowsPerColumn, int[] keyWidths, int[] widths)
        {
            public int Columns { get; } = columns;
            public int RowsPerColumn { get; } = rowsPerColumn;
            public int[] KeyWidths { get; } = keyWidths;
            public int[] Widths { get; } = widths;
        }

        private const int PanelBg = 0x00332B21;
        private const int KeyFg = 0x0060C6F2;
        private const int LabelFg = 0x00E8E8E8;
        private const int TitleFg = 0x0060E260;
        private const int MaxRowsPerColumn = 12;
        private const int Gutter = 28;
        private const int KeyGap = 10;
        private const int Padding = 8;
        private const int MinPanelWidth = 100;
        private const int FallbackLineHeight = 18;
        private const int PanelPointSize = 10;
        private const int RegularWeight = 400;

        private static readonly OverlayWindow Window = new(
            "NotemeowWhichKey",
            Win32.WsExNoActivate | Win32.WsExToolWindow,
            PanelBg,
            PaintInto);

        private static List<Cell> cells = [];
        private static IntPtr panelFont;
        private static int panelFontDpi;

        private static IntPtr PanelFont(IntPtr sci)
        {
            int dpi = Win32.DpiOf(sci);
            if (panelFont != IntPtr.Zero && panelFontDpi == dpi) return panelFont;
            if (panelFont != IntPtr.Zero) Win32.DeleteObject(panelFont);
            panelFont = Win32.CreateFontW(
                -((PanelPointSize * dpi + 36) / 72),
                0,
                0,
                0,
                RegularWeight,
                0,
                0,
                0,
                1,
                0,
                0,
                Win32.ClearTypeQuality,
                0,
                "Segoe UI");
            panelFontDpi = dpi;
            return panelFont;
        }

        internal static void Show(IntPtr sci, string title, IReadOnlyList<WhichKey.Row> rows)
        {
            if (rows == null || rows.Count == 0)
            {
                Hide();
                return;
            }
            if (!Window.Ensure()) return;
            if (!Win32.TryGetScreenRect(sci, out Win32.RECT area)) return;

            int height = Layout(sci, title ?? "", rows, area.Width);
            Window.ShowAt(area.Left, area.Bottom - height, area.Width, height);
        }

        internal static void Hide()
        {
            cells = [];
            Window.Hide();
        }

        private static int Layout(
            IntPtr sci, string title, IReadOnlyList<WhichKey.Row> rows, int panelWidth)
        {
            RowMetrics metrics = Measure(sci, rows);
            ColumnFit fit = FitColumns(
                metrics, rows.Count, Math.Max(panelWidth - Padding * 2, MinPanelWidth));
            cells = PlaceCells(title, rows, metrics, fit);
            return (fit.RowsPerColumn + 1) * metrics.LineHeight + Padding * 2;
        }

        private static RowMetrics Measure(IntPtr sci, IReadOnlyList<WhichKey.Row> rows)
        {
            var keyWidths = new int[rows.Count];
            var labelWidths = new int[rows.Count];
            int lineHeight = FallbackLineHeight;
            IntPtr hdc = Win32.GetDC(IntPtr.Zero);
            try
            {
                IntPtr oldFont = Win32.SelectObject(hdc, PanelFont(sci));
                Win32.GetTextExtentPoint32W(hdc, "Mg", 2, out Win32.SIZE ext);
                lineHeight = ext.Cy + 4;
                for (int i = 0; i < rows.Count; i++)
                {
                    keyWidths[i] = TextWidth(hdc, rows[i].Key);
                    labelWidths[i] = TextWidth(hdc, rows[i].Label);
                }
                Win32.SelectObject(hdc, oldFont);
            }
            finally
            {
                _ = Win32.ReleaseDC(IntPtr.Zero, hdc);
            }
            return new RowMetrics(lineHeight, keyWidths, labelWidths);
        }

        private static int TextWidth(IntPtr hdc, string text)
        {
            string measured = string.IsNullOrEmpty(text) ? " " : text;
            Win32.GetTextExtentPoint32W(hdc, measured, measured.Length, out Win32.SIZE ext);
            return ext.Cx;
        }

        private static ColumnFit FitColumns(RowMetrics metrics, int rowCount, int available)
        {
            int columns = (rowCount + MaxRowsPerColumn - 1) / MaxRowsPerColumn;
            while (true)
            {
                int rowsPerColumn = (rowCount + columns - 1) / columns;
                var columnKeyWidth = new int[columns];
                var columnWidth = new int[columns];
                for (int i = 0; i < rowCount; i++)
                {
                    int c = i / rowsPerColumn;
                    columnKeyWidth[c] = Math.Max(columnKeyWidth[c], metrics.KeyWidths[i]);
                }
                for (int i = 0; i < rowCount; i++)
                {
                    int c = i / rowsPerColumn;
                    columnWidth[c] = Math.Max(
                        columnWidth[c], columnKeyWidth[c] + KeyGap + metrics.LabelWidths[i]);
                }
                int total = (columns - 1) * Gutter;
                for (int c = 0; c < columns; c++) total += columnWidth[c];
                if (total <= available || columns == 1)
                {
                    return new ColumnFit(columns, rowsPerColumn, columnKeyWidth, columnWidth);
                }
                columns--;
            }
        }

        private static List<Cell> PlaceCells(
            string title, IReadOnlyList<WhichKey.Row> rows, RowMetrics metrics, ColumnFit fit)
        {
            var placed = new List<Cell> { new(title, Padding, Padding, TitleFg) };
            int x = Padding;
            for (int c = 0; c < fit.Columns; c++)
            {
                int first = c * fit.RowsPerColumn;
                int last = Math.Min(rows.Count, first + fit.RowsPerColumn);
                for (int i = first; i < last; i++)
                {
                    int y = Padding + (i - first + 1) * metrics.LineHeight;
                    placed.Add(new Cell(rows[i].Key ?? "", x, y, KeyFg));
                    placed.Add(new Cell(
                        rows[i].Label ?? "", x + fit.KeyWidths[c] + KeyGap, y, LabelFg));
                }
                x += fit.Widths[c] + Gutter;
            }
            return placed;
        }

        private static void PaintInto(IntPtr hdc)
        {
            IntPtr oldFont = Win32.SelectObject(
                hdc,
                panelFont != IntPtr.Zero ? panelFont : Win32.GetStockObject(Win32.DefaultGuiFont));
            _ = Win32.SetBkMode(hdc, Win32.TransparentBkMode);
            foreach (Cell cell in cells)
            {
                if (cell.Text.Length == 0) continue;
                _ = Win32.SetTextColor(hdc, cell.Color);
                Win32.TextOutW(hdc, cell.X, cell.Y, cell.Text, cell.Text.Length);
            }
            Win32.SelectObject(hdc, oldFont);
        }
    }
}
