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
            new()
            {
                ["meow-grab"] = DoGrab,
                ["meow-sync-grab"] = Sync,
                ["meow-swap-grab"] = Swap,
            };

        private const int MaxBeacons = 500;

        public static void Clear(Ctx ctx)
        {
            ctx.State.Grab = null;
        }

        private static void Set(Ctx ctx, int start, int end)
        {
            ctx.State.Grab = new OffsetRange(start, end);
        }

        public static void AdjustForEdits(MeowState state, List<TextEdit> edits)
        {
            OffsetRange g = state.Grab;
            if (g == null) return;
            int grabStart = g.Start;
            int grabEnd = g.End;
            var ordered = new List<TextEdit>(edits);
            ordered.Sort((a, b) => b.Start.CompareTo(a.Start));
            foreach (TextEdit e in ordered)
            {
                int delta = e.Text.Length - (e.End - e.Start);
                if (grabStart >= e.End)
                {
                    grabStart += delta;
                    grabEnd += delta;
                }
                else
                {
                    if (grabEnd >= e.End) grabEnd += delta;
                    else if (grabEnd > e.Start) grabEnd = e.Start;
                    if (grabStart > e.Start) grabStart = e.Start;
                }
            }
            if (grabEnd < grabStart) grabEnd = grabStart;
            state.Grab = new OffsetRange(grabStart, grabEnd);
        }

        private static void DoGrab(Ctx ctx)
        {
            Clear(ctx);
            SelRange sel = Selections.Primary(ctx);
            if (Selections.HasSelection(sel))
            {
                Set(ctx, sel.SelStart(), sel.SelEnd());
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
            Set(ctx, sel.SelStart(), sel.SelEnd());
            Selections.Cancel(ctx);
        }

        private static void Swap(Ctx ctx)
        {
            if (Edits.BlockedReadOnly(ctx)) return;
            MeowState state = ctx.State;
            OffsetRange g = state.Grab;
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
            int grabStart = g.Start;
            int grabEnd = g.End;
            int selStart = sel.SelStart();
            int selEnd = sel.SelEnd();
            if (Math.Max(grabStart, selStart) < Math.Min(grabEnd, selEnd) && !(grabStart == selStart && grabEnd == selEnd))
            {
                ctx.Ui.Hint("Selection overlaps the grab");
                return;
            }
            string text = ctx.Port.GetText();
            string grabText = text.Substring(grabStart, grabEnd - grabStart);
            string selText = text.Substring(selStart, selEnd - selStart);
            state.Grab = null;
            ctx.Port.Edit(
                [
                    new TextEdit(selStart, selEnd, grabText),
                    new TextEdit(grabStart, grabEnd, selText),
                ]);
            if (grabStart <= selStart)
            {
                int delta = selText.Length - (grabEnd - grabStart);
                Set(ctx, grabStart, grabStart + selText.Length);
                int caret = selStart + delta + grabText.Length;
                ctx.Port.SetSelections([new SelRange(caret, caret)]);
            }
            else
            {
                int delta = grabText.Length - (selEnd - selStart);
                Set(ctx, grabStart + delta, grabStart + delta + selText.Length);
                int caret = selStart + grabText.Length;
                ctx.Port.SetSelections([new SelRange(caret, caret)]);
            }
            state.SelType = SelType.None;
        }

        public static bool Pop(Ctx ctx)
        {
            OffsetRange g = ctx.State.Grab;
            if (g == null) return false;
            int start = g.Start;
            int end = g.End;
            Clear(ctx);
            Selections.Select(ctx, SelType.Transient, start, end, false);
            return true;
        }

        public static void Beacon(Ctx ctx)
        {
            MeowState state = ctx.State;
            OffsetRange g = state.Grab;
            if (g == null || g.End <= g.Start) return;
            SelRange sel = Selections.Primary(ctx);
            if (!Selections.HasSelection(sel)) return;
            int selStart = sel.SelStart();
            int selEnd = sel.SelEnd();
            if (selStart < g.Start || selEnd > g.End || selEnd == selStart) return;
            string text = ctx.Port.GetText();
            var sels = new List<SelRange>();
            switch (state.SelType)
            {
                case SelType.Word:
                case SelType.Symbol:
                case SelType.Visit:
                case SelType.Find:
                case SelType.Till:
                case SelType.Char:
                    {
                        string selText = text.Substring(selStart, selEnd - selStart);
                        if (selText.Trim().Length == 0) return;
                        bool bounded =
                            state.SelType == SelType.Word || state.SelType == SelType.Symbol;
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
                            int matchStart = m.Index;
                            int reEnd = m.Index + m.Length;
                            if (reEnd == matchStart)
                            {
                                from = reEnd + 1;
                                continue;
                            }
                            int s0 = g.Start + matchStart;
                            int e0 = g.Start + reEnd;
                            if (s0 != selStart)
                            {
                                sels.Add(new SelRange(s0, e0));
                                if (++added >= MaxBeacons) break;
                            }
                            from = reEnd;
                        }
                        if (sels.Count == 0) return;
                        sels.Insert(0, new SelRange(selStart, selEnd));
                        break;
                    }
                case SelType.Line:
                    {
                        int first = Text.LineOfOffset(text, g.Start);
                        int last = Text.LineOfOffset(text, Math.Max(g.End - 1, g.Start));
                        if (last <= first) return;
                        for (int line = first; line <= last; line++)
                        {
                            sels.Add(
                                new SelRange(Text.LineStart(text, line), Text.LineEnd(text, line)));
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
