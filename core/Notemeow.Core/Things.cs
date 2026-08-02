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
            return ch switch
            {
                'r' => Pair(text, offset, '(', ')', inner),
                's' => Pair(text, offset, '[', ']', inner),
                'c' => Pair(text, offset, '{', '}', inner),
                'g' => StringThing(text, offset, inner),
                'e' => Symbol(text, offset),
                'w' => Window(ctx, text),
                'b' => new OffsetRange(0, text.Length),
                'p' => Paragraph(text, offset, inner),
                'l' => Line(text, offset, inner),
                'v' => Line(text, offset, true),
                'd' => Defun(ctx, text, offset),
                '.' => Sentence(text, offset, inner),
                _ => null,
            };
        }

        internal static OffsetRange Pair(string text, int offset, char open, char close, bool inner)
        {
            int depth = 0;
            int start = -1;
            for (int i = offset - 1; i >= 0; i--)
            {
                char ch = text[i];
                if (ch == close)
                {
                    depth++;
                }
                else if (ch == open)
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
                char ch = text[j];
                if (ch == open && j != start)
                {
                    depth++;
                }
                else if (ch == close)
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
            int length = text.Length;
            int i = 0;
            while (i < length)
            {
                char quote = text[i];
                if (quote == '"' || quote == '\'' || quote == '`')
                {
                    bool triple =
                        i + 2 < length && text[i + 1] == quote && text[i + 2] == quote;
                    int len = triple ? 3 : 1;
                    int open = i;
                    int j = i + len;
                    int closeEnd = -1;
                    while (j < length)
                    {
                        char ch = text[j];
                        if (!triple && ch == '\n') break;
                        if (ch == '\\')
                        {
                            j += 2;
                            continue;
                        }
                        bool closes =
                            !triple
                                || (j + 2 < length
                                    && text[j + 1] == quote
                                    && text[j + 2] == quote);
                        if (ch == quote && closes)
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
            int inSymbol = offset;
            if (inSymbol >= text.Length || !Text.IsSymbolChar(text[inSymbol]))
            {
                if (inSymbol > 0 && Text.IsSymbolChar(text[inSymbol - 1])) inSymbol--;
                else return null;
            }
            int start = inSymbol;
            int end = inSymbol;
            while (start > 0 && Text.IsSymbolChar(text[start - 1])) start--;
            while (end < text.Length && Text.IsSymbolChar(text[end])) end++;
            return new OffsetRange(start, end);
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
            int caretLine = Text.LineOfOffset(text, Text.Clamp(offset, 0, text.Length));
            if (Blank(text, caretLine)) return null;
            int first = caretLine;
            int last = caretLine;
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
            int caretLine = Text.LineOfOffset(text, Text.Clamp(offset, 0, text.Length));
            int end = Text.LineEnd(text, caretLine);
            return inner
                ? new OffsetRange(Text.LineStart(text, caretLine), end)
                : new OffsetRange(Text.LineStart(text, caretLine), Text.LineStart(text, caretLine + 1));
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
            int start = Text.Clamp(offset, 0, text.Length - 1);
            while (start > 0)
            {
                char ch = text[start - 1];
                if (enders.IndexOf(ch) >= 0
                    || (ch == '\n' && start > 1 && text[start - 2] == '\n')) break;
                start--;
            }
            while (start < text.Length && char.IsWhiteSpace(text[start])) start++;
            int end = Text.Clamp(offset, 0, text.Length);
            while (end < text.Length
                && enders.IndexOf(text[end]) < 0
                && !(text[end] == '\n' && end + 1 < text.Length && text[end + 1] == '\n'))
            {
                end++;
            }
            if (end < text.Length && enders.IndexOf(text[end]) >= 0) end++;
            if (end <= start) return null;
            if (inner) return new OffsetRange(start, end);
            int withTrailingSpace = end;
            while (withTrailingSpace < text.Length && text[withTrailingSpace] == ' ')
                withTrailingSpace++;
            return new OffsetRange(start, withTrailingSpace);
        }

        internal static bool Blank(string text, int line)
        {
            int start = Text.LineStart(text, line);
            int end = Text.LineEnd(text, line);
            return text.Substring(start, end - start).Trim().Length == 0;
        }
    }
}
