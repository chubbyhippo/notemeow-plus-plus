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
            new Dictionary<string, MeowCommand>
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
            };

        private enum CaseOp
        {
            Upcase,
            Downcase,
            Capitalize,
        }

        private sealed class Computed
        {
            public Computed(TextEdit edit, SelRange sel)
            {
                Edit = edit;
                Sel = sel;
            }

            public TextEdit Edit { get; }
            public SelRange Sel { get; }
        }

        private delegate Computed Compute(SelRange sel, int lo, int hi);

        private sealed class Item
        {
            public Item(SelRange sel, int index, int lo)
            {
                Sel = sel;
                Index = index;
                Lo = lo;
            }

            public SelRange Sel { get; }
            public int Index { get; }
            public int Lo { get; }
        }

        private static void EditCarets(Ctx ctx, Compute compute)
        {
            List<SelRange> sels = ctx.Port.GetSelections();
            var order = new List<Item>();
            for (int i = 0; i < sels.Count; i++)
            {
                SelRange sel = sels[i];
                order.Add(new Item(sel, i, Math.Min(sel.Anchor, sel.Active)));
            }
            order.Sort((a, b) => b.Lo.CompareTo(a.Lo));
            var edits = new List<TextEdit>();
            var results = new Computed[sels.Count];
            foreach (Item item in order)
            {
                int hi = Math.Max(item.Sel.Anchor, item.Sel.Active);
                Computed r = compute(item.Sel, item.Lo, hi);
                if (r.Edit != null) edits.Add(r.Edit);
                results[item.Index] = r;
            }
            var newSels = new SelRange[sels.Count];
            int delta = 0;
            for (int i = order.Count - 1; i >= 0; i--)
            {
                Item item = order[i];
                Computed r = results[item.Index];
                newSels[item.Index] = new SelRange(r.Sel.Anchor + delta, r.Sel.Active + delta);
                if (r.Edit != null)
                {
                    delta += r.Edit.Text.Length - (r.Edit.End - r.Edit.Start);
                }
            }
            if (edits.Count > 0)
            {
                Grab.AdjustForEdits(ctx.St, edits);
                ctx.Port.Edit(edits);
            }
            ctx.Port.SetSelections(new List<SelRange>(newSels));
        }

        private static void Insert(Ctx ctx)
        {
            var collapsed = new List<SelRange>();
            foreach (SelRange s in ctx.Port.GetSelections())
            {
                int o = Math.Min(s.Anchor, s.Active);
                collapsed.Add(new SelRange(o, o));
            }
            ctx.Port.SetSelections(collapsed);
            ctx.St.SelType = SelType.None;
            Selections.ResetSelectionMemory(ctx.St);
            ctx.SetMode(MeowMode.Insert);
        }

        private static void Append(Ctx ctx)
        {
            var collapsed = new List<SelRange>();
            foreach (SelRange s in ctx.Port.GetSelections())
            {
                int o = Math.Max(s.Anchor, s.Active);
                collapsed.Add(new SelRange(o, o));
            }
            ctx.Port.SetSelections(collapsed);
            ctx.St.SelType = SelType.None;
            Selections.ResetSelectionMemory(ctx.St);
            ctx.SetMode(MeowMode.Insert);
        }

        private static void OpenBelow(Ctx ctx)
        {
            if (BlockedReadOnly(ctx)) return;
            Selections.Collapse(ctx);
            string text = ctx.Port.GetText();
            int eol = Text.LineEnd(text, Text.LineOfOffset(text, Selections.Primary(ctx).Active));
            var nl = new List<TextEdit> { new TextEdit(eol, eol, "\n") };
            Grab.AdjustForEdits(ctx.St, nl);
            ctx.Port.Edit(nl);
            ctx.Port.SetSelections(new List<SelRange> { new SelRange(eol + 1, eol + 1) });
            ctx.SetMode(MeowMode.Insert);
        }

        private static void OpenAbove(Ctx ctx)
        {
            if (BlockedReadOnly(ctx)) return;
            Selections.Collapse(ctx);
            string text = ctx.Port.GetText();
            int bol = Text.LineStart(text, Text.LineOfOffset(text, Selections.Primary(ctx).Active));
            var nl = new List<TextEdit> { new TextEdit(bol, bol, "\n") };
            Grab.AdjustForEdits(ctx.St, nl);
            ctx.Port.Edit(nl);
            ctx.Port.SetSelections(new List<SelRange> { new SelRange(bol, bol) });
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
            ctx.St.SelType = SelType.None;
            ctx.SetMode(MeowMode.Insert);
        }

        private static void Del(Ctx ctx)
        {
            if (BlockedReadOnly(ctx)) return;
            EditCarets(ctx, DeleteForward(ctx.Port.GetText()));
            ctx.St.SelType = SelType.None;
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
            ctx.St.SelType = SelType.None;
        }

        private static int[] KillRange(Ctx ctx, SelRange sel, int textLen)
        {
            int lo = Math.Min(sel.Anchor, sel.Active);
            int hi = Math.Max(sel.Anchor, sel.Active);
            if (ctx.St.SelType == SelType.Line && sel.Active >= sel.Anchor && hi < textLen)
            {
                hi++;
            }
            return new[] { lo, hi };
        }

        private static List<SelRange> RegionsInOrder(List<SelRange> sels)
        {
            var regions = new List<SelRange>();
            foreach (SelRange s in sels)
            {
                if (s.Anchor != s.Active) regions.Add(s);
            }
            regions.Sort(
                (a, b) => Math.Min(a.Anchor, a.Active).CompareTo(Math.Min(b.Anchor, b.Active)));
            return regions;
        }

        private static string JoinedKillText(Ctx ctx, string text, List<SelRange> regions)
        {
            var joined = new StringBuilder();
            for (int i = 0; i < regions.Count; i++)
            {
                int[] r = KillRange(ctx, regions[i], text.Length);
                if (i > 0) joined.Append('\n');
                joined.Append(text, r[0], r[1] - r[0]);
            }
            return joined.ToString();
        }

        private static void Kill(Ctx ctx)
        {
            if (!AllowModify(ctx)) return;
            MeowState st = ctx.St;
            string text = ctx.Port.GetText();
            SelRange prim = Selections.Primary(ctx);
            if (st.SelType == SelType.Join && Selections.HasSelection(prim))
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
                        int[] r = KillRange(ctx, sel, text.Length);
                        return new Computed(
                            new TextEdit(r[0], r[1], ""), new SelRange(r[0], r[0]));
                    });
                st.SelType = SelType.None;
                return;
            }
            if (text.Length == 0) return;
            int caret = prim.Active;
            int eol = Text.LineEnd(text, Text.LineOfOffset(text, caret));
            int end = caret == eol ? Math.Min(eol + 1, text.Length) : eol;
            if (end > caret)
            {
                ctx.Clipboard.Write(text.Substring(caret, end - caret));
                ctx.Port.Edit(new List<TextEdit> { new TextEdit(caret, end, "") });
                ctx.Port.SetSelections(new List<SelRange> { new SelRange(caret, caret) });
            }
        }

        private static void JoinKill(Ctx ctx)
        {
            string text = ctx.Port.GetText();
            SelRange prim = Selections.Primary(ctx);
            int s = Math.Min(prim.Anchor, prim.Active);
            int e = Math.Max(prim.Anchor, prim.Active);
            char before = s > 0 ? text[s - 1] : '\n';
            char after = e < text.Length ? text[e] : '\n';
            bool space =
                before != '\n'
                    && after != '\n'
                    && !char.IsWhiteSpace(before)
                    && !char.IsWhiteSpace(after)
                    && ")]}.,;:".IndexOf(after) < 0
                    && "([{".IndexOf(before) < 0;
            ctx.Port.Edit(new List<TextEdit> { new TextEdit(s, e, space ? " " : "") });
            ctx.Port.SetSelections(new List<SelRange> { new SelRange(s, s) });
            ctx.St.SelType = SelType.None;
            ctx.St.SelExpand = false;
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
                int[] r = KillRange(ctx, s, text.Length);
                int caret = s.Active >= s.Anchor ? r[1] : r[0];
                collapsed.Add(new SelRange(caret, caret));
            }
            ctx.Port.SetSelections(collapsed);
            ctx.St.SelType = SelType.None;
            ctx.St.SelExpand = false;
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
            ctx.St.SelType = SelType.None;
        }

        private static string Casified(string slice, CaseOp op)
        {
            switch (op)
            {
                case CaseOp.Upcase:
                    return slice.ToUpperInvariant();
                case CaseOp.Downcase:
                    return slice.ToLowerInvariant();
                default:
                    return CapitalizedWords(slice);
            }
        }

        private static string CapitalizedWords(string slice)
        {
            Func<char, bool> pred = Text.CharPred(false);
            var outText = new StringBuilder(slice.Length);
            bool inWord = false;
            for (int i = 0; i < slice.Length; i++)
            {
                char c = slice[i];
                if (pred(c))
                {
                    outText.Append(inWord ? char.ToLowerInvariant(c) : char.ToUpperInvariant(c));
                    inWord = true;
                }
                else
                {
                    outText.Append(c);
                    inWord = false;
                }
            }
            return outText.ToString();
        }

        private static void CaseWord(Ctx ctx, CaseOp op)
        {
            if (BlockedReadOnly(ctx)) return;
            int n = ctx.St.TakeCount(1);
            if (n == 0) return;
            bool hadSelection = Selections.HasSelection(Selections.Primary(ctx));
            string text = ctx.Port.GetText();
            Func<char, bool> pred = Text.CharPred(false);
            EditCarets(
                ctx,
                (sel, lo, hi) =>
                {
                    int from = sel.Active;
                    int[] r = WordKillRange(text, from, n, pred);
                    if (r[0] == r[1]) return new Computed(null, sel);
                    int caret = n > 0 ? r[1] : from;
                    return new Computed(
                        new TextEdit(
                            r[0], r[1], Casified(text.Substring(r[0], r[1] - r[0]), op)),
                        new SelRange(caret, caret));
                });
            if (hadSelection) Selections.Collapse(ctx);
        }

        private static int[] WordKillRange(string text, int from, int n, Func<char, bool> pred)
        {
            int target =
                n > 0
                    ? Text.Words.NextEnd(text, from, n, pred)
                    : Text.Words.PrevStart(text, from, -n, pred);
            return new[] { Math.Min(from, target), Math.Max(from, target) };
        }

        private static void KillWord(Ctx ctx)
        {
            if (BlockedReadOnly(ctx)) return;
            int n = ctx.St.TakeCount(1);
            if (n == 0) return;
            string text = ctx.Port.GetText();
            Func<char, bool> pred = Text.CharPred(false);
            var killed = new List<int[]>();
            foreach (SelRange sel in ctx.Port.GetSelections())
            {
                int[] r = WordKillRange(text, sel.Active, n, pred);
                if (r[0] != r[1]) killed.Add(r);
            }
            if (killed.Count == 0) return;
            killed.Sort((a, b) => a[0].CompareTo(b[0]));
            var joined = new StringBuilder();
            for (int i = 0; i < killed.Count; i++)
            {
                if (i > 0) joined.Append('\n');
                int[] r = killed[i];
                joined.Append(text, r[0], r[1] - r[0]);
            }
            ctx.Clipboard.Write(joined.ToString());
            EditCarets(
                ctx,
                (sel, lo, hi) =>
                {
                    int[] r = WordKillRange(text, sel.Active, n, pred);
                    if (r[0] == r[1])
                    {
                        return new Computed(null, new SelRange(sel.Active, sel.Active));
                    }
                    return new Computed(new TextEdit(r[0], r[1], ""), new SelRange(r[0], r[0]));
                });
            ctx.St.SelType = SelType.None;
            ctx.St.SelExpand = false;
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
