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
    internal static unsafe class InputBox
    {
        private const uint WmInitDialog = 0x0110;
        private const uint WmCommand = 0x0111;
        private const int IdOk = 1;
        private const int IdCancel = 2;
        private const int PromptId = 101;
        private const int EditId = 100;
        private const uint EmSetSel = 0x00B1;

        private const uint DialogStyle = 0x80C808C0;
        private const uint StaticStyle = 0x50000000;
        private const uint EditStyle = 0x50810080;
        private const uint OkStyle = 0x50010001;

        private static string promptText = "";
        private static string initialText = "";
        private static string result;

        internal static string Show(IntPtr owner, string prompt, string initial)
        {
            promptText = prompt ?? "";
            initialText = initial ?? "";
            result = null;
            IntPtr template = BuildTemplate();
            try
            {
                DialogBoxIndirectParamW(
                    GetModuleHandleW(IntPtr.Zero),
                    template,
                    owner,
                    (IntPtr)(delegate* unmanaged<IntPtr, uint, IntPtr, IntPtr, IntPtr>)&DlgProc,
                    IntPtr.Zero);
            }
            finally
            {
                Marshal.FreeHGlobal(template);
            }
            return result;
        }

        private static IntPtr BuildTemplate()
        {
            IntPtr mem = Marshal.AllocHGlobal(1024);
            int at = 0;

            WriteDword(mem, ref at, DialogStyle);
            WriteDword(mem, ref at, 0);
            WriteWord(mem, ref at, 3);
            WriteWord(mem, ref at, 0);
            WriteWord(mem, ref at, 0);
            WriteWord(mem, ref at, 220);
            WriteWord(mem, ref at, 58);
            WriteWord(mem, ref at, 0);
            WriteWord(mem, ref at, 0);
            WriteString(mem, ref at, "notemeow++");
            WriteWord(mem, ref at, 9);
            WriteString(mem, ref at, "Segoe UI");

            WriteItem(mem, ref at, StaticStyle, 7, 7, 206, 10, PromptId, 0x0082, null);
            WriteItem(mem, ref at, EditStyle, 7, 20, 206, 13, EditId, 0x0081, null);
            WriteItem(mem, ref at, OkStyle, 163, 38, 50, 14, IdOk, 0x0080, "OK");
            return mem;
        }

        private static void WriteItem(
            IntPtr mem,
            ref int at,
            uint style,
            short x,
            short y,
            short cx,
            short cy,
            int id,
            ushort classOrdinal,
            string title)
        {
            Align4(ref at);
            WriteDword(mem, ref at, style);
            WriteDword(mem, ref at, 0);
            WriteWord(mem, ref at, (ushort)x);
            WriteWord(mem, ref at, (ushort)y);
            WriteWord(mem, ref at, (ushort)cx);
            WriteWord(mem, ref at, (ushort)cy);
            WriteWord(mem, ref at, (ushort)id);
            WriteWord(mem, ref at, 0xFFFF);
            WriteWord(mem, ref at, classOrdinal);
            if (title == null) WriteWord(mem, ref at, 0);
            else WriteString(mem, ref at, title);
            WriteWord(mem, ref at, 0);
        }

        private static void Align4(ref int at)
        {
            at = (at + 3) & ~3;
        }

        private static void WriteWord(IntPtr mem, ref int at, ushort value)
        {
            Marshal.WriteInt16(mem, at, (short)value);
            at += 2;
        }

        private static void WriteDword(IntPtr mem, ref int at, uint value)
        {
            Marshal.WriteInt32(mem, at, (int)value);
            at += 4;
        }

        private static void WriteString(IntPtr mem, ref int at, string text)
        {
            foreach (char c in text) WriteWord(mem, ref at, c);
            WriteWord(mem, ref at, 0);
        }

        [UnmanagedCallersOnly]
        private static IntPtr DlgProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == WmInitDialog)
            {
                SetDlgItemTextW(hwnd, PromptId, promptText);
                SetDlgItemTextW(hwnd, EditId, initialText);
                SendDlgItemMessageW(hwnd, EditId, EmSetSel, IntPtr.Zero, (IntPtr)(-1));
                return (IntPtr)1;
            }
            if (msg == WmCommand)
            {
                int command = (int)(long)wParam & 0xFFFF;
                if (command == IdOk)
                {
                    char* buf = stackalloc char[1024];
                    int len = GetDlgItemTextW(hwnd, EditId, buf, 1024);
                    result = new string(buf, 0, len);
                    EndDialog(hwnd, (IntPtr)1);
                    return (IntPtr)1;
                }
                if (command == IdCancel)
                {
                    result = null;
                    EndDialog(hwnd, IntPtr.Zero);
                    return (IntPtr)1;
                }
            }
            return IntPtr.Zero;
        }

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandleW(IntPtr name);

        [DllImport("user32.dll")]
        private static extern IntPtr DialogBoxIndirectParamW(
            IntPtr instance, IntPtr template, IntPtr owner, IntPtr dlgProc, IntPtr param);

        [DllImport("user32.dll")]
        private static extern bool EndDialog(IntPtr hwnd, IntPtr result);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern bool SetDlgItemTextW(IntPtr hwnd, int id, string text);

        [DllImport("user32.dll")]
        private static extern int GetDlgItemTextW(IntPtr hwnd, int id, char* buf, int max);

        [DllImport("user32.dll")]
        private static extern IntPtr SendDlgItemMessageW(
            IntPtr hwnd, int id, uint msg, IntPtr wParam, IntPtr lParam);
    }
}
