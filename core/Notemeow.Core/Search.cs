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
using System.Text.RegularExpressions;

namespace Notemeow.Core
{
    public static class Search
    {
        internal static readonly Dictionary<string, MeowCommand> Commands =
            new Dictionary<string, MeowCommand>
            {
                ["meow-search"] = DoSearch,
                ["meow-visit"] = Visit,
            };

        private const int MaxSearchHistory = 50;

        public static void Push(MeowState st, string pattern)
        {
            st.SearchHistory.RemoveAll(p => p == pattern);
            st.SearchHistory.Add(pattern);
            while (st.SearchHistory.Count > MaxSearchHistory) st.SearchHistory.RemoveAt(0);
        }

        private sealed class SearchMatch
        {
            public SearchMatch(int start, int end)
            {
                Start = start;
                End = end;
            }

            public int Start { get; }
            public int End { get; }
        }

        private static bool FullyMatches(string pattern, string s)
        {
            try
            {
                return Regex.IsMatch(s, "^(?:" + pattern + ")$");
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        private static List<SearchMatch> AllMatches(string text, string pattern)
        {
            Regex re;
            try
            {
                re = new Regex(pattern);
            }
            catch (ArgumentException)
            {
                re = new Regex(Regex.Escape(pattern));
            }
            var outMatches = new List<SearchMatch>();
            int from = 0;
            while (from <= text.Length)
            {
                Match m = re.Match(text, from);
                if (!m.Success) break;
                if (m.Length == 0)
                {
                    from = m.Index + 1;
                    continue;
                }
                outMatches.Add(new SearchMatch(m.Index, m.Index + m.Length));
                from = m.Index + m.Length;
            }
            return outMatches;
        }

        private static void DoSearch(Ctx ctx)
        {
            MeowState st = ctx.St;
            SelRange sel = Selections.Primary(ctx);
            string pattern =
                st.SearchHistory.Count == 0
                    ? null
                    : st.SearchHistory[st.SearchHistory.Count - 1];
            if (Selections.HasSelection(sel))
            {
                int lo = Math.Min(sel.Anchor, sel.Active);
                int hi = Math.Max(sel.Anchor, sel.Active);
                string selText = ctx.Port.GetText().Substring(lo, hi - lo);
                if (selText.Length != 0 && (pattern == null || !FullyMatches(pattern, selText)))
                {
                    pattern = Text.EscapeRegExp(selText);
                    Push(st, pattern);
                }
            }
            if (pattern == null)
            {
                ctx.Ui.Hint("No search pattern");
                return;
            }
            SearchWith(ctx, pattern, st.TakeCount(1) < 0 || Selections.BackwardP(ctx));
        }

        private static void Visit(Ctx ctx)
        {
            bool backward = ctx.St.TakeCount(1) < 0;
            string input = ctx.Ui.Input("Visit (regexp):", null);
            if (input == null || input.Length == 0) return;
            string pattern = input;
            try
            {
                _ = new Regex(pattern);
            }
            catch (ArgumentException)
            {
                pattern = Text.EscapeRegExp(input);
            }
            Push(ctx.St, pattern);
            SearchWith(ctx, pattern, backward);
        }

        private static void SearchWith(Ctx ctx, string pattern, bool backward)
        {
            int caret = Selections.Primary(ctx).Active;
            List<SearchMatch> matches = AllMatches(ctx.Port.GetText(), pattern);
            SearchMatch m = null;
            if (!backward)
            {
                foreach (SearchMatch x in matches)
                {
                    if (x.Start >= caret)
                    {
                        m = x;
                        break;
                    }
                }
                if (m == null && matches.Count > 0) m = matches[0];
            }
            else
            {
                foreach (SearchMatch x in matches)
                {
                    if (x.End <= caret) m = x;
                }
                if (m == null && matches.Count > 0) m = matches[matches.Count - 1];
            }
            if (m == null)
            {
                ctx.Ui.Hint("No match: " + pattern);
                return;
            }
            if (!backward)
            {
                Selections.Select(ctx, SelType.Visit, m.Start, m.End, false);
            }
            else
            {
                Selections.Select(ctx, SelType.Visit, m.End, m.Start, false);
            }
        }
    }
}
