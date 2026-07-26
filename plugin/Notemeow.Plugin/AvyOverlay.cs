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
    internal static unsafe partial class AvyOverlay
    {
        internal sealed class Label(string text, int x, int y)
        {
            public string Text { get; } = text;
            public int X { get; } = x;
            public int Y { get; } = y;
        }

        private const uint WsPopup = 0x80000000;
        private const uint WsExLayered = 0x00080000;
        private const uint WsExTransparent = 0x00000020;
        private const uint WsExNoActivate = 0x08000000;
        private const uint WsExToolWindow = 0x00000080;
        private const int SwHide = 0;
        private const int SwShowNa = 8;
        private const uint WmPaint = 0x000F;
        private const uint LwaColorKey = 1;
        private const int ColorKey = 0x00010101;
        private static int LabelBg => NppApi.BgrFromRgb(Rc.OverlayColor());
        private static int LabelFg => NppApi.BgrFromRgb(Rc.OverlayTextColor());
        private const int TransparentBkMode = 1;
        private const int BoldWeight = 700;
        private const uint ClearTypeQuality = 5;
        private const uint FixedPitch = 1;

        private static IntPtr classNamePtr;
        private static ushort classAtom;
        private static IntPtr overlay;
        private static List<Label> current = [];
        private static int lineHeight = 14;
        private static int boxColor = LabelBg;
        private static IntPtr labelFont;
        private static int labelFontLineHeight;

        private static IntPtr LabelFont()
        {
            if (labelFont != IntPtr.Zero && labelFontLineHeight == lineHeight) return labelFont;
            if (labelFont != IntPtr.Zero) DeleteObject(labelFont);
            labelFont = CreateFontW(
                -(lineHeight * 3 / 4),
                0,
                0,
                0,
                BoldWeight,
                0,
                0,
                0,
                1,
                0,
                0,
                ClearTypeQuality,
                FixedPitch,
                "Consolas");
            labelFontLineHeight = lineHeight;
            return labelFont;
        }

        internal static void Show(IntPtr sci, List<Label> labels, int height)
        {
            Show(sci, labels, height, LabelBg);
        }

        internal static void Show(IntPtr sci, List<Label> labels, int height, int box)
        {
            current = labels ?? [];
            boxColor = box;
            if (height > 0) lineHeight = height;
            if (!EnsureWindow()) return;

            if (!GetClientRect(sci, out RECT rc)) return;
            POINT origin = default;
            ClientToScreen(sci, ref origin);
            int w = rc.Right - rc.Left;
            int h = rc.Bottom - rc.Top;
            MoveWindow(overlay, origin.X, origin.Y, w, h, false);
            ShowWindow(overlay, SwShowNa);
            InvalidateRect(overlay, IntPtr.Zero, true);
            UpdateWindow(overlay);
        }

        internal static void Hide()
        {
            current = [];
            if (overlay != IntPtr.Zero) ShowWindow(overlay, SwHide);
        }

        private static bool EnsureWindow()
        {
            if (overlay != IntPtr.Zero) return true;
            IntPtr hInstance = GetModuleHandleW(IntPtr.Zero);
            if (classAtom == 0)
            {
                classNamePtr = Marshal.StringToHGlobalUni("NotemeowAvyOverlay");
                var wc = new WNDCLASSW
                {
                    style = 0,
                    lpfnWndProc = (IntPtr)(delegate* unmanaged<IntPtr, uint, IntPtr, IntPtr, IntPtr>)&WndProc,
                    hInstance = hInstance,
                    hbrBackground = CreateSolidBrush(ColorKey),
                    lpszClassName = classNamePtr,
                };
                classAtom = RegisterClassW(ref wc);
                if (classAtom == 0) return false;
            }
            overlay = CreateWindowExW(
                WsExLayered | WsExTransparent | WsExNoActivate | WsExToolWindow,
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
            if (overlay == IntPtr.Zero) return false;
            SetLayeredWindowAttributes(overlay, ColorKey, 0, LwaColorKey);
            return true;
        }

        private static void PaintInto(IntPtr hdc)
        {
            GetClientRect(overlay, out RECT rc);
            IntPtr bg = CreateSolidBrush(ColorKey);
            FillRect(hdc, ref rc, bg);
            DeleteObject(bg);

            IntPtr oldFont = SelectObject(hdc, LabelFont());
            _ = SetBkMode(hdc, TransparentBkMode);
            IntPtr box = CreateSolidBrush(boxColor);
            foreach (Label lb in current)
            {
                string text = lb.Text ?? "";
                if (text.Length == 0) continue;
                GetTextExtentPoint32W(hdc, text, text.Length, out SIZE ext);
                int boxH = Math.Max(ext.Cy, lineHeight);
                var r = new RECT
                {
                    Left = lb.X,
                    Top = lb.Y,
                    Right = lb.X + ext.Cx + 4,
                    Bottom = lb.Y + boxH,
                };
                FillRect(hdc, ref r, box);
                _ = SetTextColor(hdc, LabelFg);
                TextOutW(hdc, lb.X + 2, lb.Y, text, text.Length);
            }
            DeleteObject(box);
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
        private static partial bool SetLayeredWindowAttributes(IntPtr hwnd, int key, byte alpha, uint flags);

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
        private static partial int FillRect(IntPtr hdc, ref RECT rc, IntPtr brush);

        [LibraryImport("gdi32.dll")]
        private static partial IntPtr CreateSolidBrush(int color);

        [LibraryImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool DeleteObject(IntPtr obj);

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
