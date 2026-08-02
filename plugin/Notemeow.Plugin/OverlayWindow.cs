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

namespace Notemeow.Plugin
{
    internal sealed unsafe class OverlayWindow(
        string className, uint exStyle, int backgroundColor, Action<IntPtr> paint)
    {
        private static readonly Dictionary<IntPtr, OverlayWindow> Live = [];

        private IntPtr classNamePtr;
        private ushort classAtom;
        private IntPtr handle;

        internal bool Ensure()
        {
            if (handle != IntPtr.Zero) return true;
            IntPtr instance = Win32.GetModuleHandleW(IntPtr.Zero);
            if (classAtom == 0 && !RegisterClass(instance)) return false;
            handle = Win32.CreateWindowExW(
                exStyle,
                classNamePtr,
                IntPtr.Zero,
                Win32.WsPopup,
                0,
                0,
                0,
                0,
                IntPtr.Zero,
                IntPtr.Zero,
                instance,
                IntPtr.Zero);
            if (handle == IntPtr.Zero) return false;
            if ((exStyle & Win32.WsExLayered) != 0)
            {
                Win32.SetLayeredWindowAttributes(handle, backgroundColor, 0, Win32.LwaColorKey);
            }
            Live[handle] = this;
            return true;
        }

        private bool RegisterClass(IntPtr instance)
        {
            IntPtr namePtr = Marshal.StringToHGlobalUni(className);
            IntPtr background = Win32.CreateSolidBrush(backgroundColor);
            var wc = new Win32.WNDCLASSW
            {
                style = 0,
                lpfnWndProc = (IntPtr)(delegate* unmanaged<IntPtr, uint, IntPtr, IntPtr, IntPtr>)&WndProc,
                hInstance = instance,
                hbrBackground = background,
                lpszClassName = namePtr,
            };
            classAtom = Win32.RegisterClassW(ref wc);
            if (classAtom == 0)
            {
                Win32.DeleteObject(background);
                Marshal.FreeHGlobal(namePtr);
                return false;
            }
            classNamePtr = namePtr;
            return true;
        }

        internal void ShowAt(int x, int y, int width, int height)
        {
            Win32.MoveWindow(handle, x, y, width, height, false);
            Win32.ShowWindow(handle, Win32.SwShowNa);
            Win32.InvalidateRect(handle, IntPtr.Zero, true);
            Win32.UpdateWindow(handle);
        }

        internal void Hide()
        {
            if (handle != IntPtr.Zero) Win32.ShowWindow(handle, Win32.SwHide);
        }

        private void Paint(IntPtr hdc)
        {
            Win32.GetClientRect(handle, out Win32.RECT rc);
            IntPtr background = Win32.CreateSolidBrush(backgroundColor);
            Win32.FillRect(hdc, ref rc, background);
            Win32.DeleteObject(background);
            paint(hdc);
        }

        [UnmanagedCallersOnly]
        private static IntPtr WndProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == Win32.WmPaint && Live.TryGetValue(hwnd, out OverlayWindow window))
            {
                Win32.PAINTSTRUCT ps;
                IntPtr hdc = Win32.BeginPaint(hwnd, &ps);
                if (hdc != IntPtr.Zero)
                {
                    window.Paint(hdc);
                    Win32.EndPaint(hwnd, &ps);
                }
                return IntPtr.Zero;
            }
            return Win32.DefWindowProcW(hwnd, msg, wParam, lParam);
        }
    }
}
