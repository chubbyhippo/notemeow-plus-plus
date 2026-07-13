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
    public sealed class MeowState
    {
        public MeowMode Mode = MeowMode.Normal;
        public SelType SelType = SelType.None;
        public bool SelExpand;
        public Pending? Pending;

        public int PendingCount;
        public bool Negative;

        public char? LastFind;

        public List<string> SearchHistory = new List<string>();

        public List<SavedSelection> SelectionHistory = new List<SavedSelection>();

        public SavedSelection LastSelection;

        public int? GoalColumn;

        public string LastCommand;

        public OffsetRange Grab;

        public AvySession Avy;

        public StringBuilder Keypad = new StringBuilder();

        public MeowMode KeypadPreviousState = MeowMode.Normal;

        public List<char> Unit = new List<char>();
        public IReadOnlyList<char> LastKeys = new List<char>();
        public bool Replaying;

        public int ReplayDepth;
        public int NoremapDepth;

        public int TakeCount()
        {
            return TakeCount(1);
        }

        public int TakeCount(int def)
        {
            int n = PendingCount == 0 ? def : PendingCount;
            int r = Negative ? -n : n;
            PendingCount = 0;
            Negative = false;
            return r;
        }
    }
}
