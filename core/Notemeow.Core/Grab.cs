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
    public static class Grab
    {
        internal static readonly Dictionary<string, MeowCommand> Commands =
            new Dictionary<string, MeowCommand>
            {
                ["meow-grab"] = DoGrab,
                ["meow-sync-grab"] = Sync,
                ["meow-swap-grab"] = Swap,
            };

        private const int MaxBeacons = 500;

        public static void Clear(Ctx ctx)
        {
            ctx.St.Grab = null;
        }

        private static void Set(Ctx ctx, int start, int end)
        {
            ctx.St.Grab = new OffsetRange(start, end);
        }

        public static void AdjustForEdits(MeowState st, List<TextEdit> edits)
        {
            OffsetRange g = st.Grab;
            if (g == null) return;
            int gs = g.Start;
            int ge = g.End;
            var ordered = new List<TextEdit>(edits);
            ordered.Sort((a, b) => b.Start.CompareTo(a.Start));
            foreach (TextEdit e in ordered)
            {
                int delta = e.Text.Length - (e.End - e.Start);
                if (gs >= e.End)
                {
                    gs += delta;
                    ge += delta;
                }
                else
                {
                    if (ge >= e.End) ge += delta;
                    else if (ge > e.Start) ge = e.Start;
                    if (gs > e.Start) gs = e.Start;
                }
            }
            if (ge < gs) ge = gs;
            st.Grab = new OffsetRange(gs, ge);
        }

        private static void DoGrab(Ctx ctx)
        {
            Clear(ctx);
            SelRange sel = Selections.Primary(ctx);
            if (Selections.HasSelection(sel))
            {
                Set(ctx, sel.Lo(), sel.Hi());
            }
            Selections.Cancel(ctx);
        }

        private static void Sync(Ctx ctx)
        {
            SelRange sel = Selections.Primary(ctx);
            if (!Selections.HasSelection(sel))
            {
                ctx.Ui.Hint("meow-sync-grab needs a selection");
                return;
            }
            Clear(ctx);
            Set(ctx, sel.Lo(), sel.Hi());
            Selections.Cancel(ctx);
        }

        private static void Swap(Ctx ctx)
        {
            if (Edits.BlockedReadOnly(ctx)) return;
            MeowState st = ctx.St;
            OffsetRange g = st.Grab;
            SelRange sel = Selections.Primary(ctx);
            if (g == null)
            {
                ctx.Ui.Hint("No grab");
                return;
            }
            if (!Selections.HasSelection(sel))
            {
                ctx.Ui.Hint("meow-swap-grab needs a selection");
                return;
            }
            int gs = g.Start;
            int ge = g.End;
            int ss = sel.Lo();
            int se = sel.Hi();
            if (Math.Max(gs, ss) < Math.Min(ge, se) && !(gs == ss && ge == se))
            {
                ctx.Ui.Hint("Selection overlaps the grab");
                return;
            }
            string text = ctx.Port.GetText();
            string grabText = text.Substring(gs, ge - gs);
            string selText = text.Substring(ss, se - ss);
            st.Grab = null;
            ctx.Port.Edit(
                new List<TextEdit>
                {
                    new TextEdit(ss, se, grabText),
                    new TextEdit(gs, ge, selText),
                });
            if (gs <= ss)
            {
                int delta = selText.Length - (ge - gs);
                Set(ctx, gs, gs + selText.Length);
                int caret = ss + delta + grabText.Length;
                ctx.Port.SetSelections(new List<SelRange> { new SelRange(caret, caret) });
            }
            else
            {
                int delta = grabText.Length - (se - ss);
                Set(ctx, gs + delta, gs + delta + selText.Length);
                int caret = ss + grabText.Length;
                ctx.Port.SetSelections(new List<SelRange> { new SelRange(caret, caret) });
            }
            st.SelType = SelType.None;
        }

        public static bool Pop(Ctx ctx)
        {
            OffsetRange g = ctx.St.Grab;
            if (g == null) return false;
            int start = g.Start;
            int end = g.End;
            Clear(ctx);
            Selections.Select(ctx, SelType.Transient, start, end, false);
            return true;
        }

        public static void Beacon(Ctx ctx)
        {
            MeowState st = ctx.St;
            OffsetRange g = st.Grab;
            if (g == null || g.End <= g.Start) return;
            SelRange sel = Selections.Primary(ctx);
            if (!Selections.HasSelection(sel)) return;
            int ss = sel.Lo();
            int se = sel.Hi();
            if (ss < g.Start || se > g.End || se == ss) return;
            string text = ctx.Port.GetText();
            var sels = new List<SelRange>();
            switch (st.SelType)
            {
                case SelType.Word:
                case SelType.Symbol:
                case SelType.Visit:
                case SelType.Find:
                case SelType.Till:
                case SelType.Char:
                    {
                        string selText = text.Substring(ss, se - ss);
                        if (selText.Trim().Length == 0) return;
                        bool bounded =
                            st.SelType == SelType.Word || st.SelType == SelType.Symbol;
                        string pat =
                            bounded
                                ? "\\b" + Text.EscapeRegExp(selText) + "\\b"
                                : Text.EscapeRegExp(selText);
                        Regex re;
                        try
                        {
                            re = new Regex(pat);
                        }
                        catch (ArgumentException)
                        {
                            return;
                        }
                        string region = text.Substring(g.Start, g.End - g.Start);
                        int rlen = g.End - g.Start;
                        int added = 0;
                        int from = 0;
                        while (from <= rlen)
                        {
                            Match m = re.Match(region, from);
                            if (!m.Success) break;
                            int rs = m.Index;
                            int reEnd = m.Index + m.Length;
                            if (reEnd == rs)
                            {
                                from = reEnd + 1;
                                continue;
                            }
                            int s0 = g.Start + rs;
                            int e0 = g.Start + reEnd;
                            if (s0 != ss)
                            {
                                sels.Add(new SelRange(s0, e0));
                                if (++added >= MaxBeacons) break;
                            }
                            from = reEnd;
                        }
                        if (sels.Count == 0) return;
                        sels.Insert(0, new SelRange(ss, se));
                        break;
                    }
                case SelType.Line:
                    {
                        int first = Text.LineOfOffset(text, g.Start);
                        int last = Text.LineOfOffset(text, Math.Max(g.End - 1, g.Start));
                        if (last <= first) return;
                        for (int ln = first; ln <= last; ln++)
                        {
                            sels.Add(
                                new SelRange(Text.LineStart(text, ln), Text.LineEnd(text, ln)));
                        }
                        break;
                    }
                default:
                    return;
            }
            ctx.Port.SetSelections(sels);
        }
    }
}
