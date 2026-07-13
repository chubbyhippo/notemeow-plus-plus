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
        public sealed class Row
        {
            public Row(string key, string label)
            {
                Key = key;
                Label = label;
            }

            public string Key { get; }
            public string Label { get; }
        }

        public static readonly IReadOnlyList<Row> Things = new List<Row>
        {
            new Row("r", "round ( )"),
            new Row("s", "square [ ]"),
            new Row("c", "curly { }"),
            new Row("g", "string"),
            new Row("e", "symbol"),
            new Row("w", "window"),
            new Row("b", "buffer"),
            new Row("p", "paragraph"),
            new Row("l", "line"),
            new Row("v", "visual line"),
            new Row("d", "defun"),
            new Row(".", "sentence"),
        };

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
                        descs.ContainsKey(seq)
                            ? descs[seq]
                            : b.Action != null
                                ? b.Action
                                : b.Command != null ? b.Command : b.Keys != null ? b.Keys : "";
                }
                else
                {
                    label = descs.ContainsKey(child) ? descs[child] : "+more";
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
