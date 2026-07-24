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
using System.IO;

namespace Notemeow.Core
{
    public static class Rc
    {
        public const string FileName = ".notemeowrc";

        public sealed class Binding
        {
            public Binding(string action, string keys, string command, bool recursive)
            {
                Action = action;
                Keys = keys;
                Command = command;
                Recursive = recursive;
            }

            public string Action { get; }
            public string Keys { get; }
            public string Command { get; }
            public bool Recursive { get; }
        }

        public sealed class Config
        {
            public readonly Dictionary<char, Binding> Normal = new Dictionary<char, Binding>();
            public readonly Dictionary<char, Binding> Motion = new Dictionary<char, Binding>();
            public readonly Dictionary<string, Binding> Keypad =
                new Dictionary<string, Binding>();
            public readonly Dictionary<string, string> KeypadDesc =
                new Dictionary<string, string>();

            public readonly Dictionary<string, Dictionary<char, Binding>> Repeat =
                new Dictionary<string, Dictionary<char, Binding>>();

            public bool? WhichKey;
            public int? WhichKeyDelayMs;
            public int? OverlayColor;
            public int? OverlayTextColor;
            public int? ExpandHintColor;
            public int? GrabColor;
            public readonly List<string> Errors = new List<string>();
        }

        private static Config userConfig = new Config();
        private static Config defaultConfig;

        public static Config Parse(List<string> lines)
        {
            return RcParser.Parse(lines);
        }

        public static Config InitDefaults(List<string> lines)
        {
            defaultConfig = Parse(lines);
            return defaultConfig;
        }

        public static Config SetUserLines(List<string> lines)
        {
            userConfig = Parse(lines);
            RcFileState.SaveParsed(userConfig);
            return userConfig;
        }

        public static void SetForTest(Config c)
        {
            userConfig = c;
            RcFileState.ResetForTest();
        }

        public static Config Cfg()
        {
            return userConfig;
        }

        public static Config Defaults()
        {
            if (defaultConfig == null) InitDefaults(ReadBundledLines());
            return defaultConfig;
        }

        public static List<string> BundledLines()
        {
            return ReadBundledLines();
        }

        private static List<string> ReadBundledLines()
        {
            var lines = new List<string>();
            var stream = typeof(Rc).Assembly.GetManifestResourceStream(FileName);
            if (stream == null) return lines;
            using (var reader = new StreamReader(stream))
            {
                string line;
                while ((line = reader.ReadLine()) != null) lines.Add(line);
            }
            return lines;
        }

        public static Dictionary<string, Binding> Keypad()
        {
            var merged = new Dictionary<string, Binding>(Defaults().Keypad);
            foreach (var e in Cfg().Keypad) merged[e.Key] = e.Value;
            return merged;
        }

        public static Dictionary<string, string> KeypadDescs()
        {
            var merged = new Dictionary<string, string>(Defaults().KeypadDesc);
            foreach (var e in Cfg().KeypadDesc) merged[e.Key] = e.Value;
            return merged;
        }

        public static Dictionary<string, Dictionary<char, Binding>> RepeatGroups()
        {
            var merged = new Dictionary<string, Dictionary<char, Binding>>();
            foreach (var e in Defaults().Repeat)
            {
                merged[e.Key] = new Dictionary<char, Binding>(e.Value);
            }
            foreach (var e in Cfg().Repeat)
            {
                if (!merged.TryGetValue(e.Key, out var members))
                {
                    members = new Dictionary<char, Binding>();
                    merged[e.Key] = members;
                }
                foreach (var m in e.Value) members[m.Key] = m.Value;
            }
            foreach (var members in merged.Values)
            {
                var drop = new List<char>();
                foreach (var m in members)
                {
                    if (m.Value.Command == "ignore") drop.Add(m.Key);
                }
                foreach (char k in drop) members.Remove(k);
            }
            var emptyGroups = new List<string>();
            foreach (var e in merged)
            {
                if (e.Value.Count == 0) emptyGroups.Add(e.Key);
            }
            foreach (string g in emptyGroups) merged.Remove(g);
            return merged;
        }

        public static Dictionary<char, Binding> RepeatMapFor(Binding b)
        {
            foreach (var members in RepeatGroups().Values)
            {
                foreach (Binding m in members.Values)
                {
                    if (m.Action == b.Action && m.Command == b.Command && m.Keys == b.Keys)
                    {
                        return members;
                    }
                }
            }
            return null;
        }

        public static bool WhichKeyEnabled()
        {
            if (Cfg().WhichKey != null) return Cfg().WhichKey.Value;
            if (Defaults().WhichKey != null) return Defaults().WhichKey.Value;
            return true;
        }

        private const int DefaultWhichKeyDelayMs = 250;

        public static int WhichKeyDelayMs()
        {
            if (Cfg().WhichKeyDelayMs != null) return Cfg().WhichKeyDelayMs.Value;
            if (Defaults().WhichKeyDelayMs != null) return Defaults().WhichKeyDelayMs.Value;
            return DefaultWhichKeyDelayMs;
        }

        private const int DefaultOverlayColor = 0xE52B50;
        private const int DefaultOverlayTextColor = 0xFFFFFF;
        private const int DefaultExpandHintColor = 0x2B5DB2;
        private const int DefaultGrabColor = 0x33CC33;

        public static int OverlayColor()
        {
            return ResolveColor(c => c.OverlayColor, DefaultOverlayColor);
        }

        public static int OverlayTextColor()
        {
            return ResolveColor(c => c.OverlayTextColor, DefaultOverlayTextColor);
        }

        public static int ExpandHintColor()
        {
            return ResolveColor(c => c.ExpandHintColor, DefaultExpandHintColor);
        }

        public static int GrabColor()
        {
            return ResolveColor(c => c.GrabColor, DefaultGrabColor);
        }

        private static int ResolveColor(Func<Config, int?> pick, int fallback)
        {
            int? u = pick(Cfg());
            if (u != null) return u.Value;
            int? d = pick(Defaults());
            if (d != null) return d.Value;
            return fallback;
        }
    }
}
