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
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Notemeow.Core
{
    public sealed class Chord(bool ctrl, bool alt, bool shift, char key)
    {
        public bool Ctrl { get; } = ctrl;
        public bool Alt { get; } = alt;
        public bool Shift { get; } = shift;
        public char Key { get; } = key;

        private static readonly Dictionary<string, char> PlainKeys =
            new()
            {
                ["SPC"] = ' ',
                ["SPACE"] = ' ',
                ["TAB"] = '\t',
                ["COMMA"] = ',',
                ["PERIOD"] = '.',
                ["SLASH"] = '/',
                ["SEMICOLON"] = ';',
                ["QUOTE"] = '\'',
                ["OPEN_BRACKET"] = '[',
                ["CLOSE_BRACKET"] = ']',
                ["BACK_SLASH"] = '\\',
                ["MINUS"] = '-',
                ["EQUALS"] = '=',
                ["BACK_QUOTE"] = '`',
            };

        private static readonly Dictionary<string, char> ShiftedKeys =
            new()
            {
                ["COMMA"] = '<',
                ["PERIOD"] = '>',
                ["SLASH"] = '?',
                ["SEMICOLON"] = ':',
                ["QUOTE"] = '"',
                ["OPEN_BRACKET"] = '{',
                ["CLOSE_BRACKET"] = '}',
                ["BACK_SLASH"] = '|',
                ["MINUS"] = '_',
                ["EQUALS"] = '+',
                ["BACK_QUOTE"] = '~',
                ["1"] = '!',
                ["2"] = '@',
                ["3"] = '#',
                ["4"] = '$',
                ["5"] = '%',
                ["6"] = '^',
                ["7"] = '&',
                ["8"] = '*',
                ["9"] = '(',
                ["0"] = ')',
            };

        private static readonly Regex Whitespace = new("\\s+");

        public static Chord Parse(string text)
        {
            if (text == null) return null;
            string rest = text.Trim();
            if (rest.Length == 0) return null;
            return rest.IndexOf(' ') >= 0 || rest.IndexOf('\t') >= 0
                ? ParseHostSpelling(rest)
                : ParsePrefixSpelling(rest);
        }

        private static Chord ParseHostSpelling(string text)
        {
            string[] tokens = Whitespace.Split(text);
            bool ctrl = false;
            bool alt = false;
            bool shift = false;
            for (int i = 0; i < tokens.Length - 1; i++)
            {
                switch (tokens[i].ToLowerInvariant())
                {
                    case "control":
                    case "ctrl":
                        ctrl = true;
                        break;
                    case "alt":
                    case "meta":
                        alt = true;
                        break;
                    case "shift":
                        shift = true;
                        break;
                    default:
                        return null;
                }
            }
            char? named = KeyNamed(tokens[tokens.Length - 1], shift);
            if (named == null || (!ctrl && !alt)) return null;
            char key = named.Value;
            bool shiftedLetter = char.IsLetter(key) && shift;
            return new Chord(ctrl, alt, shiftedLetter, char.ToLowerInvariant(key));
        }

        private static Chord ParsePrefixSpelling(string text)
        {
            string rest = text;
            bool ctrl = false;
            bool alt = false;
            bool shift = false;
            while (rest.Length > 2 && rest[1] == '-')
            {
                switch (char.ToUpperInvariant(rest[0]))
                {
                    case 'C':
                        ctrl = true;
                        break;
                    case 'M':
                    case 'A':
                        alt = true;
                        break;
                    case 'S':
                        shift = true;
                        break;
                    default:
                        return null;
                }
                rest = rest.Substring(2);
            }
            char? named = KeyNamed(rest, shift);
            if (named == null || (!ctrl && !alt)) return null;
            char key = named.Value;
            if (char.IsUpper(key))
            {
                shift = true;
                key = char.ToLowerInvariant(key);
            }
            return new Chord(ctrl, alt, shift, key);
        }

        private static char? KeyNamed(string token, bool shift)
        {
            string name = token.ToUpperInvariant();
            if (shift && ShiftedKeys.TryGetValue(name, out char shifted)) return shifted;
            if (PlainKeys.TryGetValue(name, out char plain)) return plain;
            return token.Length == 1 ? token[0] : (char?)null;
        }

        public string Spelling()
        {
            var outText = new StringBuilder();
            if (Ctrl) outText.Append("C-");
            if (Alt) outText.Append("M-");
            if (Shift) outText.Append("S-");
            return outText.Append(Key).ToString();
        }

        public override string ToString()
        {
            return Spelling();
        }

        public override bool Equals(object obj)
        {
            return obj is Chord other
                && Ctrl == other.Ctrl
                && Alt == other.Alt
                && Shift == other.Shift
                && Key == other.Key;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int h = Ctrl ? 1 : 0;
                h = h * 31 + (Alt ? 1 : 0);
                h = h * 31 + (Shift ? 1 : 0);
                h = h * 31 + Key;
                return h;
            }
        }
    }
}
