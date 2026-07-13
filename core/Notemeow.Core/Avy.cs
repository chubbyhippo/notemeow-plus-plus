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
    public sealed class AvySession
    {
        internal enum Phase
        {
            Collecting,
            Selecting,
        }

        internal Phase CurrentPhase = Phase.Collecting;
        internal readonly StringBuilder Input = new StringBuilder();
        internal Avy.Branch Node;
        internal readonly bool GotoLine;

        internal AvySession(bool gotoLine)
        {
            GotoLine = gotoLine;
        }
    }

    public static class Avy
    {
        private const string Keys = "asdfghjkl";

        internal static readonly Dictionary<string, MeowCommand> Commands =
            new Dictionary<string, MeowCommand>
            {
                ["avy-goto-char-timer"] = StartCharTimer,
                ["avy-goto-line"] = StartGotoLine,
            };

        internal abstract class AvyNode
        {
        }

        internal sealed class Leaf : AvyNode
        {
            public Leaf(int offset)
            {
                Offset = offset;
            }

            public int Offset { get; }
        }

        internal sealed class Branch : AvyNode
        {
            public Branch(List<Entry> children)
            {
                Children = children;
            }

            public List<Entry> Children { get; }
        }

        internal sealed class Entry
        {
            public Entry(char key, AvyNode child)
            {
                Key = key;
                Child = child;
            }

            public char Key { get; }
            public AvyNode Child { get; }
        }

        private const double SubdivLogEpsilon = 1e-6;

        public static int[] Subdiv(int n, int b)
        {
            int p = (int)Math.Floor(Math.Log(n) / Math.Log(b) + SubdivLogEpsilon) - 1;
            int x1 = 1;
            for (int i = 0; i < p; i++) x1 *= b;
            int x2 = b * x1;
            int delta = n - x2;
            int n2 = (int)Math.Floor((double)delta / (x2 - x1));
            int n1 = b - n2 - 1;
            var outSizes = new int[b];
            int idx = 0;
            for (int i = 0; i < n1; i++) outSizes[idx++] = x1;
            outSizes[idx++] = n - n1 * x1 - n2 * x2;
            for (int i = 0; i < n2; i++) outSizes[idx++] = x2;
            return outSizes;
        }

        internal static Branch Tree(List<int> candidates, string keys)
        {
            var children = new List<Entry>();
            if (candidates.Count < keys.Length)
            {
                for (int i = 0; i < candidates.Count; i++)
                {
                    children.Add(new Entry(keys[i], new Leaf(candidates[i])));
                }
                return new Branch(children);
            }
            List<int> rest = candidates;
            int[] sizes = Subdiv(candidates.Count, keys.Length);
            for (int i = 0; i < sizes.Length; i++)
            {
                int size = sizes[i];
                var taken = rest.GetRange(0, size);
                rest = rest.GetRange(size, rest.Count - size);
                children.Add(
                    new Entry(keys[i], size == 1 ? new Leaf(taken[0]) : Tree(taken, keys)));
            }
            return new Branch(children);
        }

        internal static List<AvyLabel> LabelsOf(Branch node)
        {
            var outLabels = new List<AvyLabel>();
            Walk(node, "", outLabels);
            return outLabels;
        }

        private static void Walk(AvyNode n, string path, List<AvyLabel> outLabels)
        {
            if (n is Leaf leaf)
            {
                outLabels.Add(new AvyLabel(leaf.Offset, path));
            }
            else if (n is Branch b)
            {
                foreach (Entry e in b.Children) Walk(e.Child, path + e.Key, outLabels);
            }
        }

        private static void StartCharTimer(Ctx ctx)
        {
            Cancel(ctx);
            ctx.St.Avy = new AvySession(false);
        }

        private static void StartGotoLine(Ctx ctx)
        {
            Cancel(ctx);
            var session = new AvySession(true);
            ctx.St.Avy = session;
            string text = ctx.Port.GetText();
            int[] fl = VisibleLines(ctx);
            var candidates = new List<int>();
            for (int ln = fl[0]; ln <= fl[1]; ln++) candidates.Add(Text.LineStart(text, ln));
            ToSelecting(ctx, session, candidates);
        }

        public static void Key(Ctx ctx, char c)
        {
            AvySession session = ctx.St.Avy;
            if (session == null) return;
            if (session.CurrentPhase == AvySession.Phase.Collecting) Collect(ctx, session, c);
            else Select(ctx, session, c);
        }

        private static void Collect(Ctx ctx, AvySession session, char c)
        {
            session.Input.Append(c);
            int len = session.Input.Length;
            var ranges = new List<OffsetRange>();
            foreach (int start in Matches(ctx, session.Input.ToString()))
            {
                ranges.Add(new OffsetRange(start, start + len));
            }
            ctx.Ui.ShowAvyMatches(ranges);
        }

        public static void FinishInput(Ctx ctx)
        {
            AvySession session = ctx.St.Avy;
            if (session == null || session.CurrentPhase != AvySession.Phase.Collecting) return;
            List<int> candidates = Matches(ctx, session.Input.ToString());
            if (candidates.Count == 0)
            {
                Cancel(ctx);
                ctx.Ui.Hint("zero candidates");
            }
            else if (candidates.Count == 1)
            {
                Cancel(ctx);
                Jump(ctx, candidates[0]);
            }
            else
            {
                ToSelecting(ctx, session, candidates);
            }
        }

        private static void ToSelecting(Ctx ctx, AvySession session, List<int> candidates)
        {
            ctx.Ui.ClearAvy();
            session.CurrentPhase = AvySession.Phase.Selecting;
            session.Node = Tree(candidates, Keys);
            ctx.Ui.ShowAvyLabels(LabelsOf(session.Node));
        }

        private static void Select(Ctx ctx, AvySession session, char c)
        {
            if (session.GotoLine && c >= '0' && c <= '9')
            {
                Cancel(ctx);
                string input = ctx.Ui.Input("Goto line:", c.ToString());
                if (input == null) return;
                if (!int.TryParse(input.Trim(), out int n)) return;
                string text = ctx.Port.GetText();
                int ln = Math.Min(Math.Max(n - 1, 0), Text.LineCount(text) - 1);
                Jump(ctx, Text.LineStart(text, ln));
                return;
            }
            Branch node = session.Node;
            if (node == null) return;
            AvyNode child = null;
            foreach (Entry e in node.Children)
            {
                if (e.Key == c)
                {
                    child = e.Child;
                    break;
                }
            }
            if (child == null)
            {
                ctx.Ui.Hint("No such candidate: " + c);
            }
            else if (child is Leaf leaf)
            {
                Cancel(ctx);
                Jump(ctx, leaf.Offset);
            }
            else
            {
                session.Node = (Branch)child;
                ctx.Ui.ShowAvyLabels(LabelsOf((Branch)child));
            }
        }

        private static void Jump(Ctx ctx, int offset)
        {
            SelRange sel = Selections.Primary(ctx);
            if (Selections.HasSelection(sel))
            {
                ctx.Port.SetSelections(
                    new List<SelRange> { new SelRange(Selections.Mark(ctx), offset) });
            }
            else
            {
                ctx.Port.SetSelections(new List<SelRange> { new SelRange(offset, offset) });
            }
        }

        public static void Cancel(Ctx ctx)
        {
            if (ctx.St.Avy != null) ctx.Ui.ClearAvy();
            ctx.St.Avy = null;
        }

        public static bool AwaitingTimeout(MeowState st)
        {
            return st.Avy != null
                && st.Avy.CurrentPhase == AvySession.Phase.Collecting
                && st.Avy.Input.Length > 0;
        }

        private static int[] VisibleLines(Ctx ctx)
        {
            int total = Text.LineCount(ctx.Port.GetText());
            LineRange vis = ctx.Port.VisibleLineRange();
            if (vis == null) return new[] { 0, total - 1 };
            return new[]
            {
                Text.Clamp(vis.First, 0, total - 1),
                Text.Clamp(vis.Last, 0, total - 1),
            };
        }

        private static List<int> Matches(Ctx ctx, string input)
        {
            if (input.Length == 0) return new List<int>();
            string text = ctx.Port.GetText();
            int[] fl = VisibleLines(ctx);
            int from = Text.LineStart(text, fl[0]);
            int to = Text.LineEnd(text, fl[1]);
            string haystack = text.ToLowerInvariant();
            string needle = input.ToLowerInvariant();
            var outStarts = new List<int>();
            int i = from;
            while (i <= to - needle.Length)
            {
                if (StartsAt(haystack, needle, i))
                {
                    outStarts.Add(i);
                    i += needle.Length;
                }
                else
                {
                    i++;
                }
            }
            return outStarts;
        }

        private static bool StartsAt(string haystack, string needle, int at)
        {
            if (at < 0 || at + needle.Length > haystack.Length) return false;
            for (int k = 0; k < needle.Length; k++)
            {
                if (haystack[at + k] != needle[k]) return false;
            }
            return true;
        }
    }
}
