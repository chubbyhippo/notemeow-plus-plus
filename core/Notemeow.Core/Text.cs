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
using System.Text.RegularExpressions;

namespace Notemeow.Core
{
    public static class Text
    {
        public static int Clamp(int value, int min, int max)
        {
            return Math.Min(Math.Max(value, min), max);
        }

        public static string EscapeRegExp(string pattern)
        {
            return Regex.Replace(pattern, "[.*+?^${}()|\\[\\]\\\\]", "\\$0");
        }

        public static int LineOfOffset(string text, int offset)
        {
            int line = 0;
            int end = Clamp(offset, 0, text.Length);
            for (int i = 0; i < end; i++)
            {
                if (text[i] == '\n') line++;
            }
            return line;
        }

        public static int LineCount(string text)
        {
            int lines = 1;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n') lines++;
            }
            return lines;
        }

        public static int LineStart(string text, int line)
        {
            if (line <= 0) return 0;
            int newlinesSeen = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n' && ++newlinesSeen == line) return i + 1;
            }
            return text.Length;
        }

        public static int LineEnd(string text, int line)
        {
            int start = LineStart(text, line);
            int newline = text.IndexOf('\n', Math.Min(start, text.Length));
            if (newline < 0) return text.Length;
            return newline > start && text[newline - 1] == '\r' ? newline - 1 : newline;
        }

        public static bool IsWordChar(char ch)
        {
            return char.IsLetterOrDigit(ch);
        }

        public static bool IsSymbolChar(char ch)
        {
            return IsWordChar(ch) || ch == '_' || ch == '$';
        }

        public static Func<char, bool> CharPred(bool symbol)
        {
            return symbol ? (Func<char, bool>)IsSymbolChar : IsWordChar;
        }

        public static int IndexOfChar(string text, char ch, int from)
        {
            for (int i = Math.Max(from, 0); i < text.Length; i++)
            {
                if (text[i] == ch) return i;
            }
            return -1;
        }

        public static int LastIndexOfChar(string text, char ch, int from)
        {
            for (int i = Math.Min(from, text.Length - 1); i >= 0; i--)
            {
                if (text[i] == ch) return i;
            }
            return -1;
        }

        public static int NthCharTarget(
            string text, char ch, int caret, int count, bool backward, bool till)
        {
            int found = -1;
            int from = backward ? (till ? caret - 2 : caret - 1) : (till ? caret + 1 : caret);
            for (int step = 0; step < count; step++)
            {
                found = backward ? LastIndexOfChar(text, ch, from) : IndexOfChar(text, ch, from);
                if (found < 0) return -1;
                from = backward ? found - 1 : found + 1;
            }
            if (found < 0) return -1;
            if (backward) return till ? found + 1 : found;
            return till ? found : found + 1;
        }

        public const string SentenceEnders = ".!?";

        private static bool IsSentenceGap(char ch)
        {
            return char.IsWhiteSpace(ch) || SentenceEnders.IndexOf(ch) >= 0;
        }

        public static int NextSentenceEnd(string text, int from, int count)
        {
            int i = Clamp(from, 0, text.Length);
            for (int step = 0; step < count; step++)
            {
                while (i < text.Length && SentenceEnders.IndexOf(text[i]) < 0) i++;
                while (i < text.Length && SentenceEnders.IndexOf(text[i]) >= 0) i++;
                while (i < text.Length && char.IsWhiteSpace(text[i])) i++;
            }
            return i;
        }

        public static int PrevSentenceStart(string text, int from, int count)
        {
            int i = Clamp(from, 0, text.Length);
            for (int step = 0; step < count; step++)
            {
                while (i > 0 && IsSentenceGap(text[i - 1])) i--;
                while (i > 0 && !IsSentenceGap(text[i - 1])) i--;
            }
            return i;
        }

        private static int LineStartAt(string text, int offset)
        {
            int i = offset;
            while (i > 0 && text[i - 1] != '\n') i--;
            return i;
        }

        private static int FollowingLineStart(string text, int bol)
        {
            int i = bol;
            while (i < text.Length && text[i] != '\n') i++;
            return i < text.Length ? i + 1 : i;
        }

        private static bool BlankLineAt(string text, int bol)
        {
            int i = bol;
            while (i < text.Length && text[i] != '\n')
            {
                if (!char.IsWhiteSpace(text[i])) return false;
                i++;
            }
            return true;
        }

        public static int NextParagraphEnd(string text, int from, int count)
        {
            int pos = Clamp(from, 0, text.Length);
            for (int step = 0; step < count; step++)
            {
                int i = LineStartAt(text, pos);
                while (i < text.Length && BlankLineAt(text, i)) i = FollowingLineStart(text, i);
                while (i < text.Length && !BlankLineAt(text, i)) i = FollowingLineStart(text, i);
                pos = i;
            }
            return pos;
        }

        public static int PrevParagraphStart(string text, int from, int count)
        {
            int pos = Clamp(from, 0, text.Length);
            for (int step = 0; step < count; step++)
            {
                if (pos > 0)
                {
                    int start = ParagraphStartBefore(text, pos);
                    pos = start < pos ? start : ParagraphStartBefore(text, start - 1);
                }
            }
            return pos;
        }

        private static int ParagraphStartBefore(string text, int offset)
        {
            int i = LineStartAt(text, offset);
            while (i > 0 && BlankLineAt(text, i)) i = LineStartAt(text, i - 1);
            while (i > 0 && !BlankLineAt(text, LineStartAt(text, i - 1))) i = LineStartAt(text, i - 1);
            bool prevLineEmpty = i > 0 && text[i - 1] == '\n' && (i == 1 || text[i - 2] == '\n');
            return prevLineEmpty ? i - 1 : i;
        }

        public static class Words
        {
            public static int NextEnd(string text, int from, int count, Func<char, bool> isWord)
            {
                int i = Clamp(from, 0, text.Length);
                for (int step = 0; step < count; step++)
                {
                    while (i < text.Length && !isWord(text[i])) i++;
                    while (i < text.Length && isWord(text[i])) i++;
                }
                return i;
            }

            public static int PrevStart(string text, int from, int count, Func<char, bool> isWord)
            {
                int i = Clamp(from, 0, text.Length);
                for (int step = 0; step < count; step++)
                {
                    while (i > 0 && !isWord(text[i - 1])) i--;
                    while (i > 0 && isWord(text[i - 1])) i--;
                }
                return i;
            }

            public static int FixSelectionMark(
                string text, int pos, int mark, Func<char, bool> isWord)
            {
                int probe = Clamp(mark > pos ? pos : pos - 1, 0, Math.Max(text.Length - 1, 0));
                int[] bounds = BoundsAt(text, probe, isWord);
                if (bounds == null) return mark;
                return mark > pos ? Math.Min(mark, bounds[1]) : Math.Max(mark, bounds[0]);
            }

            public static int[] BoundsAt(string text, int offset, Func<char, bool> isWord)
            {
                int inWord = OffsetInWord(text, offset, isWord);
                if (inWord < 0) return null;
                int start = inWord;
                int end = inWord;
                while (start > 0 && isWord(text[start - 1])) start--;
                while (end < text.Length && isWord(text[end])) end++;
                return [start, end];
            }

            private static int OffsetInWord(string text, int offset, Func<char, bool> isWord)
            {
                if (offset < text.Length && isWord(text[offset])) return offset;
                if (offset > 0 && isWord(text[offset - 1])) return offset - 1;
                int scan = offset;
                while (scan < text.Length && !isWord(text[scan])) scan++;
                return scan < text.Length ? scan : -1;
            }
        }

        public static bool IsBlank(char ch)
        {
            return ch == ' ' || ch == '\t';
        }
    }
}
