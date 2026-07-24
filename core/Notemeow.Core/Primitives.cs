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

namespace Notemeow.Core
{
    public sealed class SelRange
    {
        public SelRange(int anchor, int active)
        {
            Anchor = anchor;
            Active = active;
        }

        public int Anchor { get; }
        public int Active { get; }

        public int Lo() => Math.Min(Anchor, Active);

        public int Hi() => Math.Max(Anchor, Active);

        public override bool Equals(object obj)
        {
            return obj is SelRange other && Anchor == other.Anchor && Active == other.Active;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return Anchor * 31 + Active;
            }
        }
    }

    public sealed class TextEdit
    {
        public TextEdit(int start, int end, string text)
        {
            Start = start;
            End = end;
            Text = text;
        }

        public int Start { get; }
        public int End { get; }
        public string Text { get; }
    }

    public sealed class OffsetRange
    {
        public OffsetRange(int start, int end)
        {
            Start = start;
            End = end;
        }

        public int Start { get; }
        public int End { get; }
    }

    public sealed class LineRange
    {
        public LineRange(int first, int last)
        {
            First = first;
            Last = last;
        }

        public int First { get; }
        public int Last { get; }
    }

    public sealed class SavedSelection
    {
        public SavedSelection(SelType? type, bool expand, int anchor, int active)
        {
            Type = type;
            Expand = expand;
            Anchor = anchor;
            Active = active;
        }

        public SelType? Type { get; }
        public bool Expand { get; }
        public int Anchor { get; }
        public int Active { get; }

        public override bool Equals(object obj)
        {
            return obj is SavedSelection other
                && Type == other.Type
                && Expand == other.Expand
                && Anchor == other.Anchor
                && Active == other.Active;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int h = Type.HasValue ? (int)Type.Value + 1 : 0;
                h = h * 31 + (Expand ? 1 : 0);
                h = h * 31 + Anchor;
                h = h * 31 + Active;
                return h;
            }
        }
    }

    public sealed class AvyLabel
    {
        public AvyLabel(int offset, string label)
        {
            Offset = offset;
            Label = label;
        }

        public int Offset { get; }
        public string Label { get; }
    }
}
