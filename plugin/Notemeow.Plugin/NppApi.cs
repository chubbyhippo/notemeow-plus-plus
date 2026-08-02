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
    internal static partial class NppApi
    {
        internal const int NppMsg = 0x400 + 1000;
        internal const int NppmGetCurrentScintilla = NppMsg + 4;
        internal const int NppmSetStatusBar = NppMsg + 24;
        internal const int NppmSaveCurrentFile = NppMsg + 38;
        internal const int NppmMenuCommand = NppMsg + 48;
        internal const int NppmGetCurrentBufferId = NppMsg + 60;
        internal const int NppmDoOpen = NppMsg + 77;
        internal const int RunCommandUser = 0x400 + 3000;
        internal const int NppmGetFullCurrentPath = RunCommandUser + 1;

        internal const int StatusBarDocType = 0;
        internal const int StatusBarTypingMode = 5;

        internal const int NppnFirst = 1000;
        internal const int NppnReady = NppnFirst + 1;
        internal const int NppnShutdown = NppnFirst + 9;
        internal const int NppnBufferActivated = NppnFirst + 10;

        internal const int ScnUpdateUi = 2007;
        internal const int ScnModified = 2008;
        internal const uint ModeRefreshMsg = 0x8000 + 1;

        internal const int IdmFileClose = 40000 + 1000 + 3;

        internal const int SciGetLength = 2006;
        internal const int SciBeginUndoAction = 2078;
        internal const int SciEndUndoAction = 2079;
        internal const int SciGetCodePage = 2137;
        internal const int SciGetReadOnly = 2140;
        internal const int SciGetOvertype = 2187;
        internal const int SciGetFirstVisibleLine = 2152;
        internal const int SciGetModify = 2159;
        internal const int SciScrollCaret = 2169;
        internal const int SciUndo = 2176;
        internal const int SciGetText = 2182;
        internal const int SciReplaceTarget = 2194;
        internal const int SciDocLineFromVisible = 2221;
        internal const int SciLinesOnScreen = 2370;
        internal const int SciSetFirstVisibleLine = 2613;
        internal const int SciVisibleFromDocLine = 2220;
        internal const int SciLineFromPosition = 2166;
        internal const int SciGetCurrentPos = 2008;
        internal const int SciGetSelections = 2570;
        internal const int SciClearSelections = 2571;
        internal const int SciSetSelection = 2572;
        internal const int SciAddSelection = 2573;
        internal const int SciSetMainSelection = 2574;
        internal const int SciGetMainSelection = 2575;
        internal const int SciGetSelectionNCaret = 2577;
        internal const int SciGetSelectionNAnchor = 2579;
        internal const int SciSetTargetRange = 2686;
        internal const int SciSetCaretStyle = 2512;
        internal const int SciPointXFromPosition = 2164;
        internal const int SciPointYFromPosition = 2165;
        internal const int SciTextHeight = 2279;
        internal const int SciIndicSetStyle = 2080;
        internal const int SciIndicSetFore = 2082;
        internal const int SciIndicSetUnder = 2510;
        internal const int SciSetIndicatorCurrent = 2500;
        internal const int SciIndicatorFillRange = 2504;
        internal const int SciIndicatorClearRange = 2505;
        internal const int SciIndicSetAlpha = 2523;

        internal const int CaretStyleLine = 1;
        internal const int CaretStyleBlock = 2;

        internal const int IndicStraightBox = 8;

        internal const int GrabIndicator = 12;
        internal const int AvyMatchIndicator = 13;

        internal const uint WmTimer = 0x0113;
        internal const nuint AvyTimerId = 0xA5EF;
        internal const uint AvyTimeoutMs = 250;
        internal const nuint WhichKeyTimerId = 0xA5F0;

        internal static int BgrFromRgb(int rgb)
        {
            int red = (rgb >> 16) & 0xFF;
            int green = (rgb >> 8) & 0xFF;
            int blue = rgb & 0xFF;
            return (blue << 16) | (green << 8) | red;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool IsWindowVisible(IntPtr hwnd);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool GetWindowRect(IntPtr hwnd, out Rect rc);

        [LibraryImport("user32.dll")]
        internal static partial nuint SetTimer(IntPtr hwnd, nuint id, uint elapseMs, IntPtr callback);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool KillTimer(IntPtr hwnd, nuint id);

        internal const uint WmChar = 0x0102;
        internal const uint WmKeyDown = 0x0100;
        internal const uint WmSysKeyDown = 0x0104;
        internal const uint WmSysChar = 0x0106;

        internal const int VkEscape = 0x1B;
        internal const int VkControl = 0x11;
        internal const int VkShift = 0x10;
        internal const int VkMenu = 0x12;
        internal const int VkTab = 0x09;
        internal const int VkSpace = 0x20;
        internal const int VkOemComma = 0xBC;
        internal const int VkOemPeriod = 0xBE;
        internal const int VkOemSemicolon = 0xBA;
        internal const int VkOemOpenBracket = 0xDB;
        internal const int VkOemCloseBracket = 0xDD;
        internal const int VkOemPlus = 0xBB;
        internal const int VkOemMinus = 0xBD;
        internal const int VkOemSlash = 0xBF;
        internal const int VkOemBackQuote = 0xC0;
        internal const int VkOemBackSlash = 0xDC;
        internal const int VkOemQuote = 0xDE;

        internal const uint CfUnicodeText = 13;
        internal const uint GmemMoveable = 0x0002;

        [StructLayout(LayoutKind.Sequential)]
        internal struct NppData
        {
            public IntPtr NppHandle;
            public IntPtr ScintillaMainHandle;
            public IntPtr ScintillaSecondHandle;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct NmHdr
        {
            public IntPtr HwndFrom;
            public UIntPtr IdFrom;
            public uint Code;
        }

        [LibraryImport("user32.dll", EntryPoint = "SendMessageW")]
        internal static partial IntPtr SendMessage(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

        [LibraryImport("user32.dll", EntryPoint = "SendMessageW", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial IntPtr SendMessageStr(IntPtr hwnd, uint msg, IntPtr wParam, string lParam);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool PostMessage(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

        [LibraryImport("user32.dll")]
        internal static partial short GetKeyState(int vKey);

        [LibraryImport("user32.dll", EntryPoint = "MessageBoxW", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial int MessageBox(IntPtr hwnd, string text, string caption, uint type);

        [LibraryImport("comctl32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial bool SetWindowSubclass(
            IntPtr hwnd,
            delegate* unmanaged<IntPtr, uint, IntPtr, IntPtr, UIntPtr, UIntPtr, IntPtr> proc,
            UIntPtr id,
            UIntPtr refData);

        [LibraryImport("comctl32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial bool RemoveWindowSubclass(
            IntPtr hwnd,
            delegate* unmanaged<IntPtr, uint, IntPtr, IntPtr, UIntPtr, UIntPtr, IntPtr> proc,
            UIntPtr id);

        [LibraryImport("comctl32.dll")]
        internal static partial IntPtr DefSubclassProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool OpenClipboard(IntPtr owner);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool CloseClipboard();

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool EmptyClipboard();

        [LibraryImport("user32.dll")]
        internal static partial IntPtr GetClipboardData(uint format);

        [LibraryImport("user32.dll")]
        internal static partial IntPtr SetClipboardData(uint format, IntPtr handle);

        [LibraryImport("kernel32.dll")]
        internal static partial IntPtr GlobalAlloc(uint flags, UIntPtr bytes);

        [LibraryImport("kernel32.dll")]
        internal static partial IntPtr GlobalLock(IntPtr handle);

        [LibraryImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool GlobalUnlock(IntPtr handle);
    }
}
