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
    public static class TreeMeow
    {
        private static readonly Dictionary<string, string> ListMotions =
            new Dictionary<string, string>
            {
                { "meow-next", "notemeow.tree.focusDown" },
                { "meow-prev", "notemeow.tree.focusUp" },
                { "meow-left", "notemeow.tree.collapse" },
                { "meow-right", "notemeow.tree.expand" },
            };

        public static HashSet<char> BoundChars()
        {
            var all = new HashSet<char>(Rc.Defaults().Motion.Keys);
            foreach (char c in Rc.Cfg().Motion.Keys) all.Add(c);
            var bound = new HashSet<char>();
            foreach (char c in all)
            {
                Rc.Binding b;
                if (!Rc.Cfg().Motion.TryGetValue(c, out b))
                {
                    Rc.Defaults().Motion.TryGetValue(c, out b);
                }
                if (b != null && b.Command != "ignore") bound.Add(c);
            }
            return bound;
        }

        public static void Dispatch(Action<string> run, char c)
        {
            Dispatch(run, c, false, 0);
        }

        public static void Dispatch(Action<string> run, char c, bool noremap, int depth)
        {
            Rc.Binding b = null;
            if (!noremap) Rc.Cfg().Motion.TryGetValue(c, out b);
            if (b == null) Rc.Defaults().Motion.TryGetValue(c, out b);
            if (b == null) return;
            if (b.Command != null)
            {
                if (ListMotions.TryGetValue(b.Command, out string listCommand)) run(listCommand);
                return;
            }
            if (b.Action != null)
            {
                run(b.Action);
                return;
            }
            if (b.Keys == null || depth >= 8) return;
            foreach (char k in b.Keys) Dispatch(run, k, noremap || !b.Recursive, depth + 1);
        }
    }
}
