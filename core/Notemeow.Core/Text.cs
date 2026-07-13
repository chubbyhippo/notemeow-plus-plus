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
        public static int Clamp(int n, int lo, int hi)
        {
            return Math.Min(Math.Max(n, lo), hi);
        }

        public static string EscapeRegExp(string s)
        {
            return Regex.Replace(s, "[.*+?^${}()|\\[\\]\\\\]", "\\$0");
        }

        public static int LineOfOffset(string text, int offset)
        {
            int ln = 0;
            int end = Clamp(offset, 0, text.Length);
            for (int i = 0; i < end; i++)
            {
                if (text[i] == '\n') ln++;
            }
            return ln;
        }

        public static int LineCount(string text)
        {
            int n = 1;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n') n++;
            }
            return n;
        }

        public static int LineStart(string text, int line)
        {
            if (line <= 0) return 0;
            int ln = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n' && ++ln == line) return i + 1;
            }
            return text.Length;
        }

        public static int LineEnd(string text, int line)
        {
            int s = LineStart(text, line);
            int nl = text.IndexOf('\n', Math.Min(s, text.Length));
            return nl < 0 ? text.Length : nl;
        }

        public static bool IsWordChar(char c)
        {
            return char.IsLetterOrDigit(c);
        }

        public static bool IsSymbolChar(char c)
        {
            return IsWordChar(c) || c == '_' || c == '$';
        }

        public static Func<char, bool> CharPred(bool symbol)
        {
            return symbol ? (Func<char, bool>)IsSymbolChar : IsWordChar;
        }

        public static int IndexOfChar(string text, char c, int from)
        {
            for (int i = Math.Max(from, 0); i < text.Length; i++)
            {
                if (text[i] == c) return i;
            }
            return -1;
        }

        public static int LastIndexOfChar(string text, char c, int from)
        {
            for (int i = Math.Min(from, text.Length - 1); i >= 0; i--)
            {
                if (text[i] == c) return i;
            }
            return -1;
        }

        public static int NthCharTarget(
            string text, char ch, int caret, int n, bool backward, bool till)
        {
            int found = -1;
            int from = backward ? (till ? caret - 2 : caret - 1) : (till ? caret + 1 : caret);
            for (int k = 0; k < n; k++)
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

        private static bool IsSentenceGap(char c)
        {
            return char.IsWhiteSpace(c) || SentenceEnders.IndexOf(c) >= 0;
        }

        public static int NextSentenceEnd(string text, int from, int n)
        {
            int i = Clamp(from, 0, text.Length);
            for (int k = 0; k < n; k++)
            {
                while (i < text.Length && SentenceEnders.IndexOf(text[i]) < 0) i++;
                while (i < text.Length && SentenceEnders.IndexOf(text[i]) >= 0) i++;
                while (i < text.Length && char.IsWhiteSpace(text[i])) i++;
            }
            return i;
        }

        public static int PrevSentenceStart(string text, int from, int n)
        {
            int i = Clamp(from, 0, text.Length);
            for (int k = 0; k < n; k++)
            {
                while (i > 0 && IsSentenceGap(text[i - 1])) i--;
                while (i > 0 && !IsSentenceGap(text[i - 1])) i--;
            }
            return i;
        }

        public static class Words
        {
            public static int NextEnd(string text, int from, int n, Func<char, bool> pred)
            {
                int i = Clamp(from, 0, text.Length);
                for (int k = 0; k < n; k++)
                {
                    while (i < text.Length && !pred(text[i])) i++;
                    while (i < text.Length && pred(text[i])) i++;
                }
                return i;
            }

            public static int PrevStart(string text, int from, int n, Func<char, bool> pred)
            {
                int i = Clamp(from, 0, text.Length);
                for (int k = 0; k < n; k++)
                {
                    while (i > 0 && !pred(text[i - 1])) i--;
                    while (i > 0 && pred(text[i - 1])) i--;
                }
                return i;
            }

            public static int FixSelectionMark(string text, int pos, int mark, Func<char, bool> pred)
            {
                int probe = Clamp(mark > pos ? pos : pos - 1, 0, Math.Max(text.Length - 1, 0));
                int[] bounds = BoundsAt(text, probe, pred);
                if (bounds == null) return mark;
                return mark > pos ? Math.Min(mark, bounds[1]) : Math.Max(mark, bounds[0]);
            }

            public static int[] BoundsAt(string text, int offset, Func<char, bool> pred)
            {
                int o = offset;
                if (o >= text.Length || !pred(text[o]))
                {
                    if (o > 0 && pred(text[o - 1]))
                    {
                        o--;
                    }
                    else
                    {
                        int f = o;
                        while (f < text.Length && !pred(text[f])) f++;
                        if (f >= text.Length) return null;
                        o = f;
                    }
                }
                int s = o;
                int e = o;
                while (s > 0 && pred(text[s - 1])) s--;
                while (e < text.Length && pred(text[e])) e++;
                return new[] { s, e };
            }
        }
    }
}
