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
    public static class Selections
    {
        internal static readonly Dictionary<string, MeowCommand> Commands = BuildCommands();

        private static Dictionary<string, MeowCommand> BuildCommands()
        {
            var commands = new Dictionary<string, MeowCommand>();
            for (int n = 0; n <= 9; n++)
            {
                int digit = n;
                commands["meow-expand-" + n] = ctx => ExpandOrCount(ctx, digit);
            }
            commands["meow-reverse"] = Reverse;
            commands["meow-cancel-selection"] = CancelAll;
            commands["meow-pop-selection"] = Pop;
            return commands;
        }

        private const int MaxSelectionHistory = 200;
        private const int DigitZeroExpand = 10;

        private static readonly HashSet<SelType> Expandable = new HashSet<SelType>
        {
            SelType.Char,
            SelType.Word,
            SelType.Symbol,
            SelType.Line,
            SelType.Find,
            SelType.Till,
        };

        public static SelRange Primary(Ctx ctx)
        {
            return ctx.Port.GetSelections()[0];
        }

        public static bool HasSelection(SelRange sel)
        {
            return sel.Anchor != sel.Active;
        }

        public static bool BackwardP(Ctx ctx)
        {
            SelRange sel = Primary(ctx);
            return HasSelection(sel) && sel.Active < sel.Anchor;
        }

        public static int Mark(Ctx ctx)
        {
            SelRange sel = Primary(ctx);
            return HasSelection(sel) ? sel.Anchor : sel.Active;
        }

        public static void RecordSelect(
            Ctx ctx, SelType type, int anchor, int active, bool expand, int posBefore)
        {
            MeowState st = ctx.St;
            SavedSelection prev =
                st.LastSelection != null
                    ? st.LastSelection
                    : new SavedSelection(null, false, posBefore, posBefore);
            SavedSelection head =
                st.SelectionHistory.Count > 0
                    ? st.SelectionHistory[st.SelectionHistory.Count - 1]
                    : null;
            if (head == null || !head.Equals(prev)) st.SelectionHistory.Add(prev);
            while (st.SelectionHistory.Count > MaxSelectionHistory)
                st.SelectionHistory.RemoveAt(0);
            st.LastSelection = new SavedSelection(type, expand, anchor, active);
        }

        public static void Select(Ctx ctx, SelType type, int markOff, int point, bool expand)
        {
            Select(ctx, type, markOff, point, expand, true);
        }

        public static void Select(
            Ctx ctx, SelType type, int markOff, int point, bool expand, bool push)
        {
            MeowState st = ctx.St;
            int len = ctx.Port.GetText().Length;
            int m = Text.Clamp(markOff, 0, len);
            int p = Text.Clamp(point, 0, len);
            List<SelRange> sels = ctx.Port.GetSelections();
            if (push) RecordSelect(ctx, type, m, p, expand, sels[0].Active);
            else st.LastSelection = new SavedSelection(type, expand, m, p);
            st.SelType = type;
            st.SelExpand = expand;
            var next = new List<SelRange>(sels);
            next[0] = new SelRange(m, p);
            ctx.Port.SetSelections(next);
            Grab.Beacon(ctx);
            ctx.Ui.ShowExpandHints(Hints.ExpandHintPositions(ctx));
        }

        public static void ResetSelectionMemory(MeowState st)
        {
            st.SelectionHistory.Clear();
            st.LastSelection = null;
        }

        public static void Collapse(Ctx ctx)
        {
            var sels = new List<SelRange>(ctx.Port.GetSelections());
            sels[0] = new SelRange(sels[0].Active, sels[0].Active);
            ctx.Port.SetSelections(sels);
            ctx.St.SelType = SelType.None;
            ctx.St.SelExpand = false;
        }

        public static void Cancel(Ctx ctx)
        {
            Collapse(ctx);
            ResetSelectionMemory(ctx.St);
        }

        public static void CancelAll(Ctx ctx)
        {
            List<SelRange> sels = ctx.Port.GetSelections();
            if (sels.Count > 1) ctx.Port.SetSelections(new List<SelRange> { sels[0] });
            Cancel(ctx);
        }

        private static void Reverse(Ctx ctx)
        {
            SelRange sel = Primary(ctx);
            if (!HasSelection(sel)) return;
            var sels = new List<SelRange>(ctx.Port.GetSelections());
            sels[0] = new SelRange(sel.Active, sel.Anchor);
            ctx.Port.SetSelections(sels);
        }

        private static void Pop(Ctx ctx)
        {
            MeowState st = ctx.St;
            if (HasSelection(Primary(ctx)))
            {
                SavedSelection entry = null;
                if (st.SelectionHistory.Count > 0)
                {
                    entry = st.SelectionHistory[st.SelectionHistory.Count - 1];
                    st.SelectionHistory.RemoveAt(st.SelectionHistory.Count - 1);
                }
                if (entry == null) return;
                if (entry.Type == null)
                {
                    var sels = new List<SelRange>(ctx.Port.GetSelections());
                    sels[0] = new SelRange(entry.Active, entry.Active);
                    ctx.Port.SetSelections(sels);
                    Cancel(ctx);
                    ctx.Ui.Hint("No previous selection");
                }
                else
                {
                    Select(ctx, entry.Type.Value, entry.Anchor, entry.Active, entry.Expand, false);
                }
            }
            else if (!Grab.Pop(ctx))
            {
                ctx.Ui.Hint("No previous selection");
            }
        }

        private static void ExpandOrCount(Ctx ctx, int n)
        {
            MeowState st = ctx.St;
            if (HasSelection(Primary(ctx)) && Expandable.Contains(st.SelType))
            {
                Expand(ctx, n == 0 ? DigitZeroExpand : n);
            }
            else
            {
                st.PendingCount = st.PendingCount * 10 + n;
            }
        }

        private static void Expand(Ctx ctx, int n)
        {
            MeowState st = ctx.St;
            string text = ctx.Port.GetText();
            bool back = BackwardP(ctx);
            int caret = Primary(ctx).Active;
            int target;
            switch (st.SelType)
            {
                case SelType.Char:
                    target = caret + (back ? -n : n);
                    break;
                case SelType.Word:
                case SelType.Symbol:
                    {
                        Func<char, bool> p = Text.CharPred(st.SelType == SelType.Symbol);
                        target =
                            back
                                ? Text.Words.PrevStart(text, caret, n, p)
                                : Text.Words.NextEnd(text, caret, n, p);
                        break;
                    }
                case SelType.Line:
                    {
                        int ln = Text.LineOfOffset(text, caret);
                        target =
                            back
                                ? Text.LineStart(text, Math.Max(ln - n, 0))
                                : Text.LineEnd(text, Math.Min(ln + n, Text.LineCount(text) - 1));
                        break;
                    }
                case SelType.Find:
                case SelType.Till:
                    {
                        char? ch = st.LastFind;
                        if (ch == null) return;
                        int t =
                            Text.NthCharTarget(
                                text, ch.Value, caret, n, back, st.SelType == SelType.Till);
                        if (t < 0) return;
                        target = t;
                        break;
                    }
                default:
                    return;
            }
            Select(ctx, st.SelType, Mark(ctx), target, false);
        }
    }
}
