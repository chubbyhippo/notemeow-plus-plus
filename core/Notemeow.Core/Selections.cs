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
                commands["meow-expand-" + digit] = ctx => ExpandOrCount(ctx, digit);
            }
            commands["meow-reverse"] = Reverse;
            commands["meow-cancel-selection"] = CancelAll;
            commands["meow-pop-selection"] = Pop;
            return commands;
        }

        private const int MaxSelectionHistory = 200;
        private const int DigitZeroExpand = 10;

        private static readonly HashSet<SelType> Expandable =
        [
            SelType.Char,
            SelType.Word,
            SelType.Symbol,
            SelType.Line,
            SelType.Find,
            SelType.Till,
        ];

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
            MeowState state = ctx.State;
            SavedSelection prev =
                state.LastSelection ?? new SavedSelection(null, false, posBefore, posBefore);
            SavedSelection head =
                state.SelectionHistory.Count > 0
                    ? state.SelectionHistory[state.SelectionHistory.Count - 1]
                    : null;
            if (head == null || !head.Equals(prev)) state.SelectionHistory.Add(prev);
            while (state.SelectionHistory.Count > MaxSelectionHistory)
                state.SelectionHistory.RemoveAt(0);
            state.LastSelection = new SavedSelection(type, expand, anchor, active);
        }

        public static void Select(Ctx ctx, SelType type, int markOff, int point, bool expand)
        {
            Select(ctx, type, markOff, point, expand, true);
        }

        public static void Select(
            Ctx ctx, SelType type, int markOff, int point, bool expand, bool push)
        {
            MeowState state = ctx.State;
            int len = ctx.Port.GetText().Length;
            int mark = Text.Clamp(markOff, 0, len);
            int caret = Text.Clamp(point, 0, len);
            List<SelRange> sels = ctx.Port.GetSelections();
            if (push) RecordSelect(ctx, type, mark, caret, expand, sels[0].Active);
            else state.LastSelection = new SavedSelection(type, expand, mark, caret);
            state.SelType = type;
            state.SelExpand = expand;
            var next = new List<SelRange>(sels)
            {
                [0] = new SelRange(mark, caret)
            };
            ctx.Port.SetSelections(next);
            Grab.Beacon(ctx);
            ctx.Ui.ShowExpandHints(Hints.ExpandHintPositions(ctx));
        }

        public static void ResetSelectionMemory(MeowState state)
        {
            state.SelectionHistory.Clear();
            state.LastSelection = null;
        }

        public static void Collapse(Ctx ctx)
        {
            var sels = new List<SelRange>(ctx.Port.GetSelections());
            sels[0] = new SelRange(sels[0].Active, sels[0].Active);
            ctx.Port.SetSelections(sels);
            ctx.State.SelType = SelType.None;
            ctx.State.SelExpand = false;
        }

        public static void Cancel(Ctx ctx)
        {
            Collapse(ctx);
            ResetSelectionMemory(ctx.State);
        }

        public static void CancelAll(Ctx ctx)
        {
            List<SelRange> sels = ctx.Port.GetSelections();
            if (sels.Count > 1) ctx.Port.SetSelections([sels[0]]);
            Cancel(ctx);
        }

        private static void Reverse(Ctx ctx)
        {
            SelRange sel = Primary(ctx);
            if (!HasSelection(sel)) return;
            var sels = new List<SelRange>(ctx.Port.GetSelections())
            {
                [0] = new SelRange(sel.Active, sel.Anchor)
            };
            ctx.Port.SetSelections(sels);
        }

        private static void Pop(Ctx ctx)
        {
            MeowState state = ctx.State;
            if (HasSelection(Primary(ctx)))
            {
                SavedSelection entry = null;
                if (state.SelectionHistory.Count > 0)
                {
                    entry = state.SelectionHistory[state.SelectionHistory.Count - 1];
                    state.SelectionHistory.RemoveAt(state.SelectionHistory.Count - 1);
                }
                if (entry == null) return;
                if (entry.Type == null)
                {
                    var sels = new List<SelRange>(ctx.Port.GetSelections())
                    {
                        [0] = new SelRange(entry.Active, entry.Active)
                    };
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

        private static void ExpandOrCount(Ctx ctx, int digit)
        {
            MeowState state = ctx.State;
            if (HasSelection(Primary(ctx)) && Expandable.Contains(state.SelType))
            {
                Expand(ctx, digit == 0 ? DigitZeroExpand : digit);
            }
            else
            {
                state.PendingCount = state.PendingCount * 10 + digit;
            }
        }

        private static void Expand(Ctx ctx, int count)
        {
            MeowState state = ctx.State;
            string text = ctx.Port.GetText();
            bool back = BackwardP(ctx);
            int caret = Primary(ctx).Active;
            int target;
            switch (state.SelType)
            {
                case SelType.Char:
                    target = caret + (back ? -count : count);
                    break;
                case SelType.Word:
                case SelType.Symbol:
                    {
                        Func<char, bool> isWord =
                            Text.CharPred(state.SelType == SelType.Symbol);
                        target =
                            back
                                ? Text.Words.PrevStart(text, caret, count, isWord)
                                : Text.Words.NextEnd(text, caret, count, isWord);
                        break;
                    }
                case SelType.Line:
                    {
                        int caretLine = Text.LineOfOffset(text, caret);
                        target =
                            back
                                ? Text.LineStart(text, Math.Max(caretLine - count, 0))
                                : Text.LineEnd(
                                    text, Math.Min(caretLine + count, Text.LineCount(text) - 1));
                        break;
                    }
                case SelType.Find:
                case SelType.Till:
                    {
                        char? ch = state.LastFind;
                        if (ch == null) return;
                        int found =
                            Text.NthCharTarget(
                                text, ch.Value, caret, count, back, state.SelType == SelType.Till);
                        if (found < 0) return;
                        target = found;
                        break;
                    }
                default:
                    return;
            }
            Select(ctx, state.SelType, Mark(ctx), target, false);
        }
    }
}
