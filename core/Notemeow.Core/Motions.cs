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
    public static class Motions
    {
        internal static readonly Dictionary<string, MeowCommand> Commands = BuildCommands();

        private static Dictionary<string, MeowCommand> BuildCommands()
        {
            var commands = new Dictionary<string, MeowCommand>
            {
                ["meow-left"] = ctx => MoveChar(ctx, -ctx.St.TakeCount(1)),
                ["meow-right"] = ctx => MoveChar(ctx, ctx.St.TakeCount(1)),
                ["meow-next"] = ctx => MoveLine(ctx, ctx.St.TakeCount(1)),
                ["meow-prev"] = ctx => MoveLine(ctx, -ctx.St.TakeCount(1)),
                ["meow-left-expand"] = ctx => MoveExpand(ctx, -ctx.St.TakeCount(1), 0),
                ["meow-right-expand"] = ctx => MoveExpand(ctx, ctx.St.TakeCount(1), 0),
                ["meow-next-expand"] = ctx => MoveExpand(ctx, 0, ctx.St.TakeCount(1)),
                ["meow-prev-expand"] = ctx => MoveExpand(ctx, 0, -ctx.St.TakeCount(1)),
                ["meow-next-word"] = ctx => WordMotion(ctx, false, ctx.St.TakeCount(1)),
                ["meow-next-symbol"] = ctx => WordMotion(ctx, true, ctx.St.TakeCount(1)),
                ["meow-back-word"] = ctx => WordMotion(ctx, false, -ctx.St.TakeCount(1)),
                ["meow-back-symbol"] = ctx => WordMotion(ctx, true, -ctx.St.TakeCount(1)),
                ["meow-mark-word"] = ctx => MarkWord(ctx, false),
                ["meow-mark-symbol"] = ctx => MarkWord(ctx, true),
                ["meow-line"] = Line,
                ["meow-goto-line"] = GotoLine,
                ["meow-find"] = ctx => ctx.St.Pending = Pending.Find,
                ["meow-till"] = ctx => ctx.St.Pending = Pending.Till,
                ["forward-char"] = ctx => CharOrExpand(ctx, ctx.St.TakeCount(1)),
                ["backward-char"] = ctx => CharOrExpand(ctx, -ctx.St.TakeCount(1)),
                ["next-line"] = ctx =>
                {
                    LineOrExpand(ctx, ctx.St.TakeCount(1));
                    ctx.St.LastCommand = "next-line";
                },
                ["previous-line"] = ctx =>
                {
                    LineOrExpand(ctx, -ctx.St.TakeCount(1));
                    ctx.St.LastCommand = "previous-line";
                },
                ["move-beginning-of-line"] = ctx => MoveToOrExpand(ctx, SelType.Char, LineStartTarget),
                ["move-end-of-line"] = ctx => MoveToOrExpand(ctx, SelType.Char, LineEndTarget),
                ["forward-word"] = ctx => WordOrExpand(ctx, ctx.St.TakeCount(1)),
                ["backward-word"] = ctx => WordOrExpand(ctx, -ctx.St.TakeCount(1)),
                ["forward-sentence"] = ctx => SentenceOrExpand(ctx, ctx.St.TakeCount(1)),
                ["backward-sentence"] = ctx => SentenceOrExpand(ctx, -ctx.St.TakeCount(1)),
                ["beginning-of-buffer"] = ctx => BufferBoundary(ctx, true),
                ["end-of-buffer"] = ctx => BufferBoundary(ctx, false),
                ["forward-paragraph"] = ctx => ParagraphOrExpand(ctx, ctx.St.TakeCount(1)),
                ["backward-paragraph"] = ctx => ParagraphOrExpand(ctx, -ctx.St.TakeCount(1)),
            };
            return commands;
        }

        private static int LineStartTarget(string text, int off)
        {
            return Text.LineStart(text, Text.LineOfOffset(text, off));
        }

        private static int LineEndTarget(string text, int off)
        {
            return Text.LineEnd(text, Text.LineOfOffset(text, off));
        }

        private static void CharOrExpand(Ctx ctx, int dx)
        {
            if (Selections.HasSelection(Selections.Primary(ctx))) MoveExpand(ctx, dx, 0);
            else MoveChar(ctx, dx);
        }

        private static void LineOrExpand(Ctx ctx, int dy)
        {
            if (Selections.HasSelection(Selections.Primary(ctx))) MoveExpand(ctx, 0, dy);
            else MoveLine(ctx, dy);
        }

        private static void MoveToOrExpand(Ctx ctx, SelType type, Func<string, int, int> target)
        {
            string text = ctx.Port.GetText();
            bool extend = Selections.HasSelection(Selections.Primary(ctx));
            int before = Selections.Primary(ctx).Active;
            var moved = new List<SelRange>();
            foreach (SelRange s in ctx.Port.GetSelections())
            {
                int active = Text.Clamp(target(text, s.Active), 0, text.Length);
                moved.Add(new SelRange(extend ? s.Anchor : active, active));
            }
            ctx.Port.SetSelections(moved);
            if (extend)
            {
                Selections.RecordSelect(
                    ctx, type, moved[0].Anchor, moved[0].Active, true, before);
                ctx.St.SelType = type;
                ctx.St.SelExpand = true;
            }
        }

        private static void WordOrExpand(Ctx ctx, int n)
        {
            Func<char, bool> pred = Text.CharPred(false);
            MoveToOrExpand(
                ctx,
                SelType.Word,
                (text, off) =>
                    n >= 0
                        ? Text.Words.NextEnd(text, off, n, pred)
                        : Text.Words.PrevStart(text, off, -n, pred));
        }

        private static void SentenceOrExpand(Ctx ctx, int n)
        {
            MoveToOrExpand(
                ctx,
                SelType.Char,
                (text, off) =>
                    n >= 0
                        ? Text.NextSentenceEnd(text, off, n)
                        : Text.PrevSentenceStart(text, off, -n));
        }

        private static void ParagraphOrExpand(Ctx ctx, int n)
        {
            MoveToOrExpand(
                ctx,
                SelType.Char,
                (text, off) =>
                    n >= 0
                        ? Text.NextParagraphEnd(text, off, n)
                        : Text.PrevParagraphStart(text, off, -n));
        }

        private static void BufferBoundary(Ctx ctx, bool top)
        {
            bool counted = ctx.St.PendingCount != 0 || ctx.St.Negative;
            int n = ctx.St.TakeCount(1);
            MoveToOrExpand(
                ctx,
                SelType.Char,
                (text, off) =>
                {
                    int len = text.Length;
                    if (!counted) return top ? 0 : len;
                    int tenth = len * n / 10;
                    int raw = Text.Clamp(top ? tenth : len - tenth, 0, len);
                    return NextLineStart(text, raw);
                });
        }

        private static int NextLineStart(string text, int offset)
        {
            if (text.Length == 0) return 0;
            int ln = Text.LineOfOffset(text, Text.Clamp(offset, 0, text.Length));
            return ln >= Text.LineCount(text) - 1 ? text.Length : Text.LineStart(text, ln + 1);
        }

        private static SelType WordType(bool symbol)
        {
            return symbol ? SelType.Symbol : SelType.Word;
        }

        private static readonly HashSet<string> Vertical = new HashSet<string>
        {
            "meow-next", "meow-prev", "meow-next-expand", "meow-prev-expand",
            "next-line", "previous-line",
        };

        private static bool CharSelActive(Ctx ctx)
        {
            return ctx.St.SelType == SelType.Char
                && Selections.HasSelection(Selections.Primary(ctx));
        }

        private static SelRange MovedChar(int len, SelRange sel, int dx, bool extend)
        {
            int active = Text.Clamp(sel.Active + dx, 0, len);
            return new SelRange(extend ? sel.Anchor : active, active);
        }

        private static SelRange MovedLine(
            string text, SelRange sel, int dy, bool extend, int? goal)
        {
            int ln = Text.LineOfOffset(text, sel.Active);
            int target = ln + dy;
            int active;
            if (target < 0)
            {
                active = 0;
            }
            else if (target > Text.LineCount(text) - 1)
            {
                active = text.Length;
            }
            else
            {
                int col = goal.HasValue ? goal.Value : sel.Active - Text.LineStart(text, ln);
                int bol = Text.LineStart(text, target);
                active = bol + Math.Min(col, Text.LineEnd(text, target) - bol);
            }
            return new SelRange(extend ? sel.Anchor : active, active);
        }

        private static int GoalColumn(Ctx ctx)
        {
            MeowState st = ctx.St;
            if (st.GoalColumn == null
                || st.LastCommand == null
                || !Vertical.Contains(st.LastCommand))
            {
                string text = ctx.Port.GetText();
                int p = Selections.Primary(ctx).Active;
                st.GoalColumn = p - Text.LineStart(text, Text.LineOfOffset(text, p));
            }
            return st.GoalColumn.Value;
        }

        private static void MoveChar(Ctx ctx, int dx)
        {
            bool extend = CharSelActive(ctx);
            if (!extend && Selections.HasSelection(Selections.Primary(ctx)))
                Selections.Cancel(ctx);
            int len = ctx.Port.GetText().Length;
            var moved = new List<SelRange>();
            foreach (SelRange s in ctx.Port.GetSelections()) moved.Add(MovedChar(len, s, dx, extend));
            ctx.Port.SetSelections(moved);
        }

        private static void MoveLine(Ctx ctx, int dy)
        {
            bool extend = CharSelActive(ctx);
            if (!extend) Selections.Cancel(ctx);
            int goal = GoalColumn(ctx);
            string text = ctx.Port.GetText();
            List<SelRange> sels = ctx.Port.GetSelections();
            var moved = new List<SelRange>();
            for (int i = 0; i < sels.Count; i++)
            {
                moved.Add(MovedLine(text, sels[i], dy, extend, i == 0 ? goal : (int?)null));
            }
            ctx.Port.SetSelections(moved);
        }

        private static void MoveExpand(Ctx ctx, int dx, int dy)
        {
            string text = ctx.Port.GetText();
            int? goal = dy != 0 ? GoalColumn(ctx) : (int?)null;
            List<SelRange> sels = ctx.Port.GetSelections();
            int before = sels[0].Active;
            var moved = new List<SelRange>();
            for (int i = 0; i < sels.Count; i++)
            {
                moved.Add(
                    dy == 0
                        ? MovedChar(text.Length, sels[i], dx, true)
                        : MovedLine(text, sels[i], dy, true, i == 0 ? goal : null));
            }
            ctx.Port.SetSelections(moved);
            Selections.RecordSelect(
                ctx, SelType.Char, moved[0].Anchor, moved[0].Active, true, before);
            ctx.St.SelType = SelType.Char;
            ctx.St.SelExpand = true;
        }

        private static void WordMotion(Ctx ctx, bool symbol, int n)
        {
            if (n == 0) return;
            string text = ctx.Port.GetText();
            SelType type = WordType(symbol);
            SelRange sel = Selections.Primary(ctx);
            int lo = sel.Lo();
            int hi = sel.Hi();
            if (!(Selections.HasSelection(sel) && ctx.St.SelType == type)) Selections.Cancel(ctx);
            bool extend =
                ctx.St.SelExpand && ctx.St.SelType == type && Selections.HasSelection(sel);
            int from = extend ? (n < 0 ? lo : hi) : sel.Active;
            int target =
                n > 0
                    ? Text.Words.NextEnd(text, from, n, Text.CharPred(symbol))
                    : Text.Words.PrevStart(text, from, -n, Text.CharPred(symbol));
            if (target == from) return;
            int anchor =
                extend
                    ? (n < 0 ? hi : lo)
                    : Text.Words.FixSelectionMark(text, target, from, Text.CharPred(symbol));
            Selections.Select(ctx, type, anchor, target, extend);
        }

        private static void MarkWord(Ctx ctx, bool symbol)
        {
            bool neg = ctx.St.TakeCount(1) < 0;
            string text = ctx.Port.GetText();
            int[] b =
                Text.Words.BoundsAt(text, Selections.Primary(ctx).Active, Text.CharPred(symbol));
            if (b == null)
            {
                ctx.Ui.Hint("No word here");
                return;
            }
            int s = b[0];
            int e = b[1];
            if (neg) Selections.Select(ctx, WordType(symbol), e, s, true);
            else Selections.Select(ctx, WordType(symbol), s, e, true);
            string quoted = Text.EscapeRegExp(text.Substring(s, e - s));
            string pattern =
                symbol ? "(?<![\\w$])" + quoted + "(?![\\w$])" : "\\b" + quoted + "\\b";
            Search.Push(ctx.St, pattern);
        }

        private static void Line(Ctx ctx)
        {
            string text = ctx.Port.GetText();
            if (text.Length == 0) return;
            int n = ctx.St.TakeCount(1);
            int lastLine = Text.LineCount(text) - 1;
            if (ctx.St.SelType == SelType.Line
                && ctx.St.SelExpand
                && Selections.HasSelection(Selections.Primary(ctx)))
            {
                int caretLn = Text.LineOfOffset(text, Selections.Primary(ctx).Active);
                if (Selections.BackwardP(ctx))
                {
                    int lnUp = Math.Max(caretLn - Math.Abs(n), 0);
                    Selections.Select(
                        ctx, SelType.Line, Selections.Mark(ctx), Text.LineStart(text, lnUp), true);
                }
                else
                {
                    int lnDown = Math.Min(caretLn + Math.Abs(n), lastLine);
                    Selections.Select(
                        ctx, SelType.Line, Selections.Mark(ctx), Text.LineEnd(text, lnDown), true);
                }
                return;
            }
            int ln = Text.LineOfOffset(text, Selections.Primary(ctx).Active);
            if (n < 0)
            {
                int startLn = Math.Max(ln + n + 1, 0);
                Selections.Select(
                    ctx,
                    SelType.Line,
                    Text.LineEnd(text, ln),
                    Text.LineStart(text, startLn),
                    true);
            }
            else
            {
                int endLn = Math.Min(ln + n - 1, lastLine);
                Selections.Select(
                    ctx,
                    SelType.Line,
                    Text.LineStart(text, ln),
                    Text.LineEnd(text, endLn),
                    true);
            }
        }

        private static void GotoLine(Ctx ctx)
        {
            string input = ctx.Ui.Input("Goto line:", null);
            if (input == null) return;
            string text = ctx.Port.GetText();
            if (text.Length == 0) return;
            int parsed;
            if (!int.TryParse(input.Trim(), out parsed)) return;
            int ln = Text.Clamp(parsed - 1, 0, Text.LineCount(text) - 1);
            Selections.Select(
                ctx, SelType.Line, Text.LineStart(text, ln), Text.LineEnd(text, ln), true);
        }

        public static void FindTill(Ctx ctx, char ch, bool till)
        {
            int n = ctx.St.TakeCount(1);
            string text = ctx.Port.GetText();
            int caret = Selections.Primary(ctx).Active;
            int target = Text.NthCharTarget(text, ch, caret, Math.Abs(n), n < 0, till);
            if (target < 0)
            {
                ctx.Ui.Hint("char not found: " + ch);
                return;
            }
            ctx.St.LastFind = ch;
            Selections.Select(ctx, till ? SelType.Till : SelType.Find, caret, target, false);
        }
    }
}
