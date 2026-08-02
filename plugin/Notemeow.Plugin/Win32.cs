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
using System.Runtime.InteropServices;

namespace Notemeow.Plugin
{
    internal static unsafe partial class Win32
    {
        internal const uint WsPopup = 0x80000000;
        internal const uint WsExLayered = 0x00080000;
        internal const uint WsExTransparent = 0x00000020;
        internal const uint WsExNoActivate = 0x08000000;
        internal const uint WsExToolWindow = 0x00000080;
        internal const int SwHide = 0;
        internal const int SwShowNa = 8;
        internal const uint WmPaint = 0x000F;
        internal const uint LwaColorKey = 1;
        internal const int TransparentBkMode = 1;
        internal const int DefaultGuiFont = 17;
        internal const int LogPixelsY = 90;
        internal const uint ClearTypeQuality = 5;
        internal const int DefaultDpi = 96;

        internal static int DpiOf(IntPtr hwnd)
        {
            IntPtr hdc = GetDC(hwnd);
            if (hdc == IntPtr.Zero) return DefaultDpi;
            int dpi = GetDeviceCaps(hdc, LogPixelsY);
            _ = ReleaseDC(hwnd, hdc);
            return dpi > 0 ? dpi : DefaultDpi;
        }

        internal static bool TryGetScreenRect(IntPtr hwnd, out RECT screen)
        {
            screen = default;
            if (!GetClientRect(hwnd, out RECT client)) return false;
            POINT origin = default;
            ClientToScreen(hwnd, ref origin);
            screen = new RECT
            {
                Left = origin.X,
                Top = origin.Y,
                Right = origin.X + (client.Right - client.Left),
                Bottom = origin.Y + (client.Bottom - client.Top),
            };
            return true;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct WNDCLASSW
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
        internal struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public readonly int Width => Right - Left;

            public readonly int Height => Bottom - Top;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SIZE
        {
            public int Cx;
            public int Cy;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct PAINTSTRUCT
        {
            public IntPtr Hdc;
            public int Erase;
            public RECT Paint;
            public int Restore;
            public int IncUpdate;
            public fixed byte Reserved[32];
        }

        [LibraryImport("kernel32.dll")]
        internal static partial IntPtr GetModuleHandleW(IntPtr name);

        [LibraryImport("user32.dll")]
        internal static partial ushort RegisterClassW(ref WNDCLASSW wc);

        [LibraryImport("user32.dll")]
        internal static partial IntPtr CreateWindowExW(
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
        internal static partial bool ShowWindow(IntPtr hwnd, int cmd);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool MoveWindow(IntPtr hwnd, int x, int y, int w, int h, [MarshalAs(UnmanagedType.Bool)] bool repaint);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool GetClientRect(IntPtr hwnd, out RECT rc);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool ClientToScreen(IntPtr hwnd, ref POINT pt);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool SetLayeredWindowAttributes(IntPtr hwnd, int key, byte alpha, uint flags);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool InvalidateRect(IntPtr hwnd, IntPtr rc, [MarshalAs(UnmanagedType.Bool)] bool erase);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool UpdateWindow(IntPtr hwnd);

        [LibraryImport("user32.dll")]
        internal static partial IntPtr BeginPaint(IntPtr hwnd, PAINTSTRUCT* ps);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool EndPaint(IntPtr hwnd, PAINTSTRUCT* ps);

        [LibraryImport("user32.dll")]
        internal static partial IntPtr DefWindowProcW(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

        [LibraryImport("user32.dll")]
        internal static partial IntPtr GetDC(IntPtr hwnd);

        [LibraryImport("user32.dll")]
        internal static partial int ReleaseDC(IntPtr hwnd, IntPtr hdc);

        [LibraryImport("user32.dll")]
        internal static partial int FillRect(IntPtr hdc, ref RECT rc, IntPtr brush);

        [LibraryImport("gdi32.dll")]
        internal static partial IntPtr CreateSolidBrush(int color);

        [LibraryImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool DeleteObject(IntPtr obj);

        [LibraryImport("gdi32.dll")]
        internal static partial int GetDeviceCaps(IntPtr hdc, int index);

        [LibraryImport("gdi32.dll", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial IntPtr CreateFontW(
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
        internal static partial IntPtr GetStockObject(int obj);

        [LibraryImport("gdi32.dll")]
        internal static partial IntPtr SelectObject(IntPtr hdc, IntPtr obj);

        [LibraryImport("gdi32.dll")]
        internal static partial int SetBkMode(IntPtr hdc, int mode);

        [LibraryImport("gdi32.dll")]
        internal static partial int SetTextColor(IntPtr hdc, int color);

        [LibraryImport("gdi32.dll", StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool TextOutW(IntPtr hdc, int x, int y, string text, int len);

        [LibraryImport("gdi32.dll", StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool GetTextExtentPoint32W(IntPtr hdc, string text, int len, out SIZE size);
    }
}
