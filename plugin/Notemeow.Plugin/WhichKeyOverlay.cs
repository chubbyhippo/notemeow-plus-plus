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
using System.Runtime.InteropServices;
using Notemeow.Core;

namespace Notemeow.Plugin
{
    internal static unsafe partial class WhichKeyOverlay
    {
        private sealed class Cell(string text, int x, int y, int color)
        {
            public string Text { get; } = text;
            public int X { get; } = x;
            public int Y { get; } = y;
            public int Color { get; } = color;
        }

        private const uint WsPopup = 0x80000000;
        private const uint WsExNoActivate = 0x08000000;
        private const uint WsExToolWindow = 0x00000080;
        private const int SwHide = 0;
        private const int SwShowNa = 8;
        private const uint WmPaint = 0x000F;
        private const int PanelBg = 0x00332B21;
        private const int KeyFg = 0x0060C6F2;
        private const int LabelFg = 0x00E8E8E8;
        private const int TitleFg = 0x0060E260;
        private const int TransparentBkMode = 1;
        private const int DefaultGuiFont = 17;
        private const int MaxRowsPerColumn = 12;
        private const int Gutter = 28;
        private const int KeyGap = 10;
        private const int Padding = 8;

        private const int LogPixelsY = 90;
        private const int PanelPointSize = 10;
        private const uint ClearTypeQuality = 5;

        private static IntPtr classNamePtr;
        private static ushort classAtom;
        private static IntPtr overlay;
        private static List<Cell> cells = [];
        private static IntPtr panelFont;
        private static int panelFontDpi;

        private static IntPtr PanelFont(IntPtr sci)
        {
            int dpi = 96;
            IntPtr hdc = GetDC(sci);
            if (hdc != IntPtr.Zero)
            {
                dpi = GetDeviceCaps(hdc, LogPixelsY);
                _ = ReleaseDC(sci, hdc);
            }
            if (dpi <= 0) dpi = 96;
            if (panelFont != IntPtr.Zero && panelFontDpi == dpi) return panelFont;
            if (panelFont != IntPtr.Zero) DeleteObject(panelFont);
            panelFont = CreateFontW(
                -((PanelPointSize * dpi + 36) / 72),
                0,
                0,
                0,
                400,
                0,
                0,
                0,
                1,
                0,
                0,
                ClearTypeQuality,
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
            if (!EnsureWindow()) return;

            if (!GetClientRect(sci, out RECT rc)) return;
            POINT origin = default;
            ClientToScreen(sci, ref origin);
            int panelWidth = rc.Right - rc.Left;

            int height = Layout(sci, title ?? "", rows, panelWidth);
            MoveWindow(
                overlay,
                origin.X,
                origin.Y + (rc.Bottom - rc.Top) - height,
                panelWidth,
                height,
                false);
            ShowWindow(overlay, SwShowNa);
            InvalidateRect(overlay, IntPtr.Zero, true);
            UpdateWindow(overlay);
        }

        internal static void Hide()
        {
            cells = [];
            if (overlay != IntPtr.Zero) ShowWindow(overlay, SwHide);
        }

        private static int Layout(
            IntPtr sci, string title, IReadOnlyList<WhichKey.Row> rows, int panelWidth)
        {
            var next = new List<Cell>();
            IntPtr hdc = GetDC(IntPtr.Zero);
            int lineHeight = 18;
            var keyWidths = new int[rows.Count];
            var labelWidths = new int[rows.Count];
            try
            {
                IntPtr oldFont = SelectObject(hdc, PanelFont(sci));
                GetTextExtentPoint32W(hdc, "Mg", 2, out SIZE ext);
                lineHeight = ext.Cy + 4;
                for (int i = 0; i < rows.Count; i++)
                {
                    string key = rows[i].Key ?? "";
                    string label = rows[i].Label ?? "";
                    GetTextExtentPoint32W(hdc, key.Length == 0 ? " " : key, Math.Max(key.Length, 1), out ext);
                    keyWidths[i] = ext.Cx;
                    GetTextExtentPoint32W(hdc, label.Length == 0 ? " " : label, Math.Max(label.Length, 1), out ext);
                    labelWidths[i] = ext.Cx;
                }
                SelectObject(hdc, oldFont);
            }
            finally
            {
                _ = ReleaseDC(IntPtr.Zero, hdc);
            }

            int available = Math.Max(panelWidth - Padding * 2, 100);
            int columns = (rows.Count + MaxRowsPerColumn - 1) / MaxRowsPerColumn;
            int rowsPerColumn;
            int[] columnKeyWidth;
            int[] columnWidth;
            while (true)
            {
                rowsPerColumn = (rows.Count + columns - 1) / columns;
                columnKeyWidth = new int[columns];
                columnWidth = new int[columns];
                for (int i = 0; i < rows.Count; i++)
                {
                    int c = i / rowsPerColumn;
                    columnKeyWidth[c] = Math.Max(columnKeyWidth[c], keyWidths[i]);
                }
                int total = 0;
                for (int i = 0; i < rows.Count; i++)
                {
                    int c = i / rowsPerColumn;
                    columnWidth[c] = Math.Max(
                        columnWidth[c], columnKeyWidth[c] + KeyGap + labelWidths[i]);
                }
                for (int c = 0; c < columns; c++) total += columnWidth[c];
                total += (columns - 1) * Gutter;
                if (total <= available || columns == 1) break;
                columns--;
            }

            next.Add(new Cell(title, Padding, Padding, TitleFg));
            int x = Padding;
            for (int c = 0; c < columns; c++)
            {
                int first = c * rowsPerColumn;
                int last = Math.Min(rows.Count, first + rowsPerColumn);
                for (int i = first; i < last; i++)
                {
                    int y = Padding + (i - first + 1) * lineHeight;
                    next.Add(new Cell(rows[i].Key ?? "", x, y, KeyFg));
                    next.Add(new Cell(
                        rows[i].Label ?? "", x + columnKeyWidth[c] + KeyGap, y, LabelFg));
                }
                x += columnWidth[c] + Gutter;
            }
            cells = next;
            return (rowsPerColumn + 1) * lineHeight + Padding * 2;
        }

        private static bool EnsureWindow()
        {
            if (overlay != IntPtr.Zero) return true;
            IntPtr hInstance = GetModuleHandleW(IntPtr.Zero);
            if (classAtom == 0)
            {
                classNamePtr = Marshal.StringToHGlobalUni("NotemeowWhichKey");
                var wc = new WNDCLASSW
                {
                    style = 0,
                    lpfnWndProc = (IntPtr)(delegate* unmanaged<IntPtr, uint, IntPtr, IntPtr, IntPtr>)&WndProc,
                    hInstance = hInstance,
                    hbrBackground = CreateSolidBrush(PanelBg),
                    lpszClassName = classNamePtr,
                };
                classAtom = RegisterClassW(ref wc);
                if (classAtom == 0) return false;
            }
            overlay = CreateWindowExW(
                WsExNoActivate | WsExToolWindow,
                classNamePtr,
                IntPtr.Zero,
                WsPopup,
                0,
                0,
                0,
                0,
                IntPtr.Zero,
                IntPtr.Zero,
                hInstance,
                IntPtr.Zero);
            return overlay != IntPtr.Zero;
        }

        private static void PaintInto(IntPtr hdc)
        {
            GetClientRect(overlay, out RECT rc);
            IntPtr bg = CreateSolidBrush(PanelBg);
            FillRect(hdc, ref rc, bg);
            DeleteObject(bg);

            IntPtr oldFont = SelectObject(
                hdc, panelFont != IntPtr.Zero ? panelFont : GetStockObject(DefaultGuiFont));
            _ = SetBkMode(hdc, TransparentBkMode);
            foreach (Cell cell in cells)
            {
                if (cell.Text.Length == 0) continue;
                _ = SetTextColor(hdc, cell.Color);
                TextOutW(hdc, cell.X, cell.Y, cell.Text, cell.Text.Length);
            }
            SelectObject(hdc, oldFont);
        }

        [UnmanagedCallersOnly]
        private static IntPtr WndProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == WmPaint && hwnd == overlay)
            {
                PAINTSTRUCT ps;
                IntPtr hdc = BeginPaint(hwnd, &ps);
                if (hdc != IntPtr.Zero)
                {
                    PaintInto(hdc);
                    EndPaint(hwnd, &ps);
                }
                return IntPtr.Zero;
            }
            return DefWindowProcW(hwnd, msg, wParam, lParam);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WNDCLASSW
        {
            public uint style;
            public IntPtr lpfnWndProc;
            public int cbClsExtra;
            public int cbWndExtra;
            public IntPtr hInstance;
            public IntPtr hIcon;
            public IntPtr hCursor;
            public IntPtr hbrBackground;
            public IntPtr lpszMenuName;
            public IntPtr lpszClassName;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SIZE
        {
            public int Cx;
            public int Cy;
        }

        [StructLayout(LayoutKind.Sequential)]
        private unsafe struct PAINTSTRUCT
        {
            public IntPtr Hdc;
            public int Erase;
            public RECT Paint;
            public int Restore;
            public int IncUpdate;
            public fixed byte Reserved[32];
        }

        [LibraryImport("kernel32.dll")]
        private static partial IntPtr GetModuleHandleW(IntPtr name);

        [LibraryImport("user32.dll")]
        private static partial ushort RegisterClassW(ref WNDCLASSW wc);

        [LibraryImport("user32.dll")]
        private static partial IntPtr CreateWindowExW(
            uint exStyle,
            IntPtr className,
            IntPtr windowName,
            uint style,
            int x,
            int y,
            int width,
            int height,
            IntPtr parent,
            IntPtr menu,
            IntPtr instance,
            IntPtr param);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool ShowWindow(IntPtr hwnd, int cmd);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool MoveWindow(IntPtr hwnd, int x, int y, int w, int h, [MarshalAs(UnmanagedType.Bool)] bool repaint);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool GetClientRect(IntPtr hwnd, out RECT rc);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool ClientToScreen(IntPtr hwnd, ref POINT pt);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool InvalidateRect(IntPtr hwnd, IntPtr rc, [MarshalAs(UnmanagedType.Bool)] bool erase);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool UpdateWindow(IntPtr hwnd);

        [LibraryImport("user32.dll")]
        private static partial IntPtr BeginPaint(IntPtr hwnd, PAINTSTRUCT* ps);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool EndPaint(IntPtr hwnd, PAINTSTRUCT* ps);

        [LibraryImport("user32.dll")]
        private static partial IntPtr DefWindowProcW(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

        [LibraryImport("user32.dll")]
        private static partial IntPtr GetDC(IntPtr hwnd);

        [LibraryImport("user32.dll")]
        private static partial int ReleaseDC(IntPtr hwnd, IntPtr hdc);

        [LibraryImport("user32.dll")]
        private static partial int FillRect(IntPtr hdc, ref RECT rc, IntPtr brush);

        [LibraryImport("gdi32.dll")]
        private static partial IntPtr CreateSolidBrush(int color);

        [LibraryImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool DeleteObject(IntPtr obj);

        [LibraryImport("gdi32.dll")]
        private static partial int GetDeviceCaps(IntPtr hdc, int index);

        [LibraryImport("gdi32.dll", StringMarshalling = StringMarshalling.Utf16)]
        private static partial IntPtr CreateFontW(
            int height,
            int width,
            int escapement,
            int orientation,
            int weight,
            uint italic,
            uint underline,
            uint strikeOut,
            uint charSet,
            uint outPrecision,
            uint clipPrecision,
            uint quality,
            uint pitchAndFamily,
            string faceName);

        [LibraryImport("gdi32.dll")]
        private static partial IntPtr GetStockObject(int obj);

        [LibraryImport("gdi32.dll")]
        private static partial IntPtr SelectObject(IntPtr hdc, IntPtr obj);

        [LibraryImport("gdi32.dll")]
        private static partial int SetBkMode(IntPtr hdc, int mode);

        [LibraryImport("gdi32.dll")]
        private static partial int SetTextColor(IntPtr hdc, int color);

        [LibraryImport("gdi32.dll", StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool TextOutW(IntPtr hdc, int x, int y, string text, int len);

        [LibraryImport("gdi32.dll", StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool GetTextExtentPoint32W(IntPtr hdc, string text, int len, out SIZE size);
    }
}
