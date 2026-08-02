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
using System.Text;

namespace Notemeow.Core
{
    public static class Edits
    {
        public static bool AllowModify(Ctx ctx)
        {
            return ctx.Port.IsWritable();
        }

        public static bool BlockedReadOnly(Ctx ctx)
        {
            if (AllowModify(ctx)) return false;
            ctx.Ui.Hint("Buffer is read-only");
            return true;
        }

        internal static readonly Dictionary<string, MeowCommand> Commands =
            new()
            {
                ["meow-insert"] = Insert,
                ["meow-append"] = Append,
                ["meow-open-above"] = OpenAbove,
                ["meow-open-below"] = OpenBelow,
                ["meow-change"] = Change,
                ["meow-delete"] = Del,
                ["meow-backward-delete"] = BackwardDelete,
                ["meow-kill"] = Kill,
                ["meow-save"] = Save,
                ["meow-yank"] = Yank,
                ["meow-replace"] = Replace,
                ["meow-undo"] = Undo,
                ["meow-undo-in-selection"] = UndoInSelection,
                ["upcase-word"] = ctx => CaseWord(ctx, CaseOp.Upcase),
                ["downcase-word"] = ctx => CaseWord(ctx, CaseOp.Downcase),
                ["capitalize-word"] = ctx => CaseWord(ctx, CaseOp.Capitalize),
                ["kill-word"] = KillWord,
                ["open-line"] = OpenLine,
                ["delete-horizontal-space"] = ctx => HorizontalSpace(ctx, ""),
                ["just-one-space"] = ctx => HorizontalSpace(ctx, " "),
            };

        private enum CaseOp
        {
            Upcase,
            Downcase,
            Capitalize,
        }

        private sealed class Computed(TextEdit edit, SelRange sel)
        {
            public TextEdit Edit { get; } = edit;
            public SelRange Sel { get; } = sel;
        }

        private delegate Computed Compute(SelRange sel, int selStart, int selEnd);

        private sealed class Item(SelRange sel, int index, int selStart)
        {
            public SelRange Sel { get; } = sel;
            public int Index { get; } = index;
            public int SelStart { get; } = selStart;
        }

        private static void EditCarets(Ctx ctx, Compute compute)
        {
            List<SelRange> sels = ctx.Port.GetSelections();
            var order = new List<Item>();
            for (int i = 0; i < sels.Count; i++)
            {
                SelRange sel = sels[i];
                order.Add(new Item(sel, i, sel.SelStart()));
            }
            order.Sort((left, right) => right.SelStart.CompareTo(left.SelStart));
            var edits = new List<TextEdit>();
            var results = new Computed[sels.Count];
            foreach (Item item in order)
            {
                int selEnd = item.Sel.SelEnd();
                Computed computed = compute(item.Sel, item.SelStart, selEnd);
                if (computed.Edit != null) edits.Add(computed.Edit);
                results[item.Index] = computed;
            }
            var newSels = new SelRange[sels.Count];
            int delta = 0;
            for (int i = order.Count - 1; i >= 0; i--)
            {
                Item item = order[i];
                Computed computed = results[item.Index];
                newSels[item.Index] =
                    new SelRange(computed.Sel.Anchor + delta, computed.Sel.Active + delta);
                TextEdit edit = computed.Edit;
                if (edit != null)
                {
                    delta += edit.Text.Length - (edit.End - edit.Start);
                }
            }
            if (edits.Count > 0)
            {
                Grab.AdjustForEdits(ctx.State, edits);
                ctx.Port.Edit(edits);
            }
            ctx.Port.SetSelections([.. newSels]);
        }

        private static void Insert(Ctx ctx)
        {
            var collapsed = new List<SelRange>();
            foreach (SelRange sel in ctx.Port.GetSelections())
            {
                int start = sel.SelStart();
                collapsed.Add(new SelRange(start, start));
            }
            ctx.Port.SetSelections(collapsed);
            ctx.State.SelType = SelType.None;
            Selections.ResetSelectionMemory(ctx.State);
            ctx.SetMode(MeowMode.Insert);
        }

        private static void Append(Ctx ctx)
        {
            var collapsed = new List<SelRange>();
            foreach (SelRange sel in ctx.Port.GetSelections())
            {
                int end = sel.SelEnd();
                collapsed.Add(new SelRange(end, end));
            }
            ctx.Port.SetSelections(collapsed);
            ctx.State.SelType = SelType.None;
            Selections.ResetSelectionMemory(ctx.State);
            ctx.SetMode(MeowMode.Insert);
        }

        private static void OpenBelow(Ctx ctx)
        {
            if (BlockedReadOnly(ctx)) return;
            Selections.Collapse(ctx);
            string text = ctx.Port.GetText();
            int eol = Text.LineEnd(text, Text.LineOfOffset(text, Selections.Primary(ctx).Active));
            var nl = new List<TextEdit> { new(eol, eol, "\n") };
            Grab.AdjustForEdits(ctx.State, nl);
            ctx.Port.Edit(nl);
            ctx.Port.SetSelections([new SelRange(eol + 1, eol + 1)]);
            ctx.SetMode(MeowMode.Insert);
        }

        private static void OpenLine(Ctx ctx)
        {
            if (BlockedReadOnly(ctx)) return;
            Selections.Collapse(ctx);
            int at = Selections.Primary(ctx).Active;
            var nl = new List<TextEdit> { new(at, at, "\n") };
            Grab.AdjustForEdits(ctx.State, nl);
            ctx.Port.Edit(nl);
            ctx.Port.SetSelections([new SelRange(at, at)]);
        }

        private static void HorizontalSpace(Ctx ctx, string replacement)
        {
            if (BlockedReadOnly(ctx)) return;
            Selections.Collapse(ctx);
            string text = ctx.Port.GetText();
            int at = Selections.Primary(ctx).Active;
            int from = at;
            while (from > 0 && Text.IsBlank(text[from - 1])) from--;
            int to = at;
            while (to < text.Length && Text.IsBlank(text[to])) to++;
            if (from == to && replacement.Length == 0) return;
            var edits = new List<TextEdit> { new(from, to, replacement) };
            Grab.AdjustForEdits(ctx.State, edits);
            ctx.Port.Edit(edits);
            int caret = from + replacement.Length;
            ctx.Port.SetSelections([new SelRange(caret, caret)]);
        }

        private static void OpenAbove(Ctx ctx)
        {
            if (BlockedReadOnly(ctx)) return;
            Selections.Collapse(ctx);
            string text = ctx.Port.GetText();
            int bol = Text.LineStart(text, Text.LineOfOffset(text, Selections.Primary(ctx).Active));
            var nl = new List<TextEdit> { new(bol, bol, "\n") };
            Grab.AdjustForEdits(ctx.State, nl);
            ctx.Port.Edit(nl);
            ctx.Port.SetSelections([new SelRange(bol, bol)]);
            ctx.SetMode(MeowMode.Insert);
        }

        private static Compute DeleteForward(string text)
        {
            return (sel, lo, hi) =>
            {
                if (lo != hi)
                {
                    return new Computed(new TextEdit(lo, hi, ""), new SelRange(lo, lo));
                }
                if (lo < text.Length)
                {
                    return new Computed(new TextEdit(lo, lo + 1, ""), new SelRange(lo, lo));
                }
                return new Computed(null, new SelRange(lo, lo));
            };
        }

        private static void Change(Ctx ctx)
        {
            if (!AllowModify(ctx)) return;
            string text = ctx.Port.GetText();
            SelRange prim = Selections.Primary(ctx);
            if (!Selections.HasSelection(prim) && prim.Active >= text.Length) return;
            EditCarets(ctx, DeleteForward(text));
            ctx.State.SelType = SelType.None;
            ctx.SetMode(MeowMode.Insert);
        }

        private static void Del(Ctx ctx)
        {
            if (BlockedReadOnly(ctx)) return;
            EditCarets(ctx, DeleteForward(ctx.Port.GetText()));
            ctx.State.SelType = SelType.None;
        }

        private static void BackwardDelete(Ctx ctx)
        {
            if (!AllowModify(ctx)) return;
            EditCarets(
                ctx,
                (sel, lo, hi) =>
                {
                    if (lo != hi)
                    {
                        return new Computed(new TextEdit(lo, hi, ""), new SelRange(lo, lo));
                    }
                    if (lo > 0)
                    {
                        return new Computed(
                            new TextEdit(lo - 1, lo, ""), new SelRange(lo - 1, lo - 1));
                    }
                    return new Computed(null, new SelRange(lo, lo));
                });
            ctx.State.SelType = SelType.None;
        }

        private static int[] KillRange(Ctx ctx, SelRange sel, string text)
        {
            int start = sel.SelStart();
            int end = sel.SelEnd();
            if (ctx.State.SelType == SelType.Line && sel.Active >= sel.Anchor && end < text.Length)
            {
                if (text[end] == '\r') end++;
                if (end < text.Length && text[end] == '\n') end++;
            }
            return [start, end];
        }

        private static List<SelRange> RegionsInOrder(List<SelRange> sels)
        {
            var regions = new List<SelRange>();
            foreach (SelRange s in sels)
            {
                if (s.Anchor != s.Active) regions.Add(s);
            }
            regions.Sort(
                (a, b) => a.SelStart().CompareTo(b.SelStart()));
            return regions;
        }

        private static string JoinedKillText(Ctx ctx, string text, List<SelRange> regions)
        {
            var joined = new StringBuilder();
            for (int i = 0; i < regions.Count; i++)
            {
                int[] killed = KillRange(ctx, regions[i], text);
                if (i > 0) joined.Append('\n');
                joined.Append(text, killed[0], killed[1] - killed[0]);
            }
            return joined.ToString();
        }

        private static void Kill(Ctx ctx)
        {
            if (!AllowModify(ctx)) return;
            MeowState state = ctx.State;
            string text = ctx.Port.GetText();
            SelRange prim = Selections.Primary(ctx);
            if (state.SelType == SelType.Join && Selections.HasSelection(prim))
            {
                JoinKill(ctx);
                return;
            }
            if (Selections.HasSelection(prim))
            {
                ctx.Clipboard.Write(
                    JoinedKillText(ctx, text, RegionsInOrder(ctx.Port.GetSelections())));
                EditCarets(
                    ctx,
                    (sel, lo, hi) =>
                    {
                        if (lo == hi) return new Computed(null, sel);
                        int[] r = KillRange(ctx, sel, text);
                        return new Computed(
                            new TextEdit(r[0], r[1], ""), new SelRange(r[0], r[0]));
                    });
                state.SelType = SelType.None;
                return;
            }
            if (text.Length == 0) return;
            int caret = prim.Active;
            int line = Text.LineOfOffset(text, caret);
            int eol = Text.LineEnd(text, line);
            int end = caret == eol ? Text.LineStart(text, line + 1) : eol;
            if (end > caret)
            {
                ctx.Clipboard.Write(text.Substring(caret, end - caret));
                ctx.Port.Edit([new TextEdit(caret, end, "")]);
                ctx.Port.SetSelections([new SelRange(caret, caret)]);
            }
        }

        private static void JoinKill(Ctx ctx)
        {
            string text = ctx.Port.GetText();
            SelRange prim = Selections.Primary(ctx);
            int start = prim.SelStart();
            int end = prim.SelEnd();
            char before = start > 0 ? text[start - 1] : '\n';
            char after = end < text.Length ? text[end] : '\n';
            bool space =
                before != '\n'
                    && after != '\n'
                    && !char.IsWhiteSpace(before)
                    && !char.IsWhiteSpace(after)
                    && ")]}.,;:".IndexOf(after) < 0
                    && "([{".IndexOf(before) < 0;
            ctx.Port.Edit([new TextEdit(start, end, space ? " " : "")]);
            ctx.Port.SetSelections([new SelRange(start, start)]);
            ctx.State.SelType = SelType.None;
            ctx.State.SelExpand = false;
        }

        private static void Save(Ctx ctx)
        {
            string text = ctx.Port.GetText();
            List<SelRange> sels = ctx.Port.GetSelections();
            List<SelRange> withSel = RegionsInOrder(sels);
            if (withSel.Count == 0) return;
            ctx.Clipboard.Write(JoinedKillText(ctx, text, withSel));
            var collapsed = new List<SelRange>();
            foreach (SelRange s in sels)
            {
                if (s.Anchor == s.Active)
                {
                    collapsed.Add(s);
                    continue;
                }
                int[] r = KillRange(ctx, s, text);
                int caret = s.Active >= s.Anchor ? r[1] : r[0];
                collapsed.Add(new SelRange(caret, caret));
            }
            ctx.Port.SetSelections(collapsed);
            ctx.State.SelType = SelType.None;
            ctx.State.SelExpand = false;
        }

        private static void Yank(Ctx ctx)
        {
            if (BlockedReadOnly(ctx)) return;
            string clip = ctx.Clipboard.Read();
            if (clip == null || clip.Length == 0) return;
            EditCarets(
                ctx,
                (sel, lo, hi) =>
                    new Computed(
                        new TextEdit(sel.Active, sel.Active, clip),
                        new SelRange(sel.Active + clip.Length, sel.Active + clip.Length)));
        }

        private static void Replace(Ctx ctx)
        {
            if (!AllowModify(ctx)) return;
            if (!Selections.HasSelection(Selections.Primary(ctx))) return;
            string raw = ctx.Clipboard.Read();
            if (raw == null) return;
            string clip = System.Text.RegularExpressions.Regex.Replace(raw, "\\n+$", "");
            EditCarets(
                ctx,
                (sel, lo, hi) =>
                    lo == hi
                        ? new Computed(null, sel)
                        : new Computed(
                            new TextEdit(lo, hi, clip),
                            new SelRange(lo + clip.Length, lo + clip.Length)));
            ctx.State.SelType = SelType.None;
        }

        private static string Casified(string slice, CaseOp op)
        {
            return op switch
            {
                CaseOp.Upcase => slice.ToUpperInvariant(),
                CaseOp.Downcase => slice.ToLowerInvariant(),
                _ => CapitalizedWords(slice),
            };
        }

        private static string CapitalizedWords(string slice)
        {
            Func<char, bool> isWord = Text.CharPred(false);
            var outText = new StringBuilder(slice.Length);
            bool inWord = false;
            for (int i = 0; i < slice.Length; i++)
            {
                char ch = slice[i];
                if (isWord(ch))
                {
                    outText.Append(inWord ? char.ToLowerInvariant(ch) : char.ToUpperInvariant(ch));
                    inWord = true;
                }
                else
                {
                    outText.Append(ch);
                    inWord = false;
                }
            }
            return outText.ToString();
        }

        private static void CaseWord(Ctx ctx, CaseOp op)
        {
            if (BlockedReadOnly(ctx)) return;
            int count = ctx.State.TakeCount(1);
            if (count == 0) return;
            bool hadSelection = Selections.HasSelection(Selections.Primary(ctx));
            string text = ctx.Port.GetText();
            Func<char, bool> isWord = Text.CharPred(false);
            EditCarets(
                ctx,
                (sel, selStart, selEnd) =>
                {
                    int from = sel.Active;
                    int[] range = WordKillRange(text, from, count, isWord);
                    if (range[0] == range[1]) return new Computed(null, sel);
                    int caret = count > 0 ? range[1] : from;
                    return new Computed(
                        new TextEdit(
                            range[0],
                            range[1],
                            Casified(text.Substring(range[0], range[1] - range[0]), op)),
                        new SelRange(caret, caret));
                });
            if (hadSelection) Selections.Collapse(ctx);
        }

        private static int[] WordKillRange(string text, int from, int count, Func<char, bool> isWord)
        {
            int target =
                count > 0
                    ? Text.Words.NextEnd(text, from, count, isWord)
                    : Text.Words.PrevStart(text, from, -count, isWord);
            return [Math.Min(from, target), Math.Max(from, target)];
        }

        private static void KillWord(Ctx ctx)
        {
            if (BlockedReadOnly(ctx)) return;
            int count = ctx.State.TakeCount(1);
            if (count == 0) return;
            string text = ctx.Port.GetText();
            Func<char, bool> isWord = Text.CharPred(false);
            var killed = new List<int[]>();
            foreach (SelRange sel in ctx.Port.GetSelections())
            {
                int[] range = WordKillRange(text, sel.Active, count, isWord);
                if (range[0] != range[1]) killed.Add(range);
            }
            if (killed.Count == 0) return;
            killed.Sort((left, right) => left[0].CompareTo(right[0]));
            var joined = new StringBuilder();
            for (int i = 0; i < killed.Count; i++)
            {
                if (i > 0) joined.Append('\n');
                int[] range = killed[i];
                joined.Append(text, range[0], range[1] - range[0]);
            }
            ctx.Clipboard.Write(joined.ToString());
            EditCarets(
                ctx,
                (sel, selStart, selEnd) =>
                {
                    int[] range = WordKillRange(text, sel.Active, count, isWord);
                    if (range[0] == range[1])
                    {
                        return new Computed(null, new SelRange(sel.Active, sel.Active));
                    }
                    return new Computed(
                        new TextEdit(range[0], range[1], ""), new SelRange(range[0], range[0]));
                });
            ctx.State.SelType = SelType.None;
            ctx.State.SelExpand = false;
        }

        private static void Undo(Ctx ctx)
        {
            if (Selections.HasSelection(Selections.Primary(ctx))) Selections.Cancel(ctx);
            ctx.Port.Undo();
        }

        private static void UndoInSelection(Ctx ctx)
        {
            if (Selections.HasSelection(Selections.Primary(ctx))) ctx.Port.Undo();
        }
    }
}
