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
    public class ToolWindowEscapeSpec : SpecDsl
    {
        private const string NavRc =
            "map <leader>tn meow-next\n"
            + "repeat nav . meow-next\n"
            + "repeat nav , meow-prev";

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

        [Fact(DisplayName = "given KEYPAD then escape is meow's and exits the keypad")]
        public void KeypadEscapeIsMeows()
        {
            Given("keypad escape", "<caret>hello");
            WhenKeys(" ");
            ThenMode(MeowMode.Keypad);
            Assert.True(PressEsc());
            ThenMode(MeowMode.Normal);
        }

        [Fact(DisplayName = "given an active selection then escape is meow's and clears it")]
        public void SelectionEscapeIsMeows()
        {
            Given("selection escape", "<caret>hello world");
            WhenKeys("w");
            Assert.NotNull(SelectedText());
            Assert.True(PressEsc());
            Assert.Null(SelectedText());
        }

        [Fact(DisplayName = "given an armed repeat run then escape is meow's and ends it")]
        public void RepeatRunEscapeIsMeows()
        {
            Given("four lines", "<caret>one\ntwo\nthree\nfour");
            GivenRc(NavRc);
            WhenKeys(" tn");
            Assert.NotNull(Engine.RepeatMap);
            Assert.True(PressEsc());
            Assert.Null(Engine.RepeatMap);
        }

        [Fact(DisplayName = "given NORMAL with nothing to cancel then escape is not meow's")]
        public void IdleEscapeIsNotMeows()
        {
            Given("idle escape", "<caret>hello");
            Assert.False(PressEsc());
        }

        [Fact(DisplayName = "given INSERT then escape is meow's and returns to NORMAL")]
        public void InsertEscapeIsMeows()
        {
            Given("insert escape", "<caret>hello");
            WhenKeys("i");
            ThenMode(MeowMode.Insert);
            Assert.True(PressEsc());
            ThenMode(MeowMode.Normal);
        }
    }
}
