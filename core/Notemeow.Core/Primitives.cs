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
    public sealed class SelRange(int anchor, int active)
    {
        public int Anchor { get; } = anchor;
        public int Active { get; } = active;

        public int SelStart() => Math.Min(Anchor, Active);

        public int SelEnd() => Math.Max(Anchor, Active);

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

    public sealed class TextEdit(int start, int end, string text)
    {
        public int Start { get; } = start;
        public int End { get; } = end;
        public string Text { get; } = text;
    }

    public sealed class OffsetRange(int start, int end)
    {
        public int Start { get; } = start;
        public int End { get; } = end;
    }

    public sealed class LineRange(int first, int last)
    {
        public int First { get; } = first;
        public int Last { get; } = last;
    }

    public sealed class SavedSelection(SelType? type, bool expand, int anchor, int active)
    {
        public SelType? Type { get; } = type;
        public bool Expand { get; } = expand;
        public int Anchor { get; } = anchor;
        public int Active { get; } = active;

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

    public sealed class AvyLabel(int offset, string label)
    {
        public int Offset { get; } = offset;
        public string Label { get; } = label;
    }
}
