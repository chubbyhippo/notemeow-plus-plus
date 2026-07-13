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
    internal static class NppApi
    {
        internal const int NppMsg = 0x400 + 1000;
        internal const int NppmGetCurrentScintilla = NppMsg + 4;
        internal const int NppmSetStatusBar = NppMsg + 24;
        internal const int NppmMenuCommand = NppMsg + 48;
        internal const int NppmGetCurrentBufferId = NppMsg + 60;
        internal const int NppmDoOpen = NppMsg + 77;

        internal const int StatusBarDocType = 0;
        internal const int StatusBarTypingMode = 5;

        internal const int NppnFirst = 1000;
        internal const int NppnReady = NppnFirst + 1;
        internal const int NppnShutdown = NppnFirst + 9;
        internal const int NppnBufferActivated = NppnFirst + 10;

        internal const int IdmFileClose = 40000 + 1000 + 3;

        internal const int SciInsertText = 2003;
        internal const int SciGetLength = 2006;
        internal const int SciBeginUndoAction = 2078;
        internal const int SciEndUndoAction = 2079;
        internal const int SciGetCodePage = 2137;
        internal const int SciGetReadOnly = 2140;
        internal const int SciGetFirstVisibleLine = 2152;
        internal const int SciScrollCaret = 2169;
        internal const int SciUndo = 2176;
        internal const int SciGetText = 2182;
        internal const int SciReplaceTarget = 2194;
        internal const int SciDocLineFromVisible = 2221;
        internal const int SciLinesOnScreen = 2370;
        internal const int SciGetSelections = 2570;
        internal const int SciClearSelections = 2571;
        internal const int SciSetSelection = 2572;
        internal const int SciAddSelection = 2573;
        internal const int SciSetMainSelection = 2574;
        internal const int SciGetMainSelection = 2575;
        internal const int SciGetSelectionNCaret = 2577;
        internal const int SciGetSelectionNAnchor = 2579;
        internal const int SciDeleteRange = 2645;
        internal const int SciSetTargetRange = 2686;
        internal const int SciSetCaretStyle = 2512;
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

        // Scintilla indicator slot for the grab highlight. 0-7 are lexer
        // indicators and Notepad++'s own smart-highlight/mark features cluster
        // in 27-31, so this sits in the free middle band; change it if a
        // plugin you use claims it.
        internal const int GrabIndicator = 12;

        internal const uint WmChar = 0x0102;
        internal const uint WmKeyDown = 0x0100;
        internal const uint WmSysKeyDown = 0x0104;
        internal const uint WmSysChar = 0x0106;

        internal const int VkEscape = 0x1B;
        internal const int VkControl = 0x11;
        internal const int VkShift = 0x10;
        internal const int VkMenu = 0x12;
        internal const int VkOemComma = 0xBC;
        internal const int VkOemPeriod = 0xBE;

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

        [DllImport("user32.dll", EntryPoint = "SendMessageW")]
        internal static extern IntPtr SendMessage(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", EntryPoint = "SendMessageW", CharSet = CharSet.Unicode)]
        internal static extern IntPtr SendMessageStr(IntPtr hwnd, uint msg, IntPtr wParam, string lParam);

        [DllImport("user32.dll")]
        internal static extern short GetKeyState(int vKey);

        [DllImport("user32.dll", EntryPoint = "MessageBoxW", CharSet = CharSet.Unicode)]
        internal static extern int MessageBox(IntPtr hwnd, string text, string caption, uint type);

        [DllImport("comctl32.dll", SetLastError = true)]
        internal static extern unsafe bool SetWindowSubclass(
            IntPtr hwnd,
            delegate* unmanaged<IntPtr, uint, IntPtr, IntPtr, UIntPtr, UIntPtr, IntPtr> proc,
            UIntPtr id,
            UIntPtr refData);

        [DllImport("comctl32.dll")]
        internal static extern unsafe bool RemoveWindowSubclass(
            IntPtr hwnd,
            delegate* unmanaged<IntPtr, uint, IntPtr, IntPtr, UIntPtr, UIntPtr, IntPtr> proc,
            UIntPtr id);

        [DllImport("comctl32.dll")]
        internal static extern IntPtr DefSubclassProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        internal static extern bool OpenClipboard(IntPtr owner);

        [DllImport("user32.dll")]
        internal static extern bool CloseClipboard();

        [DllImport("user32.dll")]
        internal static extern bool EmptyClipboard();

        [DllImport("user32.dll")]
        internal static extern IntPtr GetClipboardData(uint format);

        [DllImport("user32.dll")]
        internal static extern IntPtr SetClipboardData(uint format, IntPtr handle);

        [DllImport("kernel32.dll")]
        internal static extern IntPtr GlobalAlloc(uint flags, UIntPtr bytes);

        [DllImport("kernel32.dll")]
        internal static extern IntPtr GlobalLock(IntPtr handle);

        [DllImport("kernel32.dll")]
        internal static extern bool GlobalUnlock(IntPtr handle);
    }
}
