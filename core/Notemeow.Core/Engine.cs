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
    public static class Engine
    {
        private static readonly Rc.Binding KeypadBinding =
            new Rc.Binding(null, null, "meow-keypad", true);

        private const int MaxReplayDepth = 8;

        public static Dictionary<char, Rc.Binding> RepeatMap;

        public static void EnterKeypad(Ctx ctx)
        {
            MeowState st = ctx.St;
            if (st.Mode == MeowMode.Keypad) return;
            st.KeypadPreviousState = st.Mode;
            ctx.SetMode(MeowMode.Keypad);
            ctx.Ui.ScheduleWhichKey("keypad", "");
        }

        public static void RunEmacsMotion(Ctx ctx, string command)
        {
            if (Registry.Commands.TryGetValue(command, out MeowCommand cmd)) cmd(ctx);
            ctx.Ui.Refresh(ctx.St);
        }

        public static bool HandleChar(Ctx ctx, char c)
        {
            MeowState st = ctx.St;
            if (st.Mode == MeowMode.Insert) return false;
            if (st.Mode == MeowMode.Keypad)
            {
                Keypad.Key(ctx, c);
                st.LastCommand = "keypad";
                ctx.Ui.Refresh(st);
                return true;
            }
            if (st.Avy != null)
            {
                Avy.Key(ctx, c);
                st.LastCommand = "avy";
                ctx.Ui.Refresh(st);
                return true;
            }

            ctx.Ui.HideWhichKey();
            ctx.Ui.ClearExpandHints();

            Pending? pend = st.Pending;
            Rc.Binding repeatBinding = null;
            if (pend == null && RepeatMap != null) RepeatMap.TryGetValue(c, out repeatBinding);
            if (pend == null && repeatBinding == null) RepeatMap = null;
            bool motionish = st.Mode == MeowMode.Motion;
            Rc.Binding binding =
                pend == null
                    ? repeatBinding != null ? repeatBinding : Resolve(ctx, c, motionish)
                    : null;
            string cmd = binding != null ? binding.Command : null;

            if (!st.Replaying && cmd != "repeat")
            {
                if (pend == null && st.PendingCount == 0 && !st.Negative) st.Unit.Clear();
                st.Unit.Add(c);
            }

            if (pend != null)
            {
                st.Pending = null;
                ResolvePending(ctx, pend.Value, c);
                st.LastCommand = "pending";
            }
            else if (binding != null)
            {
                RunBinding(ctx, binding);
                st.LastCommand =
                    cmd != null
                        ? cmd
                        : binding.Action != null ? binding.Action : st.LastCommand;
            }
            else
            {
                st.LastCommand = null;
            }

            bool awaitingMoreKeys =
                st.Pending != null
                    || (st.PendingCount != 0 && cmd != null && cmd.StartsWith("meow-expand-"))
                    || (st.Negative && cmd == "meow-negative-argument")
                    || cmd == "meow-keypad";
            if (!st.Replaying && cmd != "repeat" && !awaitingMoreKeys)
            {
                st.LastKeys = new List<char>(st.Unit);
            }

            ctx.Ui.Refresh(st);
            return true;
        }

        private static Rc.Binding Resolve(Ctx ctx, char c, bool motion)
        {
            if (c == ' ') return KeypadBinding;
            if (ctx.St.NoremapDepth == 0)
            {
                Rc.Config cfg = Rc.Cfg();
                Rc.Binding user;
                if ((motion ? cfg.Motion : cfg.Normal).TryGetValue(c, out user)) return user;
            }
            Rc.Config d = Rc.Defaults();
            Rc.Binding def;
            (motion ? d.Motion : d.Normal).TryGetValue(c, out def);
            return def;
        }

        private static void ResolvePending(Ctx ctx, Pending p, char c)
        {
            switch (p)
            {
                case Pending.Find:
                    Motions.FindTill(ctx, c, false);
                    break;
                case Pending.Till:
                    Motions.FindTill(ctx, c, true);
                    break;
                case Pending.Inner:
                case Pending.Bounds:
                case Pending.Begin:
                case Pending.End:
                    Structures.ThingSelect(ctx, p, c);
                    break;
                default:
                    break;
            }
        }

        public static void RepeatLast(Ctx ctx)
        {
            MeowState st = ctx.St;
            IReadOnlyList<char> keys = st.LastKeys;
            if (keys.Count == 0) return;
            st.Replaying = true;
            try
            {
                foreach (char k in keys) HandleChar(ctx, k);
            }
            finally
            {
                st.Replaying = false;
            }
        }

        public static void RunBinding(Ctx ctx, Rc.Binding b)
        {
            Dispatch(ctx, b);
            Dictionary<char, Rc.Binding> map = Rc.RepeatMapFor(b);
            if (map == null) return;
            if (RepeatMap == null)
            {
                var keys = new StringBuilder();
                foreach (char k in map.Keys)
                {
                    if (keys.Length > 0) keys.Append(", ");
                    keys.Append(k);
                }
                ctx.Ui.Hint("Repeat with " + keys);
            }
            RepeatMap = map;
        }

        private static void Dispatch(Ctx ctx, Rc.Binding b)
        {
            MeowState st = ctx.St;
            if (b.Command != null)
            {
                if (Registry.Commands.TryGetValue(b.Command, out MeowCommand cmd)) cmd(ctx);
                else ctx.Ui.Hint("Unknown meow command: " + b.Command);
                return;
            }
            if (b.Action != null)
            {
                try
                {
                    ctx.Ui.RunCommand(b.Action);
                }
                catch (System.Exception)
                {
                    ctx.Ui.Hint("Unknown command: " + b.Action);
                }
                return;
            }
            if (b.Keys == null) return;
            if (st.ReplayDepth >= MaxReplayDepth)
            {
                ctx.Ui.Hint("notemeow: mapping recursion is too deep");
                return;
            }
            bool savedReplaying = st.Replaying;
            st.Replaying = true;
            st.ReplayDepth++;
            if (!b.Recursive) st.NoremapDepth++;
            try
            {
                for (int i = 0; i < b.Keys.Length; i++) HandleChar(ctx, b.Keys[i]);
            }
            finally
            {
                if (!b.Recursive) st.NoremapDepth--;
                st.ReplayDepth--;
                st.Replaying = savedReplaying;
            }
        }

        public static bool EscapeKey(Ctx ctx)
        {
            MeowState st = ctx.St;
            if (st.Avy != null)
            {
                Avy.Cancel(ctx);
                ctx.Ui.Refresh(st);
                return true;
            }
            st.Pending = null;
            RepeatMap = null;
            ctx.Ui.HideWhichKey();
            ctx.Ui.ClearExpandHints();
            if (st.Mode == MeowMode.Insert)
            {
                ctx.SetMode(MeowMode.Normal);
                ctx.Ui.Refresh(st);
                return true;
            }
            if (st.Mode == MeowMode.Keypad)
            {
                Keypad.Exit(ctx);
                ctx.Ui.Refresh(st);
                return true;
            }
            List<SelRange> sels = ctx.Port.GetSelections();
            if (sels.Count > 1)
            {
                SelRange p = sels[0];
                ctx.Port.SetSelections(
                    new List<SelRange> { new SelRange(p.Active, p.Active) });
                ctx.Ui.Refresh(st);
                return true;
            }
            return false;
        }
    }
}
