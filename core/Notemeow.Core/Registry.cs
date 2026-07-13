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
    public static class Registry
    {
        public static readonly IReadOnlyDictionary<string, MeowCommand> Commands = Build();

        private static IReadOnlyDictionary<string, MeowCommand> Build()
        {
            var commands = new Dictionary<string, MeowCommand>();
            foreach (var e in Motions.Commands) commands[e.Key] = e.Value;
            foreach (var e in Selections.Commands) commands[e.Key] = e.Value;
            foreach (var e in Search.Commands) commands[e.Key] = e.Value;
            foreach (var e in Structures.Commands) commands[e.Key] = e.Value;
            foreach (var e in Grab.Commands) commands[e.Key] = e.Value;
            foreach (var e in Edits.Commands) commands[e.Key] = e.Value;
            commands["meow-negative-argument"] = ctx => ctx.St.Negative = true;
            commands["negative-argument"] = ctx => ctx.St.Negative = true;
            commands["meow-quit"] = ctx => ctx.Port.CloseEditor();
            commands["meow-keypad"] = Engine.EnterKeypad;
            commands["repeat"] = Engine.RepeatLast;
            commands["ignore"] = ctx => { };
            return commands;
        }
    }
}
