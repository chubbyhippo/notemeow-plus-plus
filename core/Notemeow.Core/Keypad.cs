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
    public static class Keypad
    {
        public static void Key(Ctx ctx, char c)
        {
            MeowState st = ctx.St;
            ctx.Ui.HideWhichKey();
            Dictionary<string, Rc.Binding> keypad = Rc.Keypad();
            string buf = st.Keypad.ToString();

            if (buf == "/")
            {
                Describe(ctx, c);
                Exit(ctx);
                return;
            }
            if (buf.Length == 0)
            {
                if (c >= '0' && c <= '9')
                {
                    st.PendingCount = st.PendingCount * 10 + (c - '0');
                    Exit(ctx);
                    return;
                }
                if (c == '?')
                {
                    Exit(ctx);
                    ctx.Ui.Info("Meow Cheatsheet", Cheatsheet);
                    return;
                }
                if (c == '/')
                {
                    st.Keypad.Append('/');
                    return;
                }
            }

            st.Keypad.Append(c);
            string cur = st.Keypad.ToString();
            if (keypad.TryGetValue(cur, out Rc.Binding binding))
            {
                Exit(ctx);
                Engine.RunBinding(ctx, binding);
                return;
            }
            bool hasPrefix = false;
            foreach (string seq in keypad.Keys)
            {
                if (seq.StartsWith(cur))
                {
                    hasPrefix = true;
                    break;
                }
            }
            if (!hasPrefix)
            {
                Exit(ctx);
                ctx.Ui.Hint("SPC " + Spaced(cur) + " is undefined");
            }
            else
            {
                ctx.Ui.ScheduleWhichKey("keypad", cur);
            }
        }

        public static void Exit(Ctx ctx)
        {
            ctx.Ui.HideWhichKey();
            ctx.SetMode(ctx.St.KeypadPreviousState);
        }

        private static string Spaced(string seq)
        {
            var outText = new StringBuilder();
            for (int i = 0; i < seq.Length; i++)
            {
                if (i > 0) outText.Append(' ');
                outText.Append(seq[i]);
            }
            return outText.ToString();
        }

        private static void Describe(Ctx ctx, char c)
        {
            Dictionary<string, string> descs = Rc.KeypadDescs();
            var rows = new List<string>();
            var seqs = new List<string>();
            foreach (var e in Rc.Keypad())
            {
                if (e.Key.StartsWith(c.ToString())) seqs.Add(e.Key);
            }
            seqs.Sort(System.StringComparer.Ordinal);
            Dictionary<string, Rc.Binding> keypad = Rc.Keypad();
            foreach (string seq in seqs)
            {
                Rc.Binding b = keypad[seq];
                string target =
                    b.Action != null
                        ? b.Action
                        : b.Command != null ? b.Command : b.Keys != null ? b.Keys : "";
                string desc = descs.ContainsKey(seq) ? "  (" + descs[seq] + ")" : "";
                rows.Add("SPC " + Spaced(seq) + "  ->  " + target + desc);
            }
            string entries = string.Join("\n", rows);
            ctx.Ui.Info(
                "Meow Describe: SPC " + c,
                entries.Length == 0 ? "SPC " + c + " is undefined" : entries);
        }

        public const string Cheatsheet =
            "The bundled default layout (meow's suggested QWERTY) — every key below can\n"
            + "be rebound from ~/.notemeowrc.\n"
            + "\n"
            + "NORMAL — selection first, then act\n"
            + "  h j k l  move (cancel selection)       H J K L  extend char selection\n"
            + "  w / W    mark word / symbol            e / E    next word / symbol end\n"
            + "  b / B    back word / symbol            x        line (repeat: extend)\n"
            + "  f / t    find / till char (inclusive / exclusive)\n"
            + "  1-9, 0   expand selection by N units (0 = 10); without selection: count\n"
            + "  -        negative argument              ;        reverse selection\n"
            + "  i / a    insert at start / end          I / A    open line above / below\n"
            + "  c        change                         s        kill (cut)\n"
            + "  d / D    delete char/sel fwd / back     y        save (copy)\n"
            + "  p        yank (paste at point)          r        replace selection with clipboard\n"
            + "  u        undo                           '        repeat last command\n"
            + "  z        pop selection                  g        cancel selection / cursors\n"
            + "  Q / X    goto line                      q        close editor tab\n"
            + "  ESC      insert -> normal; drops extra cursors\n"
            + "\n"
            + "KEYPAD (SPC)\n"
            + "  SPC 1-9 count   SPC ? this sheet   SPC / describe key\n"
            + "  the SPC command table itself is rc lines: map <leader><seq> <target>\n"
            + "\n"
            + "~/.notemeowrc: nmap <key> <action>(command.id) | nmap <key> meow-command | nmap <key> <meow keys>\n"
            + "  mmap ... (MOTION mode) | map <leader><seq> ... | desc <leader><seq> text | set nowhich-key\n"
            + "  repeat <group> <key> <target> — tap-to-continue groups\n"
            + "  every binding above is an rc line — the defaults ship bundled inside the\n"
            + "  plugin; ~/.notemeowrc overrides them key by key";
    }
}
