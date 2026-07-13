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

namespace Notemeow.Core
{
    public static class Things
    {
        public static OffsetRange Inner(Ctx ctx, char ch, int offset)
        {
            return Compute(ctx, ch, offset, true);
        }

        public static OffsetRange Bounds(Ctx ctx, char ch, int offset)
        {
            return Compute(ctx, ch, offset, false);
        }

        private static OffsetRange Compute(Ctx ctx, char ch, int offset, bool inner)
        {
            string text = ctx.Port.GetText();
            switch (ch)
            {
                case 'r':
                    return Pair(text, offset, '(', ')', inner);
                case 's':
                    return Pair(text, offset, '[', ']', inner);
                case 'c':
                    return Pair(text, offset, '{', '}', inner);
                case 'g':
                    return StringThing(text, offset, inner);
                case 'e':
                    return Symbol(text, offset);
                case 'w':
                    return Window(ctx, text);
                case 'b':
                    return new OffsetRange(0, text.Length);
                case 'p':
                    return Paragraph(text, offset, inner);
                case 'l':
                    return Line(text, offset, inner);
                case 'v':
                    return Line(text, offset, true);
                case 'd':
                    return Defun(ctx, text, offset);
                case '.':
                    return Sentence(text, offset, inner);
                default:
                    return null;
            }
        }

        internal static OffsetRange Pair(string text, int offset, char open, char close, bool inner)
        {
            int depth = 0;
            int start = -1;
            for (int i = offset - 1; i >= 0; i--)
            {
                char c = text[i];
                if (c == close)
                {
                    depth++;
                }
                else if (c == open)
                {
                    if (depth == 0)
                    {
                        start = i;
                        break;
                    }
                    depth--;
                }
            }
            if (start < 0) return null;
            depth = 0;
            int end = -1;
            for (int j = offset; j < text.Length; j++)
            {
                char c = text[j];
                if (c == open && j != start)
                {
                    depth++;
                }
                else if (c == close)
                {
                    if (depth == 0)
                    {
                        end = j;
                        break;
                    }
                    depth--;
                }
            }
            if (end < 0) return null;
            return inner ? new OffsetRange(start + 1, end) : new OffsetRange(start, end + 1);
        }

        private static OffsetRange StringThing(string text, int offset, bool inner)
        {
            int n = text.Length;
            int i = 0;
            while (i < n)
            {
                char c = text[i];
                if (c == '"' || c == '\'' || c == '`')
                {
                    bool triple = i + 2 < n && text[i + 1] == c && text[i + 2] == c;
                    int len = triple ? 3 : 1;
                    int open = i;
                    int j = i + len;
                    int closeEnd = -1;
                    while (j < n)
                    {
                        char d = text[j];
                        if (!triple && d == '\n') break;
                        if (d == '\\')
                        {
                            j += 2;
                            continue;
                        }
                        bool closes =
                            !triple
                                || (j + 2 < n && text[j + 1] == c && text[j + 2] == c);
                        if (d == c && closes)
                        {
                            closeEnd = j + len;
                            break;
                        }
                        j++;
                    }
                    if (closeEnd < 0)
                    {
                        i = open + len;
                        continue;
                    }
                    if (offset >= open && offset < closeEnd)
                    {
                        return inner
                            ? new OffsetRange(open + len, closeEnd - len)
                            : new OffsetRange(open, closeEnd);
                    }
                    i = closeEnd;
                    continue;
                }
                i++;
            }
            return null;
        }

        private static OffsetRange Symbol(string text, int offset)
        {
            int o = offset;
            if (o >= text.Length || !Text.IsSymbolChar(text[o]))
            {
                if (o > 0 && Text.IsSymbolChar(text[o - 1])) o--;
                else return null;
            }
            int s = o;
            int e = o;
            while (s > 0 && Text.IsSymbolChar(text[s - 1])) s--;
            while (e < text.Length && Text.IsSymbolChar(text[e])) e++;
            return new OffsetRange(s, e);
        }

        private static OffsetRange Window(Ctx ctx, string text)
        {
            LineRange vis = ctx.Port.VisibleLineRange();
            int last = Text.LineCount(text) - 1;
            int first = Text.Clamp(vis != null ? vis.First : 0, 0, Math.Max(last, 0));
            int stop = Text.Clamp(vis != null ? vis.Last : last, 0, Math.Max(last, 0));
            return new OffsetRange(Text.LineStart(text, first), Text.LineEnd(text, stop));
        }

        private static OffsetRange Paragraph(string text, int offset, bool inner)
        {
            if (text.Length == 0) return null;
            int count = Text.LineCount(text);
            int ln = Text.LineOfOffset(text, Text.Clamp(offset, 0, text.Length));
            if (Blank(text, ln)) return null;
            int first = ln;
            int last = ln;
            while (first > 0 && !Blank(text, first - 1)) first--;
            while (last < count - 1 && !Blank(text, last + 1)) last++;
            int start = Text.LineStart(text, first);
            if (inner) return new OffsetRange(start, Text.LineEnd(text, last));
            int stop = last;
            while (stop < count - 1 && Blank(text, stop + 1)) stop++;
            int end = stop < count - 1 ? Text.LineStart(text, stop + 1) : Text.LineEnd(text, stop);
            return new OffsetRange(start, end);
        }

        private static OffsetRange Line(string text, int offset, bool inner)
        {
            int ln = Text.LineOfOffset(text, Text.Clamp(offset, 0, text.Length));
            int end = Text.LineEnd(text, ln);
            return inner
                ? new OffsetRange(Text.LineStart(text, ln), end)
                : new OffsetRange(Text.LineStart(text, ln), Math.Min(end + 1, text.Length));
        }

        private static OffsetRange Defun(Ctx ctx, string text, int offset)
        {
            OffsetRange fromHost = ctx.Port.SymbolRangeAt(offset);
            if (fromHost != null) return fromHost;
            OffsetRange b = Pair(text, offset, '{', '}', false);
            if (b == null) return null;
            while (true)
            {
                OffsetRange outer = Pair(text, b.Start, '{', '}', false);
                if (outer == null) break;
                b = outer;
            }
            return b;
        }

        private static OffsetRange Sentence(string text, int offset, bool inner)
        {
            if (text.Length == 0) return null;
            string enders = Text.SentenceEnders;
            int s = Text.Clamp(offset, 0, text.Length - 1);
            while (s > 0)
            {
                char c = text[s - 1];
                if (enders.IndexOf(c) >= 0 || (c == '\n' && s > 1 && text[s - 2] == '\n')) break;
                s--;
            }
            while (s < text.Length && char.IsWhiteSpace(text[s])) s++;
            int e = Text.Clamp(offset, 0, text.Length);
            while (e < text.Length
                && enders.IndexOf(text[e]) < 0
                && !(text[e] == '\n' && e + 1 < text.Length && text[e + 1] == '\n'))
            {
                e++;
            }
            if (e < text.Length && enders.IndexOf(text[e]) >= 0) e++;
            if (e <= s) return null;
            if (inner) return new OffsetRange(s, e);
            int be = e;
            while (be < text.Length && text[be] == ' ') be++;
            return new OffsetRange(s, be);
        }

        internal static bool Blank(string text, int line)
        {
            int s = Text.LineStart(text, line);
            int e = Text.LineEnd(text, line);
            return text.Substring(s, e - s).Trim().Length == 0;
        }
    }
}
