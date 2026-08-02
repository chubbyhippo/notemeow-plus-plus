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
    public static class View
    {
        public const string RecenterCommand = "recenter-top-bottom";

        public static readonly IReadOnlyList<RevealAt> RecenterPositions =
            [RevealAt.Center, RevealAt.Top, RevealAt.Bottom];

        internal static readonly Dictionary<string, MeowCommand> Commands =
            new() { [RecenterCommand] = Recenter };

        public static RevealAt RecenterPosition(int phase)
        {
            int count = RecenterPositions.Count;
            return RecenterPositions[((phase % count) + count) % count];
        }

        public static int NextRecenterPhase(string previousCommand, int phase)
        {
            return previousCommand == RecenterCommand ? phase + 1 : 0;
        }

        private static void Recenter(Ctx ctx)
        {
            MeowState state = ctx.State;
            state.RecenterPhase = NextRecenterPhase(state.LastCommand, state.RecenterPhase);
            state.LastCommand = RecenterCommand;
            ctx.Ui.RevealCaret(RecenterPosition(state.RecenterPhase));
        }
    }
}
