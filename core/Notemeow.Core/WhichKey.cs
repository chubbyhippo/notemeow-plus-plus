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
    public static class WhichKey
    {
        public sealed class Row(string key, string label)
        {
            public string Key { get; } = key;
            public string Label { get; } = label;
        }

        public static readonly IReadOnlyList<Row> Things =
        [
            new("r", "round ( )"),
            new("s", "square [ ]"),
            new("c", "curly { }"),
            new("g", "string"),
            new("e", "symbol"),
            new("w", "window"),
            new("b", "buffer"),
            new("p", "paragraph"),
            new("l", "line"),
            new("v", "visual line"),
            new("d", "defun"),
            new(".", "sentence"),
        ];

        public static List<Row> KeypadRows(string buffer)
        {
            Dictionary<string, string> descs = Rc.KeypadDescs();
            var rows = new Dictionary<string, string>();
            var order = new List<string>();
            foreach (var e in Rc.Keypad())
            {
                string seq = e.Key;
                if (!seq.StartsWith(buffer) || seq == buffer) continue;
                string child = buffer + seq[buffer.Length];
                string label;
                if (seq == child)
                {
                    Rc.Binding b = e.Value;
                    label =
                        descs.TryGetValue(seq, out string value)
                            ? value : b.Action ?? b.Command ?? b.Keys ?? "";
                }
                else
                {
                    label = descs.TryGetValue(child, out string value) ? value : "+more";
                }
                if (!rows.ContainsKey(child))
                {
                    rows[child] = label;
                    order.Add(child);
                }
                else if (descs.ContainsKey(child))
                {
                    rows[child] = label;
                }
            }
            order.Sort(System.StringComparer.Ordinal);
            var outRows = new List<Row>();
            foreach (string child in order)
            {
                char key = child[child.Length - 1];
                outRows.Add(new Row(key == ' ' ? "SPC" : key.ToString(), rows[child]));
            }
            return outRows;
        }
    }
}
