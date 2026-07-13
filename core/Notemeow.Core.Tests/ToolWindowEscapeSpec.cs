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

using Xunit;
using static Notemeow.Core.ToolWindowEscape;

namespace Notemeow.Core.Tests
{
    public class ToolWindowEscapeSpec
    {
        public ToolWindowEscapeSpec()
        {
            Reset();
        }

        [Fact(DisplayName = "given a single escape in a tool window then it does not jump")]
        public void SingleEscapeNoJump()
        {
            Assert.False(OnEscape("terminal", 1_000));
        }

        [Fact(DisplayName =
            "given a second escape in the same tool window within the timeout then it jumps")]
        public void SecondEscapeSameWindowJumps()
        {
            OnEscape("terminal", 1_000);
            Assert.True(OnEscape("terminal", 1_000 + TimeoutMs));
        }

        [Fact(DisplayName = "given a completed jump then the next escape starts a new pair")]
        public void CompletedJumpStartsNewPair()
        {
            OnEscape("terminal", 1_000);
            Assert.True(OnEscape("terminal", 1_100));
            Assert.False(OnEscape("terminal", 1_200));
        }

        [Fact(DisplayName = "given escapes slower than the timeout then they do not pair but re-arm")]
        public void SlowerThanTimeoutReArms()
        {
            OnEscape("terminal", 1_000);
            Assert.False(OnEscape("terminal", 1_001 + TimeoutMs));
            Assert.True(OnEscape("terminal", 1_200 + TimeoutMs));
        }

        [Fact(DisplayName = "given escapes in different tool windows then they do not pair")]
        public void DifferentWindowsNoPair()
        {
            OnEscape("terminal", 1_000);
            Assert.False(OnEscape("list", 1_100));
            Assert.True(OnEscape("list", 1_200));
        }

        [Fact(DisplayName = "given focus outside any tool window then the pair breaks")]
        public void FocusOutsideBreaksPair()
        {
            OnEscape("terminal", 1_000);
            Assert.False(OnEscape(null, 1_100));
            Assert.False(OnEscape("terminal", 1_200));
        }
    }
}
