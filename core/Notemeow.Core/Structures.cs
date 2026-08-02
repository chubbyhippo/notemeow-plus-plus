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

using System.Collections.Generic;

namespace Notemeow.Core
{
    public static class Structures
    {
        internal static readonly Dictionary<string, MeowCommand> Commands =
            new()
            {
                ["meow-inner-of-thing"] = ctx => PendThing(ctx, Pending.Inner),
                ["meow-bounds-of-thing"] = ctx => PendThing(ctx, Pending.Bounds),
                ["meow-beginning-of-thing"] = ctx => PendThing(ctx, Pending.Begin),
                ["meow-end-of-thing"] = ctx => PendThing(ctx, Pending.End),
                ["meow-block"] = Block,
                ["meow-to-block"] = ToBlock,
                ["meow-join"] = Join,
            };

        private static void PendThing(Ctx ctx, Pending p)
        {
            ctx.State.Pending = p;
            ctx.Ui.ScheduleWhichKey("things", "");
        }

        public static void ThingSelect(Ctx ctx, Pending kind, char ch)
        {
            int off = Selections.Primary(ctx).Active;
            OffsetRange b =
                kind == Pending.Bounds ? Things.Bounds(ctx, ch, off) : Things.Inner(ctx, ch, off);
            if (b == null)
            {
                ctx.Ui.Hint("No thing '" + ch + "' here");
                return;
            }
            switch (kind)
            {
                case Pending.Inner:
                    Selections.Select(ctx, SelType.Transient, b.Start, b.End, false);
                    break;
                case Pending.Bounds:
                    Selections.Select(ctx, SelType.Transient, b.End, b.Start, false);
                    break;
                case Pending.Begin:
                    Selections.Select(ctx, SelType.Transient, off, b.Start, false);
                    break;
                case Pending.End:
                    Selections.Select(ctx, SelType.Transient, off, b.End, false);
                    break;
                default:
                    break;
            }
        }

        private static int[] EnclosingPair(string text, int selStart, int selEnd)
        {
            const string opens = "([{";
            const string closes = ")]}";
            var openOffsets = new Stack<int>();
            int[] best = null;
            int i = 0;
            while (i < text.Length)
            {
                char ch = text[i];
                if (ch == '"' || ch == '\'' || ch == '`')
                {
                    int j = i + 1;
                    while (j < text.Length && text[j] != ch && text[j] != '\n')
                    {
                        if (text[j] == '\\') j++;
                        j++;
                    }
                    if (j < text.Length && text[j] == ch)
                    {
                        i = j + 1;
                        continue;
                    }
                }
                if (opens.IndexOf(ch) >= 0)
                {
                    openOffsets.Push(i);
                }
                else if (closes.IndexOf(ch) >= 0)
                {
                    int kind = closes.IndexOf(ch);
                    while (openOffsets.Count > 0)
                    {
                        int open = openOffsets.Pop();
                        if (opens.IndexOf(text[open]) == kind)
                        {
                            if (open < selStart && i + 1 >= selEnd && (best == null || i - open < best[1] - best[0]))
                            {
                                best = [open, i];
                            }
                            break;
                        }
                    }
                }
                i++;
            }
            return best;
        }

        private static void Block(Ctx ctx)
        {
            string text = ctx.Port.GetText();
            SelRange sel = Selections.Primary(ctx);
            bool active = ctx.State.SelType == SelType.Block && Selections.HasSelection(sel);
            bool back = Selections.BackwardP(ctx) != (ctx.State.TakeCount(1) < 0);
            int selStart = active ? sel.SelStart() : sel.Active;
            int selEnd = active ? sel.SelEnd() : sel.Active;
            int[] p = EnclosingPair(text, selStart, selEnd);
            if (p == null)
            {
                ctx.Ui.Hint("No enclosing block");
                return;
            }
            if (back) Selections.Select(ctx, SelType.Block, p[1] + 1, p[0], true);
            else Selections.Select(ctx, SelType.Block, p[0], p[1] + 1, true);
        }

        private static void ToBlock(Ctx ctx)
        {
            string text = ctx.Port.GetText();
            bool back =
                (ctx.State.SelType == SelType.Block && Selections.BackwardP(ctx))
                    || ctx.State.TakeCount(1) < 0;
            int caret = Selections.Primary(ctx).Active;
            int[] p = EnclosingPair(text, caret, caret);
            if (p == null)
            {
                ctx.Ui.Hint("No enclosing block");
                return;
            }
            Selections.Select(ctx, SelType.Block, caret, back ? p[0] : p[1] + 1, true);
        }

        private static void Join(Ctx ctx)
        {
            string text = ctx.Port.GetText();
            if (text.Length == 0) return;
            int count = ctx.State.TakeCount(1);
            int caretLine = Text.LineOfOffset(text, Selections.Primary(ctx).Active);
            if (count >= 0)
            {
                int prevLine = caretLine - 1;
                while (prevLine >= 0 && Things.Blank(text, prevLine)) prevLine--;
                if (prevLine < 0) return;
                Selections.Select(
                    ctx,
                    SelType.Join,
                    Text.LineEnd(text, prevLine),
                    FirstNonBlankOffset(text, caretLine),
                    true);
            }
            else
            {
                int lastLine = Text.LineCount(text) - 1;
                int nextLine = caretLine + 1;
                while (nextLine <= lastLine && Things.Blank(text, nextLine)) nextLine++;
                if (nextLine > lastLine) return;
                Selections.Select(
                    ctx,
                    SelType.Join,
                    Text.LineEnd(text, caretLine),
                    FirstNonBlankOffset(text, nextLine),
                    true);
            }
        }

        private static int FirstNonBlankOffset(string text, int line)
        {
            int offset = Text.LineStart(text, line);
            int eol = Text.LineEnd(text, line);
            while (offset < eol && char.IsWhiteSpace(text[offset])) offset++;
            return offset;
        }
    }
}
