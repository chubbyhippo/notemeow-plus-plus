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
using System.Text;

namespace Notemeow.Core
{
    public static class RcFileState
    {
        private static string state;

        private static string Fingerprint(Rc.Config c)
        {
            var sb = new StringBuilder();
            AppendBindings(sb, "n", c.Normal);
            AppendBindings(sb, "m", c.Motion);
            AppendKeypad(sb, c.Keypad);
            AppendDescs(sb, c.KeypadDesc);
            AppendRepeat(sb, c.Repeat);
            sb.Append("wk=").Append(c.WhichKey).Append(';');
            sb.Append("delay=").Append(c.WhichKeyDelayMs).Append(';');
            return sb.ToString();
        }

        private static void AppendBindings(
            StringBuilder sb, string tag, Dictionary<char, Rc.Binding> map)
        {
            var keys = new List<char>(map.Keys);
            keys.Sort();
            foreach (char k in keys) AppendBinding(sb.Append(tag).Append(k).Append('='), map[k]);
        }

        private static void AppendKeypad(StringBuilder sb, Dictionary<string, Rc.Binding> map)
        {
            var keys = new List<string>(map.Keys);
            keys.Sort(System.StringComparer.Ordinal);
            foreach (string k in keys)
                AppendBinding(sb.Append("k[").Append(k).Append("]="), map[k]);
        }

        private static void AppendDescs(StringBuilder sb, Dictionary<string, string> map)
        {
            var keys = new List<string>(map.Keys);
            keys.Sort(System.StringComparer.Ordinal);
            foreach (string k in keys)
                sb.Append("d[").Append(k).Append("]=").Append(map[k]).Append(';');
        }

        private static void AppendRepeat(
            StringBuilder sb, Dictionary<string, Dictionary<char, Rc.Binding>> map)
        {
            var groups = new List<string>(map.Keys);
            groups.Sort(System.StringComparer.Ordinal);
            foreach (string g in groups)
            {
                var keys = new List<char>(map[g].Keys);
                keys.Sort();
                foreach (char k in keys)
                    AppendBinding(sb.Append("r[").Append(g).Append('.').Append(k).Append("]="),
                        map[g][k]);
            }
        }

        private static void AppendBinding(StringBuilder sb, Rc.Binding b)
        {
            sb.Append(b.Action)
                .Append('|')
                .Append(b.Keys)
                .Append('|')
                .Append(b.Command)
                .Append('|')
                .Append(b.Recursive)
                .Append(';');
        }

        internal static void SaveParsed(Rc.Config c)
        {
            state = Fingerprint(c);
        }

        public static bool EqualTo(List<string> lines)
        {
            string s = state;
            return s != null && Fingerprint(Rc.Parse(lines)) == s;
        }

        internal static void ResetForTest()
        {
            state = null;
        }
    }
}
