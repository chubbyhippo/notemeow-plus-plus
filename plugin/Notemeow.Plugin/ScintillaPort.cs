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
using System.Text;
using Notemeow.Core;

namespace Notemeow.Plugin
{
    internal sealed class ScintillaPort : IEditorPort
    {
        private readonly IntPtr sci;
        private readonly IntPtr npp;

        internal ScintillaPort(IntPtr scintilla, IntPtr nppHandle)
        {
            sci = scintilla;
            npp = nppHandle;
        }

        private IntPtr Send(int msg)
        {
            return NppApi.SendMessage(sci, (uint)msg, IntPtr.Zero, IntPtr.Zero);
        }

        private IntPtr Send(int msg, long w)
        {
            return NppApi.SendMessage(sci, (uint)msg, (IntPtr)w, IntPtr.Zero);
        }

        private IntPtr Send(int msg, long w, long l)
        {
            return NppApi.SendMessage(sci, (uint)msg, (IntPtr)w, (IntPtr)l);
        }

        private sealed class Mapping
        {
            public string Text;
            public int[] CharToByte;
            public int[] ByteToChar;
        }

        private unsafe Mapping Load()
        {
            int byteLen = (int)Send(NppApi.SciGetLength);
            var bytes = new byte[byteLen];
            if (byteLen > 0)
            {
                var buf = new byte[byteLen + 1];
                fixed (byte* p = buf)
                {
                    NppApi.SendMessage(sci, NppApi.SciGetText, (IntPtr)(byteLen + 1), (IntPtr)p);
                }
                Array.Copy(buf, bytes, byteLen);
            }
            bool utf8 = (int)Send(NppApi.SciGetCodePage) == 65001;
            var sb = new StringBuilder(byteLen);
            var byteToChar = new int[byteLen + 1];
            var charToByteList = new List<int>(byteLen + 1);
            int bi = 0;
            while (bi < byteLen)
            {
                int charIndex = sb.Length;
                int seqLen = 1;
                if (!utf8 || bytes[bi] < 0x80)
                {
                    sb.Append((char)bytes[bi]);
                }
                else
                {
                    seqLen = Utf8SeqLen(bytes[bi]);
                    if (bi + seqLen > byteLen || !ValidContinuation(bytes, bi, seqLen))
                    {
                        seqLen = 1;
                        sb.Append((char)bytes[bi]);
                    }
                    else
                    {
                        int cp = DecodeUtf8(bytes, bi, seqLen);
                        if (cp <= 0xFFFF)
                        {
                            sb.Append((char)cp);
                        }
                        else
                        {
                            cp -= 0x10000;
                            sb.Append((char)(0xD800 + (cp >> 10)));
                            sb.Append((char)(0xDC00 + (cp & 0x3FF)));
                        }
                    }
                }
                for (int k = 0; k < seqLen; k++) byteToChar[bi + k] = charIndex;
                while (charToByteList.Count < sb.Length) charToByteList.Add(bi);
                bi += seqLen;
            }
            byteToChar[byteLen] = sb.Length;
            charToByteList.Add(byteLen);
            return new Mapping
            {
                Text = sb.ToString(),
                CharToByte = charToByteList.ToArray(),
                ByteToChar = byteToChar,
            };
        }

        private static int Utf8SeqLen(byte b)
        {
            if ((b & 0xE0) == 0xC0) return 2;
            if ((b & 0xF0) == 0xE0) return 3;
            if ((b & 0xF8) == 0xF0) return 4;
            return 1;
        }

        private static bool ValidContinuation(byte[] bytes, int start, int len)
        {
            for (int i = 1; i < len; i++)
            {
                if ((bytes[start + i] & 0xC0) != 0x80) return false;
            }
            return true;
        }

        private static int DecodeUtf8(byte[] bytes, int start, int len)
        {
            int cp = len == 2 ? bytes[start] & 0x1F : len == 3 ? bytes[start] & 0x0F : bytes[start] & 0x07;
            for (int i = 1; i < len; i++) cp = (cp << 6) | (bytes[start + i] & 0x3F);
            return cp;
        }

        private static int ToChar(Mapping m, long bytePos)
        {
            long clamped = Math.Max(0, Math.Min(bytePos, m.ByteToChar.Length - 1));
            return m.ByteToChar[clamped];
        }

        private static int ToByte(Mapping m, int charPos)
        {
            int clamped = Math.Max(0, Math.Min(charPos, m.CharToByte.Length - 1));
            return m.CharToByte[clamped];
        }

        public string GetText()
        {
            return Load().Text;
        }

        public List<SelRange> GetSelections()
        {
            Mapping m = Load();
            int n = (int)Send(NppApi.SciGetSelections);
            int main = (int)Send(NppApi.SciGetMainSelection);
            var result = new List<SelRange>();
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < n; i++)
                {
                    bool isMain = i == main;
                    if ((pass == 0) != isMain) continue;
                    long anchor = (long)Send(NppApi.SciGetSelectionNAnchor, i);
                    long caret = (long)Send(NppApi.SciGetSelectionNCaret, i);
                    result.Add(new SelRange(ToChar(m, anchor), ToChar(m, caret)));
                }
            }
            if (result.Count == 0) result.Add(new SelRange(0, 0));
            return result;
        }

        public void SetSelections(List<SelRange> sels)
        {
            if (sels == null || sels.Count == 0) return;
            Mapping m = Load();
            Send(NppApi.SciClearSelections);
            Send(NppApi.SciSetSelection, ToByte(m, sels[0].Active), ToByte(m, sels[0].Anchor));
            for (int i = 1; i < sels.Count; i++)
            {
                Send(NppApi.SciAddSelection, ToByte(m, sels[i].Active), ToByte(m, sels[i].Anchor));
            }
            Send(NppApi.SciSetMainSelection, 0);
            Send(NppApi.SciScrollCaret);
        }

        public unsafe void Edit(List<TextEdit> edits)
        {
            if (edits == null || edits.Count == 0) return;
            Mapping m = Load();
            var ordered = new List<TextEdit>(edits);
            ordered.Sort((a, b) => b.Start.CompareTo(a.Start));
            Send(NppApi.SciBeginUndoAction);
            try
            {
                foreach (TextEdit e in ordered)
                {
                    int startB = ToByte(m, e.Start);
                    int endB = ToByte(m, e.End);
                    Send(NppApi.SciSetTargetRange, startB, endB);
                    string text = e.Text ?? "";
                    int byteCount = Encoding.UTF8.GetByteCount(text);
                    var utf8 = new byte[byteCount + 1];
                    Encoding.UTF8.GetBytes(text, 0, text.Length, utf8, 0);
                    fixed (byte* p = utf8)
                    {
                        NppApi.SendMessage(sci, NppApi.SciReplaceTarget, (IntPtr)byteCount, (IntPtr)p);
                    }
                }
            }
            finally
            {
                Send(NppApi.SciEndUndoAction);
            }
        }

        public bool IsWritable()
        {
            return (int)Send(NppApi.SciGetReadOnly) == 0;
        }

        public LineRange VisibleLineRange()
        {
            long firstVisible = (long)Send(NppApi.SciGetFirstVisibleLine);
            long onScreen = (long)Send(NppApi.SciLinesOnScreen);
            long first = (long)Send(NppApi.SciDocLineFromVisible, firstVisible);
            long last = (long)Send(NppApi.SciDocLineFromVisible, firstVisible + Math.Max(onScreen - 1, 0));
            return new LineRange((int)first, (int)last);
        }

        public void Undo()
        {
            Send(NppApi.SciUndo);
        }

        public void CloseEditor()
        {
            NppApi.SendMessage(npp, NppApi.NppmMenuCommand, IntPtr.Zero, (IntPtr)NppApi.IdmFileClose);
        }

        public OffsetRange SymbolRangeAt(int offset)
        {
            return null;
        }

        private bool grabIndicatorReady;

        internal void HighlightGrab(OffsetRange grab)
        {
            if (!grabIndicatorReady)
            {
                Send(NppApi.SciIndicSetStyle, NppApi.GrabIndicator, NppApi.IndicStraightBox);
                Send(NppApi.SciIndicSetFore, NppApi.GrabIndicator, 0x33CC33);
                Send(NppApi.SciIndicSetAlpha, NppApi.GrabIndicator, 60);
                Send(NppApi.SciIndicSetUnder, NppApi.GrabIndicator, 1);
                grabIndicatorReady = true;
            }
            int len = (int)Send(NppApi.SciGetLength);
            Send(NppApi.SciSetIndicatorCurrent, NppApi.GrabIndicator);
            Send(NppApi.SciIndicatorClearRange, 0, len);
            if (grab == null || grab.End <= grab.Start) return;
            Mapping m = Load();
            int sb = ToByte(m, grab.Start);
            int eb = ToByte(m, grab.End);
            Send(NppApi.SciIndicatorFillRange, sb, eb - sb);
        }

        private bool matchIndicatorReady;

        internal void HighlightMatches(List<OffsetRange> ranges)
        {
            if (!matchIndicatorReady)
            {
                Send(NppApi.SciIndicSetStyle, NppApi.AvyMatchIndicator, NppApi.IndicStraightBox);
                Send(NppApi.SciIndicSetFore, NppApi.AvyMatchIndicator, 0x00D7FF);
                Send(NppApi.SciIndicSetAlpha, NppApi.AvyMatchIndicator, 70);
                Send(NppApi.SciIndicSetUnder, NppApi.AvyMatchIndicator, 1);
                matchIndicatorReady = true;
            }
            int len = (int)Send(NppApi.SciGetLength);
            Send(NppApi.SciSetIndicatorCurrent, NppApi.AvyMatchIndicator);
            Send(NppApi.SciIndicatorClearRange, 0, len);
            if (ranges == null || ranges.Count == 0) return;
            Mapping m = Load();
            foreach (OffsetRange r in ranges)
            {
                int sb = ToByte(m, r.Start);
                int eb = ToByte(m, r.End);
                if (eb > sb) Send(NppApi.SciIndicatorFillRange, sb, eb - sb);
            }
        }

        internal void ClearMatches()
        {
            int len = (int)Send(NppApi.SciGetLength);
            Send(NppApi.SciSetIndicatorCurrent, NppApi.AvyMatchIndicator);
            Send(NppApi.SciIndicatorClearRange, 0, len);
        }

        internal int TextHeight()
        {
            return (int)Send(NppApi.SciTextHeight, 0);
        }

        internal List<AvyOverlay.Label> ResolveLabels(List<AvyLabel> labels)
        {
            var outLabels = new List<AvyOverlay.Label>();
            if (labels == null || labels.Count == 0) return outLabels;
            Mapping m = Load();
            foreach (AvyLabel lb in labels)
            {
                int b = ToByte(m, lb.Offset);
                int x = (int)Send(NppApi.SciPointXFromPosition, 0, b);
                int y = (int)Send(NppApi.SciPointYFromPosition, 0, b);
                outLabels.Add(new AvyOverlay.Label(lb.Label, x, y));
            }
            return outLabels;
        }

        internal IntPtr Handle => sci;
    }
}
