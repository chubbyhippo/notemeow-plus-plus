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

namespace Notemeow.Core
{
    public static class Windmove
    {
        public enum Dir
        {
            Left,
            Right,
            Up,
            Down,
        }

        public sealed class ViewLayout
        {
            public ViewLayout(bool twoViews, bool stacked, bool onSecond)
            {
                TwoViews = twoViews;
                Stacked = stacked;
                OnSecond = onSecond;
            }

            public bool TwoViews { get; }
            public bool Stacked { get; }
            public bool OnSecond { get; }
        }

        public const string FocusOtherView = "notemeow.focusOtherView";

        public static string Plan(Dir dir, ViewLayout layout)
        {
            if (layout == null || !layout.TwoViews) return null;
            if (!layout.Stacked)
            {
                if (dir == Dir.Left && layout.OnSecond) return FocusOtherView;
                if (dir == Dir.Right && !layout.OnSecond) return FocusOtherView;
                return null;
            }
            if (dir == Dir.Up && layout.OnSecond) return FocusOtherView;
            if (dir == Dir.Down && !layout.OnSecond) return FocusOtherView;
            return null;
        }

        public static string NoWindowMessage(Dir dir)
        {
            return "No window " + dir.ToString().ToLowerInvariant() + " from selected window";
        }
    }
}
