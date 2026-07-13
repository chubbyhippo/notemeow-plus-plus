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
    public static class Structures
    {
        internal static readonly Dictionary<string, MeowCommand> Commands =
            new Dictionary<string, MeowCommand>
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
            ctx.St.Pending = p;
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

        private static int[] EnclosingPair(string text, int s, int e)
        {
            const string opens = "([{";
            const string closes = ")]}";
            var stack = new Stack<int>();
            int[] best = null;
            int i = 0;
            while (i < text.Length)
            {
                char c = text[i];
                if (c == '"' || c == '\'' || c == '`')
                {
                    int j = i + 1;
                    while (j < text.Length && text[j] != c && text[j] != '\n')
                    {
                        if (text[j] == '\\') j++;
                        j++;
                    }
                    if (j < text.Length && text[j] == c)
                    {
                        i = j + 1;
                        continue;
                    }
                }
                if (opens.IndexOf(c) >= 0)
                {
                    stack.Push(i);
                }
                else if (closes.IndexOf(c) >= 0)
                {
                    int kind = closes.IndexOf(c);
                    while (stack.Count > 0)
                    {
                        int o = stack.Pop();
                        if (opens.IndexOf(text[o]) == kind)
                        {
                            if (o < s && i + 1 >= e && (best == null || i - o < best[1] - best[0]))
                            {
                                best = new[] { o, i };
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
            bool active = ctx.St.SelType == SelType.Block && Selections.HasSelection(sel);
            bool back = Selections.BackwardP(ctx) != (ctx.St.TakeCount(1) < 0);
            int s = active ? Math.Min(sel.Anchor, sel.Active) : sel.Active;
            int e = active ? Math.Max(sel.Anchor, sel.Active) : sel.Active;
            int[] p = EnclosingPair(text, s, e);
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
                (ctx.St.SelType == SelType.Block && Selections.BackwardP(ctx))
                    || ctx.St.TakeCount(1) < 0;
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
            int n = ctx.St.TakeCount(1);
            int ln = Text.LineOfOffset(text, Selections.Primary(ctx).Active);
            if (n >= 0)
            {
                int pl = ln - 1;
                while (pl >= 0 && Things.Blank(text, pl)) pl--;
                if (pl < 0) return;
                int m = Text.LineEnd(text, pl);
                int p = Text.LineStart(text, ln);
                int eol = Text.LineEnd(text, ln);
                while (p < eol && char.IsWhiteSpace(text[p])) p++;
                Selections.Select(ctx, SelType.Join, m, p, true);
            }
            else
            {
                int last = Text.LineCount(text) - 1;
                int nl = ln + 1;
                while (nl <= last && Things.Blank(text, nl)) nl++;
                if (nl > last) return;
                int m = Text.LineEnd(text, ln);
                int p = Text.LineStart(text, nl);
                int eol = Text.LineEnd(text, nl);
                while (p < eol && char.IsWhiteSpace(text[p])) p++;
                Selections.Select(ctx, SelType.Join, m, p, true);
            }
        }
    }
}
