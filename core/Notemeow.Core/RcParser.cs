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
using System.Text.RegularExpressions;

namespace Notemeow.Core
{
    internal static class RcParser
    {
        private static readonly Regex ActionRe =
            new Regex("^<action>\\(([\\w.\\-$(),=]+)\\)$", RegexOptions.IgnoreCase);
        private static readonly Regex WhichKeyLetRe =
            new Regex("^let\\s+g:WhichKeyDesc\\w*\\s*=\\s*\"(.+)\"$");
        private static readonly Regex TrailingCommentRe = new Regex("\\s\"");
        private static readonly HashSet<string> ColorSetKeys = new HashSet<string>
        {
            "overlay-color",
            "overlay-text-color",
            "expand-hint-color",
            "grab-color",
        };
        private static readonly Regex HexColorRe = new Regex("^[0-9a-fA-F]{6}$");

        internal static Rc.Config Parse(List<string> lines)
        {
            var c = new Rc.Config();
            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i].Trim();
                int lineNo = i + 1;
                Action<string> err = msg => c.Errors.Add("line " + lineNo + ": " + msg);

                if (line.Length == 0 || line.StartsWith("\"") || line.StartsWith("#")) continue;

                Match wk = WhichKeyLetRe.Match(line);
                if (wk.Success)
                {
                    ParseDescBody(c, wk.Groups[1].Value, err);
                    continue;
                }

                Match cut = TrailingCommentRe.Match(line);
                if (cut.Success) line = line.Substring(0, cut.Index).TrimEnd();
                if (line.Length == 0) continue;

                SplitFirst(line, out string cmd, out string rest);
                switch (cmd)
                {
                    case "let":
                    case "cmap":
                    case "cnoremap":
                        break;
                    case "set":
                        ParseSet(c, rest, err);
                        break;
                    case "desc":
                        ParseDescBody(c, rest, err);
                        break;
                    case "map":
                    case "noremap":
                    case "nmap":
                    case "nnoremap":
                    case "mmap":
                    case "mnoremap":
                        ParseMap(c, cmd, rest, err);
                        break;
                    case "repeat":
                        ParseRepeat(c, rest, err);
                        break;
                    default:
                        err("unknown command '" + cmd + "'");
                        break;
                }
            }
            return c;
        }

        private static void SplitFirst(string s, out string first, out string rest)
        {
            Match m = Regex.Match(s, "^(\\S+)(?:\\s+(.*))?$", RegexOptions.Singleline);
            first = m.Groups[1].Value;
            rest = m.Groups[2].Success ? m.Groups[2].Value.Trim() : "";
        }

        private static void ParseSet(Rc.Config c, string rest, Action<string> err)
        {
            if (rest == "which-key")
            {
                c.WhichKey = true;
            }
            else if (rest == "nowhich-key")
            {
                c.WhichKey = false;
            }
            else if (rest.StartsWith("timeoutlen"))
            {
                string eq =
                    rest.Contains("=") ? rest.Substring(rest.IndexOf('=') + 1).Trim() : "";
                int? n;
                if (eq.Length != 0)
                {
                    n = ParseIntOrNull(eq);
                }
                else
                {
                    string[] parts = Regex.Split(rest, "\\s+");
                    n = ParseIntOrNull(parts.Length > 1 ? parts[1] : "");
                }
                if (n != null && n.Value >= 0) c.WhichKeyDelayMs = n.Value;
            }
            else
            {
                ParseSetColor(c, rest, err);
            }
        }

        private static int? ParseIntOrNull(string s)
        {
            return int.TryParse(s, out int n) ? n : (int?)null;
        }

        private static void ParseSetColor(Rc.Config c, string rest, Action<string> err)
        {
            int eqIndex = rest.IndexOf('=');
            string key = (eqIndex >= 0 ? rest.Substring(0, eqIndex) : rest).Trim();
            if (!ColorSetKeys.Contains(key)) return;
            string value = eqIndex >= 0 ? rest.Substring(eqIndex + 1).Trim() : "";
            int? color = ParseHexColor(value);
            if (color == null)
            {
                err("set " + key + ": invalid color '" + value + "' (expected #RRGGBB)");
                return;
            }
            switch (key)
            {
                case "overlay-color":
                    c.OverlayColor = color;
                    break;
                case "overlay-text-color":
                    c.OverlayTextColor = color;
                    break;
                case "expand-hint-color":
                    c.ExpandHintColor = color;
                    break;
                case "grab-color":
                    c.GrabColor = color;
                    break;
            }
        }

        private static int? ParseHexColor(string text)
        {
            string hex = text.StartsWith("#") ? text.Substring(1) : text;
            if (!HexColorRe.IsMatch(hex)) return null;
            return Convert.ToInt32(hex, 16);
        }

        private static void ParseDescBody(Rc.Config c, string body, Action<string> err)
        {
            if (!body.StartsWith("<leader>"))
            {
                err("descriptions must start with <leader>: " + body);
                return;
            }
            string after = body.Substring("<leader>".Length);
            string seqToken = Regex.Split(after, "\\s", RegexOptions.None)[0];
            string desc = after.Substring(seqToken.Length).Trim();
            string seq = ParseKeys(seqToken, err);
            if (seq == null) return;
            if (seq.Length == 0)
            {
                err("empty key sequence in description: " + body);
                return;
            }
            c.KeypadDesc[seq] = desc;
        }

        private static void ParseMap(Rc.Config c, string cmd, string rest, Action<string> err)
        {
            SplitFirst(rest, out string lhs, out string rhs);
            if (lhs.Length == 0 || rhs.Length == 0)
            {
                err(cmd + " needs a key and a target");
                return;
            }
            bool recursive = cmd == "map" || cmd == "nmap" || cmd == "mmap";
            bool motion = cmd == "mmap" || cmd == "mnoremap";

            Rc.Binding binding = ParseTarget(rhs, recursive, cmd + " " + rest, err);
            if (binding == null) return;

            if (lhs.StartsWith("<leader>"))
            {
                if (motion)
                {
                    err(cmd + " cannot define keypad entries; use map <leader>...");
                    return;
                }
                string seq = ParseKeys(lhs.Substring("<leader>".Length), err);
                if (seq == null) return;
                if (seq.Length == 0)
                {
                    err("<leader> alone cannot be mapped");
                }
                else if ("0123456789?/".IndexOf(seq[0]) >= 0)
                {
                    err(
                        "keypad "
                            + seq[0]
                            + " is reserved (digit argument / cheatsheet / describe)");
                }
                else
                {
                    c.Keypad[seq] = binding;
                }
                return;
            }

            string keys = ParseKeys(lhs, err);
            if (keys == null) return;
            if (keys.Length != 1)
            {
                err(
                    (motion ? "motion" : "normal")
                        + "-mode key must be a single printable key: "
                        + lhs);
            }
            else if (keys == " ")
            {
                err("SPC is the keypad key and cannot be remapped");
            }
            else
            {
                (motion ? c.Motion : c.Normal)[keys[0]] = binding;
            }
        }

        private static Rc.Binding ParseTarget(
            string rhs, bool recursive, string errContext, Action<string> err)
        {
            Match am = ActionRe.Match(rhs);
            if (am.Success) return new Rc.Binding(am.Groups[1].Value, null, null, recursive);
            if (Registry.Commands.ContainsKey(rhs))
                return new Rc.Binding(null, null, rhs, recursive);
            if (rhs.StartsWith("meow-"))
            {
                err("unknown meow command '" + rhs + "'");
                return null;
            }
            string keys = ParseKeys(Regex.Replace(rhs, "\\s+", ""), err);
            if (keys == null) return null;
            if (keys.Length == 0)
            {
                err("empty target in '" + errContext + "'");
                return null;
            }
            return new Rc.Binding(null, keys, null, recursive);
        }

        private static void ParseRepeat(Rc.Config c, string rest, Action<string> err)
        {
            SplitFirst(rest, out string group, out string afterGroup);
            SplitFirst(afterGroup, out string keyToken, out string target);
            if (group.Length == 0 || keyToken.Length == 0 || target.Length == 0)
            {
                err("repeat needs a group, a member key and a target");
                return;
            }
            string key = ParseKeys(keyToken, err);
            if (key == null) return;
            if (key.Length != 1)
            {
                err("repeat member key must be a single printable key: " + keyToken);
            }
            else if (key == " ")
            {
                err("SPC is the keypad key and cannot be a repeat member");
            }
            else
            {
                Rc.Binding binding = ParseTarget(target.Trim(), true, "repeat " + rest, err);
                if (binding == null) return;
                if (!c.Repeat.TryGetValue(group, out var members))
                {
                    members = new Dictionary<char, Rc.Binding>();
                    c.Repeat[group] = members;
                }
                members[key[0]] = binding;
            }
        }

        private static string ParseKeys(string s, Action<string> err)
        {
            var outKeys = new StringBuilder();
            int i = 0;
            while (i < s.Length)
            {
                char ch = s[i];
                if (ch == '<')
                {
                    int close = s.IndexOf('>', i);
                    if (close < 0)
                    {
                        outKeys.Append(ch);
                        i++;
                        continue;
                    }
                    string token = s.Substring(i + 1, close - i - 1).ToLowerInvariant();
                    if (token == "space")
                    {
                        outKeys.Append(' ');
                    }
                    else if (token == "lt")
                    {
                        outKeys.Append('<');
                    }
                    else
                    {
                        err(
                            "unsupported key token "
                                + s.Substring(i, close + 1 - i)
                                + " (only printable keys reach the meow engine)");
                        return null;
                    }
                    i = close + 1;
                }
                else
                {
                    outKeys.Append(ch);
                    i++;
                }
            }
            return outKeys.ToString();
        }
    }
}
