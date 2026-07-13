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
    internal static unsafe class WhichKeyOverlay
    {
        private const uint WsPopup = 0x80000000;
        private const uint WsExNoActivate = 0x08000000;
        private const uint WsExToolWindow = 0x00000080;
        private const int SwHide = 0;
        private const int SwShowNa = 8;
        private const int PanelBg = 0x00332B21;
        private const int KeyFg = 0x0060C6F2;
        private const int LabelFg = 0x00E8E8E8;
        private const int TitleFg = 0x0060E260;
        private const int TransparentBkMode = 1;
        private const int DefaultGuiFont = 17;
        private const int ColumnWidth = 240;
        private const int Padding = 6;

        private static IntPtr classNamePtr;
        private static ushort classAtom;
        private static IntPtr overlay;
        private static string currentTitle = "";
        private static IReadOnlyList<WhichKey.Row> currentRows = new List<WhichKey.Row>();

        internal static void Show(IntPtr sci, string title, IReadOnlyList<WhichKey.Row> rows)
        {
            currentTitle = title ?? "";
            currentRows = rows ?? new List<WhichKey.Row>();
            if (currentRows.Count == 0)
            {
                Hide();
                return;
            }
            if (!EnsureWindow()) return;

            RECT rc;
            if (!GetClientRect(sci, out rc)) return;
            POINT origin = default;
            ClientToScreen(sci, ref origin);
            int width = rc.Right - rc.Left;

            int lineHeight = MeasureLineHeight();
            int columns = Math.Max(1, Math.Min(width / ColumnWidth, currentRows.Count));
            int rowsPerColumn = (currentRows.Count + columns - 1) / columns;
            int height = (rowsPerColumn + 1) * lineHeight + Padding * 2;

            MoveWindow(
                overlay,
                origin.X,
                origin.Y + (rc.Bottom - rc.Top) - height,
                width,
                height,
                false);
            ShowWindow(overlay, SwShowNa);
            Paint(lineHeight, rowsPerColumn);
        }

        internal static void Hide()
        {
            currentRows = new List<WhichKey.Row>();
            if (overlay != IntPtr.Zero) ShowWindow(overlay, SwHide);
        }

        private static int MeasureLineHeight()
        {
            IntPtr hdc = GetDC(IntPtr.Zero);
            if (hdc == IntPtr.Zero) return 16;
            try
            {
                IntPtr oldFont = SelectObject(hdc, GetStockObject(DefaultGuiFont));
                SIZE ext;
                GetTextExtentPoint32W(hdc, "Mg", 2, out ext);
                SelectObject(hdc, oldFont);
                return ext.Cy + 2;
            }
            finally
            {
                ReleaseDC(IntPtr.Zero, hdc);
            }
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

        private static void Paint(int lineHeight, int rowsPerColumn)
        {
            if (overlay == IntPtr.Zero) return;
            IntPtr hdc = GetDC(overlay);
            if (hdc == IntPtr.Zero) return;
            try
            {
                RECT rc;
                GetClientRect(overlay, out rc);
                IntPtr bg = CreateSolidBrush(PanelBg);
                FillRect(hdc, ref rc, bg);
                DeleteObject(bg);

                IntPtr oldFont = SelectObject(hdc, GetStockObject(DefaultGuiFont));
                SetBkMode(hdc, TransparentBkMode);

                SetTextColor(hdc, TitleFg);
                TextOutW(hdc, Padding, Padding, currentTitle, currentTitle.Length);

                for (int i = 0; i < currentRows.Count; i++)
                {
                    WhichKey.Row row = currentRows[i];
                    int column = i / rowsPerColumn;
                    int rowInColumn = i % rowsPerColumn;
                    int x = Padding + column * ColumnWidth;
                    int y = Padding + (rowInColumn + 1) * lineHeight;
                    string key = row.Key ?? "";
                    string label = " " + (row.Label ?? "");
                    SetTextColor(hdc, KeyFg);
                    TextOutW(hdc, x, y, key, key.Length);
                    SIZE ext;
                    GetTextExtentPoint32W(hdc, "SPC", 3, out ext);
                    SetTextColor(hdc, LabelFg);
                    TextOutW(hdc, x + ext.Cx, y, label, label.Length);
                }
                SelectObject(hdc, oldFont);
            }
            finally
            {
                ReleaseDC(overlay, hdc);
            }
        }

        [UnmanagedCallersOnly]
        private static IntPtr WndProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
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

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandleW(IntPtr name);

        [DllImport("user32.dll")]
        private static extern ushort RegisterClassW(ref WNDCLASSW wc);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateWindowExW(
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

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hwnd, int cmd);

        [DllImport("user32.dll")]
        private static extern bool MoveWindow(IntPtr hwnd, int x, int y, int w, int h, bool repaint);

        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr hwnd, out RECT rc);

        [DllImport("user32.dll")]
        private static extern bool ClientToScreen(IntPtr hwnd, ref POINT pt);

        [DllImport("user32.dll")]
        private static extern IntPtr DefWindowProcW(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

        [DllImport("user32.dll")]
        private static extern int FillRect(IntPtr hdc, ref RECT rc, IntPtr brush);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateSolidBrush(int color);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr obj);

        [DllImport("gdi32.dll")]
        private static extern IntPtr GetStockObject(int obj);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr obj);

        [DllImport("gdi32.dll")]
        private static extern int SetBkMode(IntPtr hdc, int mode);

        [DllImport("gdi32.dll")]
        private static extern int SetTextColor(IntPtr hdc, int color);

        [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
        private static extern bool TextOutW(IntPtr hdc, int x, int y, string text, int len);

        [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
        private static extern bool GetTextExtentPoint32W(IntPtr hdc, string text, int len, out SIZE size);
    }
}
