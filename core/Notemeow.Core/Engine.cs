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
            new(null, null, "meow-keypad", true);

        private const int MaxReplayDepth = 8;

        public static Dictionary<char, Rc.Binding> RepeatMap { get; set; }

        public static void EnterKeypad(Ctx ctx)
        {
            MeowState state = ctx.State;
            if (state.Mode == MeowMode.Keypad) return;
            state.KeypadPreviousState = state.Mode;
            ctx.SetMode(MeowMode.Keypad);
            ctx.Ui.ScheduleWhichKey("keypad", "");
        }

        public static void RunEmacsMotion(Ctx ctx, string command)
        {
            if (Registry.Commands.TryGetValue(command, out MeowCommand cmd)) cmd(ctx);
            ctx.Ui.Refresh(ctx.State);
        }

        public static bool HandleChar(Ctx ctx, char c)
        {
            MeowState state = ctx.State;
            if (state.Mode == MeowMode.Insert) return false;
            if (state.Mode == MeowMode.Keypad)
            {
                Keypad.Key(ctx, c);
                state.LastCommand = "keypad";
                ctx.Ui.Refresh(state);
                return true;
            }
            if (state.Avy != null)
            {
                Avy.Key(ctx, c);
                state.LastCommand = "avy";
                ctx.Ui.Refresh(state);
                return true;
            }

            ctx.Ui.HideWhichKey();
            ctx.Ui.ClearExpandHints();

            Pending? pend = state.Pending;
            Rc.Binding repeatBinding = null;
            if (pend == null && RepeatMap != null) RepeatMap.TryGetValue(c, out repeatBinding);
            if (pend == null && repeatBinding == null) RepeatMap = null;
            bool motionish = state.Mode == MeowMode.Motion;
            Rc.Binding binding =
                pend == null
                    ? repeatBinding ?? Resolve(ctx, c, motionish)
                    : null;
            string cmd = binding?.Command;

            if (!state.Replaying && cmd != "repeat")
            {
                if (pend == null && state.PendingCount == 0 && !state.Negative) state.Unit.Clear();
                state.Unit.Add(c);
            }

            if (pend != null)
            {
                state.Pending = null;
                ResolvePending(ctx, pend.Value, c);
                state.LastCommand = "pending";
            }
            else if (binding != null)
            {
                RunBinding(ctx, binding);
                state.LastCommand =
                    cmd ?? binding.Action ?? state.LastCommand;
            }
            else
            {
                state.LastCommand = null;
            }

            bool awaitingMoreKeys =
                state.Pending != null
                    || (state.PendingCount != 0 && cmd != null && cmd.StartsWith("meow-expand-"))
                    || (state.Negative && cmd == "meow-negative-argument")
                    || cmd == "meow-keypad";
            if (!state.Replaying && cmd != "repeat" && !awaitingMoreKeys)
            {
                state.LastKeys = [.. state.Unit];
            }

            ctx.Ui.Refresh(state);
            return true;
        }

        private static Rc.Binding Resolve(Ctx ctx, char c, bool motion)
        {
            if (c == ' ') return KeypadBinding;
            if (ctx.State.NoremapDepth == 0)
            {
                Rc.Config cfg = Rc.Cfg();
                if ((motion ? cfg.Motion : cfg.Normal).TryGetValue(c, out Rc.Binding user)) return user;
            }
            Rc.Config d = Rc.Defaults();
            (motion ? d.Motion : d.Normal).TryGetValue(c, out Rc.Binding def);
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
            MeowState state = ctx.State;
            IReadOnlyList<char> keys = state.LastKeys;
            if (keys.Count == 0) return;
            state.Replaying = true;
            try
            {
                foreach (char k in keys) HandleChar(ctx, k);
            }
            finally
            {
                state.Replaying = false;
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
            MeowState state = ctx.State;
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
            if (state.ReplayDepth >= MaxReplayDepth)
            {
                ctx.Ui.Hint("notemeow: mapping recursion is too deep");
                return;
            }
            bool savedReplaying = state.Replaying;
            state.Replaying = true;
            state.ReplayDepth++;
            if (!b.Recursive) state.NoremapDepth++;
            try
            {
                for (int i = 0; i < b.Keys.Length; i++) HandleChar(ctx, b.Keys[i]);
            }
            finally
            {
                if (!b.Recursive) state.NoremapDepth--;
                state.ReplayDepth--;
                state.Replaying = savedReplaying;
            }
        }

        public static bool EscapeKey(Ctx ctx)
        {
            MeowState state = ctx.State;
            if (state.Avy != null)
            {
                Avy.Cancel(ctx);
                ctx.Ui.Refresh(state);
                return true;
            }
            bool hadTransient = state.Pending != null || RepeatMap != null;
            state.Pending = null;
            RepeatMap = null;
            ctx.Ui.HideWhichKey();
            ctx.Ui.ClearExpandHints();
            if (state.Mode == MeowMode.Insert)
            {
                ctx.SetMode(MeowMode.Normal);
                ctx.Ui.Refresh(state);
                return true;
            }
            if (state.Mode == MeowMode.Keypad)
            {
                Keypad.Exit(ctx);
                ctx.Ui.Refresh(state);
                return true;
            }
            List<SelRange> sels = ctx.Port.GetSelections();
            if (sels.Count > 1 || Selections.HasSelection(sels[0]))
            {
                Selections.CancelAll(ctx);
                ctx.Ui.Refresh(state);
                return true;
            }
            return hadTransient;
        }
    }
}
