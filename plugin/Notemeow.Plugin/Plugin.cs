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
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Notemeow.Core;

namespace Notemeow.Plugin
{
    internal static unsafe class Plugin
    {
        private static NppApi.NppData nppData;
        private static readonly Dictionary<ulong, MeowState> States =
            new Dictionary<ulong, MeowState>();
        private static bool swallowEscChar;
        private static bool swallowSysChar;
        private static string whichKeyKind;
        private static string whichKeyBuffer;

        private static readonly IntPtr NameBuf = Marshal.StringToHGlobalUni("notemeow++");

        private const int FuncItemSize = 128 + 8 + 4 + 4 + 8;
        private const int FuncCount = 4;
        private static IntPtr funcItems;

        [UnmanagedCallersOnly(EntryPoint = "isUnicode")]
        internal static int IsUnicode()
        {
            return 1;
        }

        [UnmanagedCallersOnly(EntryPoint = "setInfo")]
        internal static void SetInfo(NppApi.NppData data)
        {
            nppData = data;
            NppApi.SetWindowSubclass(data.ScintillaMainHandle, &SubclassProc, (UIntPtr)1, UIntPtr.Zero);
            NppApi.SetWindowSubclass(data.ScintillaSecondHandle, &SubclassProc, (UIntPtr)1, UIntPtr.Zero);
            LoadUserRc(false);
        }

        [UnmanagedCallersOnly(EntryPoint = "getName")]
        internal static IntPtr GetName()
        {
            return NameBuf;
        }

        [UnmanagedCallersOnly(EntryPoint = "getFuncsArray")]
        internal static IntPtr GetFuncsArray(int* count)
        {
            *count = FuncCount;
            if (funcItems == IntPtr.Zero) funcItems = BuildFuncItems();
            return funcItems;
        }

        [UnmanagedCallersOnly(EntryPoint = "beNotified")]
        internal static void BeNotified(IntPtr scn)
        {
            var hdr = *(NppApi.NmHdr*)scn;
            if (hdr.Code == NppApi.NppnShutdown)
            {
                NppApi.RemoveWindowSubclass(nppData.ScintillaMainHandle, &SubclassProc, (UIntPtr)1);
                NppApi.RemoveWindowSubclass(nppData.ScintillaSecondHandle, &SubclassProc, (UIntPtr)1);
            }
            else if (hdr.Code == NppApi.NppnBufferActivated || hdr.Code == NppApi.NppnReady)
            {
                ShowMode(CurrentState(), ActiveScintilla());
            }
        }

        [UnmanagedCallersOnly(EntryPoint = "messageProc")]
        internal static IntPtr MessageProc(uint msg, UIntPtr wParam, IntPtr lParam)
        {
            return (IntPtr)1;
        }

        private static IntPtr BuildFuncItems()
        {
            IntPtr mem = Marshal.AllocHGlobal(FuncItemSize * FuncCount);
            WriteFuncItem(mem, 0, "Edit .notemeowrc", &MenuEditRc);
            WriteFuncItem(mem, 1, "Reload .notemeowrc", &MenuReloadRc);
            WriteFuncItem(mem, 2, "Meow Cheatsheet", &MenuCheatsheet);
            WriteFuncItem(mem, 3, "About notemeow++", &MenuAbout);
            return mem;
        }

        private static void WriteFuncItem(
            IntPtr mem, int index, string name, delegate* unmanaged<void> handler)
        {
            IntPtr slot = mem + index * FuncItemSize;
            for (int i = 0; i < 64; i++)
            {
                char c = i < name.Length ? name[i] : '\0';
                Marshal.WriteInt16(slot, i * 2, (short)c);
            }
            Marshal.WriteIntPtr(slot, 128, (IntPtr)handler);
            Marshal.WriteInt32(slot, 136, 0);
            Marshal.WriteInt32(slot, 140, 0);
            Marshal.WriteIntPtr(slot, 144, IntPtr.Zero);
        }

        private static MeowState CurrentState()
        {
            ulong id = (ulong)NppApi.SendMessage(
                nppData.NppHandle, NppApi.NppmGetCurrentBufferId, IntPtr.Zero, IntPtr.Zero);
            if (!States.TryGetValue(id, out MeowState st))
            {
                st = new MeowState();
                States[id] = st;
            }
            return st;
        }

        private static Ctx MakeCtx(IntPtr sciHwnd)
        {
            var port = new ScintillaPort(sciHwnd, nppData.NppHandle);
            return new Ctx(
                port,
                new Win32Clipboard(nppData.NppHandle),
                new NppUi(nppData.NppHandle, sciHwnd, port),
                CurrentState());
        }

        private static IntPtr ActiveScintilla()
        {
            int view = 0;
            NppApi.SendMessage(
                nppData.NppHandle, NppApi.NppmGetCurrentScintilla, IntPtr.Zero, (IntPtr)(&view));
            return view == 0 ? nppData.ScintillaMainHandle : nppData.ScintillaSecondHandle;
        }

        private static void ArmAvyTimer(IntPtr hwnd)
        {
            if (Avy.AwaitingTimeout(CurrentState()))
            {
                NppApi.SetTimer(hwnd, NppApi.AvyTimerId, NppApi.AvyTimeoutMs, IntPtr.Zero);
            }
            else
            {
                NppApi.KillTimer(hwnd, NppApi.AvyTimerId);
            }
        }

        private static void ShowWhichKey(IntPtr sci)
        {
            string kind = whichKeyKind;
            if (kind == null) return;
            string buffer = whichKeyBuffer ?? "";
            IReadOnlyList<WhichKey.Row> rows =
                kind == "things" ? WhichKey.Things : WhichKey.KeypadRows(buffer);
            string title =
                kind == "things"
                    ? "thing:"
                    : buffer.Length == 0 ? "SPC" : "SPC " + string.Join(" ", buffer.ToCharArray());
            WhichKeyOverlay.Show(sci, title, rows);
        }

        private static Windmove.ViewLayout CurrentViewLayout()
        {
            IntPtr main = nppData.ScintillaMainHandle;
            IntPtr second = nppData.ScintillaSecondHandle;
            bool twoViews = NppApi.IsWindowVisible(main) && NppApi.IsWindowVisible(second);
            if (!twoViews) return new Windmove.ViewLayout(false, false, false);
            NppApi.Rect mainRect;
            NppApi.Rect secondRect;
            NppApi.GetWindowRect(main, out mainRect);
            NppApi.GetWindowRect(second, out secondRect);
            bool stacked = mainRect.Top != secondRect.Top;
            bool onSecond =
                ActiveScintilla() == main
                    ? stacked ? mainRect.Top > secondRect.Top : mainRect.Left > secondRect.Left
                    : stacked ? secondRect.Top > mainRect.Top : secondRect.Left > mainRect.Left;
            return new Windmove.ViewLayout(true, stacked, onSecond);
        }

        private static void ShowMode(MeowState st, IntPtr sci)
        {
            NppApi.SendMessageStr(
                nppData.NppHandle,
                NppApi.NppmSetStatusBar,
                (IntPtr)NppApi.StatusBarTypingMode,
                "MEOW " + st.Mode.ToString().ToUpperInvariant());
            int style = st.Mode == MeowMode.Insert ? NppApi.CaretStyleLine : NppApi.CaretStyleBlock;
            NppApi.SendMessage(sci, (uint)NppApi.SciSetCaretStyle, (IntPtr)style, IntPtr.Zero);
        }

        [UnmanagedCallersOnly]
        private static IntPtr SubclassProc(
            IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam, UIntPtr id, UIntPtr refData)
        {
            try
            {
                switch (msg)
                {
                    case NppApi.WmChar:
                        {
                            char c = (char)(long)wParam;
                            if (c == (char)27)
                            {
                                if (swallowEscChar)
                                {
                                    swallowEscChar = false;
                                    return IntPtr.Zero;
                                }
                                break;
                            }
                            if (c < ' ') break;
                            if (Engine.HandleChar(MakeCtx(hwnd), c))
                            {
                                ArmAvyTimer(hwnd);
                                return IntPtr.Zero;
                            }
                            break;
                        }
                    case NppApi.WmTimer:
                        if ((nuint)(long)wParam == NppApi.AvyTimerId)
                        {
                            NppApi.KillTimer(hwnd, NppApi.AvyTimerId);
                            Avy.FinishInput(MakeCtx(hwnd));
                            return IntPtr.Zero;
                        }
                        if ((nuint)(long)wParam == NppApi.WhichKeyTimerId)
                        {
                            NppApi.KillTimer(hwnd, NppApi.WhichKeyTimerId);
                            ShowWhichKey(hwnd);
                            return IntPtr.Zero;
                        }
                        break;
                    case NppApi.WmKeyDown:
                        {
                            int vk = (int)(long)wParam;
                            if (vk == NppApi.VkEscape)
                            {
                                if (Engine.EscapeKey(MakeCtx(hwnd)))
                                {
                                    NppApi.KillTimer(hwnd, NppApi.AvyTimerId);
                                    swallowEscChar = true;
                                    return IntPtr.Zero;
                                }
                                break;
                            }
                            bool ctrl = NppApi.GetKeyState(NppApi.VkControl) < 0;
                            bool alt = NppApi.GetKeyState(NppApi.VkMenu) < 0;
                            if (ctrl && !alt)
                            {
                                string cmd = CtrlChord(vk);
                                if (cmd != null)
                                {
                                    Ctx ctx = MakeCtx(hwnd);
                                    if (ctx.St.Mode == MeowMode.Normal)
                                    {
                                        Engine.RunEmacsMotion(ctx, cmd);
                                        return IntPtr.Zero;
                                    }
                                }
                            }
                            break;
                        }
                    case NppApi.WmSysKeyDown:
                        {
                            int vk = (int)(long)wParam;
                            bool shift = NppApi.GetKeyState(NppApi.VkShift) < 0;
                            string cmd = AltChord(vk, shift);
                            if (cmd != null)
                            {
                                Ctx ctx = MakeCtx(hwnd);
                                if (ctx.St.Mode == MeowMode.Normal)
                                {
                                    Engine.RunEmacsMotion(ctx, cmd);
                                    swallowSysChar = true;
                                    return IntPtr.Zero;
                                }
                            }
                            break;
                        }
                    case NppApi.WmSysChar:
                        if (swallowSysChar)
                        {
                            swallowSysChar = false;
                            return IntPtr.Zero;
                        }
                        break;
                    default:
                        break;
                }
            }
            catch
            {
            }
            return NppApi.DefSubclassProc(hwnd, msg, wParam, lParam);
        }

        private static string CtrlChord(int vk)
        {
            switch (vk)
            {
                case 'F': return "forward-char";
                case 'B': return "backward-char";
                case 'N': return "next-line";
                case 'P': return "previous-line";
                case 'A': return "move-beginning-of-line";
                case 'E': return "move-end-of-line";
                default: return null;
            }
        }

        private static string AltChord(int vk, bool shift)
        {
            switch (vk)
            {
                case 'F': return "forward-word";
                case 'B': return "backward-word";
                case 'A': return "backward-sentence";
                case 'E': return "forward-sentence";
                case 'U': return "upcase-word";
                case 'L': return "downcase-word";
                case 'C': return "capitalize-word";
                case 'D': return "kill-word";
                case NppApi.VkOemComma: return shift ? "beginning-of-buffer" : null;
                case NppApi.VkOemPeriod: return shift ? "end-of-buffer" : null;
                default: return null;
            }
        }

        private static string RcPath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".notemeowrc");
        }

        private static void LoadUserRc(bool report)
        {
            string path = RcPath();
            if (!File.Exists(path))
            {
                if (report)
                {
                    NppApi.MessageBox(
                        nppData.NppHandle,
                        "No ~/.notemeowrc yet — use \"Edit .notemeowrc\" to create one.",
                        "notemeow++",
                        0);
                }
                return;
            }
            Rc.Config cfg = Rc.SetUserLines(new List<string>(File.ReadAllLines(path)));
            if (!report) return;
            var sb = new StringBuilder();
            sb.Append("Loaded ").Append(path).Append('\n');
            sb.Append(cfg.Normal.Count).Append(" normal, ")
                .Append(cfg.Motion.Count).Append(" motion, ")
                .Append(cfg.Keypad.Count).Append(" keypad mappings");
            if (cfg.Errors.Count > 0)
            {
                sb.Append("\n\nProblems:\n");
                int shown = 0;
                foreach (string e in cfg.Errors)
                {
                    sb.Append(e).Append('\n');
                    if (++shown >= 10) break;
                }
            }
            NppApi.MessageBox(nppData.NppHandle, sb.ToString(), "notemeow++", 0);
        }

        private static void EditRc()
        {
            string path = RcPath();
            if (!File.Exists(path)) File.WriteAllLines(path, Rc.BundledLines());
            NppApi.SendMessageStr(nppData.NppHandle, NppApi.NppmDoOpen, IntPtr.Zero, path);
        }

        private static void ReloadRc()
        {
            SaveRcEditorIfDirty();
            LoadUserRc(true);
        }

        private static void SaveRcEditorIfDirty()
        {
            const int pathCapacity = 1024;
            char* buf = stackalloc char[pathCapacity];
            IntPtr got = NppApi.SendMessage(
                nppData.NppHandle,
                NppApi.NppmGetFullCurrentPath,
                (IntPtr)pathCapacity,
                (IntPtr)buf);
            if (got == IntPtr.Zero) return;
            string current = new string(buf);
            if (!string.Equals(current, RcPath(), StringComparison.OrdinalIgnoreCase)) return;
            IntPtr modified = NppApi.SendMessage(
                ActiveScintilla(), (uint)NppApi.SciGetModify, IntPtr.Zero, IntPtr.Zero);
            if (modified == IntPtr.Zero) return;
            NppApi.SendMessage(
                nppData.NppHandle, NppApi.NppmSaveCurrentFile, IntPtr.Zero, IntPtr.Zero);
        }

        [UnmanagedCallersOnly]
        private static void MenuEditRc()
        {
            try
            {
                EditRc();
            }
            catch (Exception e)
            {
                NppApi.MessageBox(nppData.NppHandle, e.Message, "notemeow++", 0);
            }
        }

        [UnmanagedCallersOnly]
        private static void MenuReloadRc()
        {
            try
            {
                ReloadRc();
            }
            catch (Exception e)
            {
                NppApi.MessageBox(nppData.NppHandle, e.Message, "notemeow++", 0);
            }
        }

        [UnmanagedCallersOnly]
        private static void MenuCheatsheet()
        {
            NppApi.MessageBox(nppData.NppHandle, Keypad.Cheatsheet, "Meow Cheatsheet", 0);
        }

        [UnmanagedCallersOnly]
        private static void MenuAbout()
        {
            NppApi.MessageBox(
                nppData.NppHandle,
                "notemeow++ — meow modal editing for Notepad++\n"
                    + "Engine: Notemeow.Core (281 behavior specs)\n"
                    + "License: GPL-3.0-or-later",
                "About notemeow++",
                0);
        }

        private sealed class Win32Clipboard : IClipboardPort
        {
            private readonly IntPtr owner;

            internal Win32Clipboard(IntPtr owner)
            {
                this.owner = owner;
            }

            public string Read()
            {
                if (!NppApi.OpenClipboard(owner)) return null;
                try
                {
                    IntPtr handle = NppApi.GetClipboardData(NppApi.CfUnicodeText);
                    if (handle == IntPtr.Zero) return null;
                    IntPtr ptr = NppApi.GlobalLock(handle);
                    if (ptr == IntPtr.Zero) return null;
                    try
                    {
                        return Marshal.PtrToStringUni(ptr);
                    }
                    finally
                    {
                        NppApi.GlobalUnlock(handle);
                    }
                }
                finally
                {
                    NppApi.CloseClipboard();
                }
            }

            public void Write(string text)
            {
                if (text == null) return;
                if (!NppApi.OpenClipboard(owner)) return;
                try
                {
                    NppApi.EmptyClipboard();
                    int bytes = (text.Length + 1) * 2;
                    IntPtr handle = NppApi.GlobalAlloc(NppApi.GmemMoveable, (UIntPtr)bytes);
                    if (handle == IntPtr.Zero) return;
                    IntPtr ptr = NppApi.GlobalLock(handle);
                    if (ptr == IntPtr.Zero) return;
                    fixed (char* src = text)
                    {
                        Buffer.MemoryCopy(src, (void*)ptr, bytes, text.Length * 2);
                    }
                    Marshal.WriteInt16(ptr, text.Length * 2, 0);
                    NppApi.GlobalUnlock(handle);
                    NppApi.SetClipboardData(NppApi.CfUnicodeText, handle);
                }
                finally
                {
                    NppApi.CloseClipboard();
                }
            }
        }

        private sealed class NppUi : IUiPort
        {
            private const int ExpandHintBg = 0x00B25D2B;

            private readonly IntPtr npp;
            private readonly IntPtr sci;
            private readonly ScintillaPort port;

            internal NppUi(IntPtr npp, IntPtr sci, ScintillaPort port)
            {
                this.npp = npp;
                this.sci = sci;
                this.port = port;
            }

            private void Status(string text)
            {
                NppApi.SendMessageStr(
                    npp, NppApi.NppmSetStatusBar, (IntPtr)NppApi.StatusBarDocType, text);
            }

            public void Hint(string text)
            {
                Status("meow — " + text);
            }

            public void Info(string title, string body)
            {
                NppApi.MessageBox(npp, body, title, 0);
            }

            public string Input(string prompt, string initial)
            {
                return InputBox.Show(npp, prompt, initial);
            }

            public void RunCommand(string idText)
            {
                if (idText == "notemeow.editRc")
                {
                    EditRc();
                    return;
                }
                if (idText == "notemeow.reloadRc")
                {
                    ReloadRc();
                    return;
                }
                if (idText == "notemeow.windmoveLeft")
                {
                    WindmoveTo(Windmove.Dir.Left);
                    return;
                }
                if (idText == "notemeow.windmoveDown")
                {
                    WindmoveTo(Windmove.Dir.Down);
                    return;
                }
                if (idText == "notemeow.windmoveUp")
                {
                    WindmoveTo(Windmove.Dir.Up);
                    return;
                }
                if (idText == "notemeow.windmoveRight")
                {
                    WindmoveTo(Windmove.Dir.Right);
                    return;
                }
                if (idText == Windmove.FocusOtherView)
                {
                    RunCommand("IDM_VIEW_SWITCHTO_OTHER_VIEW");
                    return;
                }
                int id;
                if (!NppMenuIds.TryGet(idText, out id) && !int.TryParse(idText, out id))
                {
                    throw new InvalidOperationException("not a menu command id: " + idText);
                }
                NppApi.SendMessage(npp, NppApi.NppmMenuCommand, IntPtr.Zero, (IntPtr)id);
            }

            private void WindmoveTo(Windmove.Dir dir)
            {
                string plan = Windmove.Plan(dir, CurrentViewLayout());
                if (plan == null) Hint(Windmove.NoWindowMessage(dir));
                else RunCommand(plan);
            }

            public void ScheduleWhichKey(string kind, string buffer)
            {
                if (!Rc.WhichKeyEnabled()) return;
                whichKeyKind = kind;
                whichKeyBuffer = buffer;
                NppApi.SetTimer(
                    sci, NppApi.WhichKeyTimerId, (uint)Rc.WhichKeyDelayMs(), IntPtr.Zero);
            }

            public void HideWhichKey()
            {
                NppApi.KillTimer(sci, NppApi.WhichKeyTimerId);
                WhichKeyOverlay.Hide();
            }

            public void ShowExpandHints(List<int> positions)
            {
                var labels = new List<AvyLabel>();
                for (int i = 0; i < positions.Count; i++)
                {
                    labels.Add(new AvyLabel(positions[i], ((i + 1) % 10).ToString()));
                }
                AvyOverlay.Show(
                    port.Handle, port.ResolveLabels(labels), port.TextHeight(), ExpandHintBg);
            }

            public void ClearExpandHints()
            {
                AvyOverlay.Hide();
            }

            public void ShowAvyMatches(List<OffsetRange> matches)
            {
                port.HighlightMatches(matches);
            }

            public void ShowAvyLabels(List<AvyLabel> labels)
            {
                AvyOverlay.Show(port.Handle, port.ResolveLabels(labels), port.TextHeight());
            }

            public void ClearAvy()
            {
                port.ClearMatches();
                AvyOverlay.Hide();
            }

            public void ModeChanged(MeowState st)
            {
                ShowMode(st, sci);
            }

            public void Refresh(MeowState st)
            {
                ShowMode(st, sci);
                port.HighlightGrab(st.Grab);
            }
        }
    }
}
