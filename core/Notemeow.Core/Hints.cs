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

namespace Notemeow.Core
{
    public static class Hints
    {
        private const int ExpandDigitCount = 10;

        public static List<int> ExpandHintPositions(Ctx ctx)
        {
            return ExpandHintPositions(ctx, ExpandDigitCount);
        }

        public static List<int> ExpandHintPositions(Ctx ctx, int count)
        {
            MeowState st = ctx.St;
            string text = ctx.Port.GetText();
            SelRange sel = ctx.Port.GetSelections()[0];
            if (sel.Anchor == sel.Active) return new List<int>();
            int caret = sel.Active;
            bool backward = caret < sel.Anchor;
            var outPositions = new List<int>();
            switch (st.SelType)
            {
                case SelType.Word:
                case SelType.Symbol:
                    {
                        Func<char, bool> pred = Text.CharPred(st.SelType == SelType.Symbol);
                        int i = caret;
                        for (int k = 0; k < count; k++)
                        {
                            i =
                                backward
                                    ? Text.Words.PrevStart(text, i, 1, pred)
                                    : Text.Words.NextEnd(text, i, 1, pred);
                            if (backward ? i <= 0 : i >= text.Length) break;
                            outPositions.Add(i);
                        }
                        break;
                    }
                case SelType.Line:
                    {
                        int ln = Text.LineOfOffset(text, caret);
                        for (int k = 0; k < count; k++)
                        {
                            ln += backward ? -1 : 1;
                            if (ln < 0 || ln > Text.LineCount(text) - 1) break;
                            outPositions.Add(
                                backward ? Text.LineStart(text, ln) : Text.LineEnd(text, ln));
                        }
                        break;
                    }
                case SelType.Find:
                case SelType.Till:
                    {
                        char? c = st.LastFind;
                        if (c == null) return outPositions;
                        bool till = st.SelType == SelType.Till;
                        for (int k = 1; k <= count; k++)
                        {
                            int t = Text.NthCharTarget(text, c.Value, caret, k, backward, till);
                            if (t < 0) break;
                            outPositions.Add(t);
                        }
                        break;
                    }
                default:
                    break;
            }
            var seen = new HashSet<int>();
            var unique = new List<int>();
            foreach (int p in outPositions)
            {
                if (seen.Add(p)) unique.Add(p);
            }
            return unique;
        }
    }
}
