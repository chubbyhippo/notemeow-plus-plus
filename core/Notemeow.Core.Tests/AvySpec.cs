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

using Notemeow.Core;
using Xunit;

namespace Notemeow.Core.Tests
{
    public class AvySpec : SpecDsl
    {
        private void Timeout()
        {
            Avy.FinishInput(Ctx());
        }

        [Fact(DisplayName = "given S with input matching many places then labels select the jump target")]
        public void SInputManyPlacesLabelsSelect()
        {
            Given("repeats", "<caret>foo bar foo baz foo");
            WhenKeys("S");
            WhenKeys("fo");
            Timeout();
            WhenKeys("s");
            ThenCaretAt(8);
            Assert.Null(St.Avy);
        }

        [Fact(DisplayName = "given a single candidate then avy jumps immediately (avy-single-candidate-jump)")]
        public void SingleCandidateJumpsImmediately()
        {
            Given("words", "<caret>alpha beta gamma");
            WhenKeys("S");
            WhenKeys("gam");
            Timeout();
            ThenCaretAt(11);
            Assert.Null(St.Avy);
        }

        [Fact(DisplayName = "given no candidates then the session ends where it started")]
        public void NoCandidatesEndsWhereStarted()
        {
            Given("words", "<caret>alpha beta");
            WhenKeys("S");
            WhenKeys("zz");
            Timeout();
            ThenCaretAt(0);
            Assert.Null(St.Avy);
            WhenKeys("l");
            ThenCaretAt(1);
        }

        [Fact(DisplayName = "given matching is case-insensitive (avy-case-fold-search)")]
        public void MatchingCaseInsensitive()
        {
            Given("mixed case", "<caret>Foo bar fOO");
            WhenKeys("S");
            WhenKeys("foo");
            Timeout();
            WhenKeys("s");
            ThenCaretAt(8);
        }

        [Fact(DisplayName = "given an active selection then the avy jump extends it (avy-action-goto)")]
        public void ActiveSelectionJumpExtends()
        {
            Given("words", "<caret>hello world again");
            WhenKeys("w");
            WhenKeys("S");
            WhenKeys("aga");
            Timeout();
            ThenSelection("hello world ");
            ThenCaretAtSelectionEnd();
        }

        [Fact(DisplayName = "given a bad selection key then avy stays active (avy-handler-default)")]
        public void BadSelectionKeyStaysActive()
        {
            Given("repeats", "<caret>xx xx xx");
            WhenKeys("S");
            WhenKeys("xx");
            Timeout();
            WhenKeys("z");
            Assert.NotNull(St.Avy);
            WhenKeys("d");
            ThenCaretAt(6);
        }

        [Fact(
            DisplayName =
                "given more candidates than keys then leading keys stay single and the last key hosts a subtree")]
        public void MoreCandidatesThanKeysSubtree()
        {
            Given("ten es", "<caret>e e e e e e e e e e");
            WhenKeys("S");
            WhenKeys("e");
            Timeout();
            WhenKeys("l");
            Assert.NotNull(St.Avy);
            WhenKeys("s");
            ThenCaretAt(18);
        }

        [Fact(DisplayName = "given escape during an avy session then it cancels in place")]
        public void EscapeCancelsInPlace()
        {
            Given("words", "<caret>foo foo foo");
            WhenKeys("S");
            WhenKeys("foo");
            Timeout();
            Assert.NotNull(St.Avy);
            Assert.True(PressEsc());
            Assert.Null(St.Avy);
            ThenCaretAt(0);
        }

        [Fact(DisplayName = "given Q then visible lines are labeled and a key jumps to that line")]
        public void QLabelsVisibleLinesJump()
        {
            Given("four lines", "one\ntwo\nthr<caret>ee\nfour");
            WhenKeys("Q");
            Assert.NotNull(St.Avy);
            WhenKeys("f");
            ThenCaretAt(14);
            Assert.Null(St.Avy);
        }

        [Fact(DisplayName = "given Q then a digit switches to the goto-line number prompt")]
        public void QDigitSwitchesToNumberPrompt()
        {
            Given("four lines", "<caret>one\ntwo\nthree\nfour");
            GivenMinibufferAnswers("3");
            WhenKeys("Q3");
            ThenCaretAt(8);
            Assert.Null(St.Avy);
        }

        [Fact(DisplayName = "the avy-subdiv distribution matches avy 0-5-0")]
        public void AvySubdivDistribution()
        {
            Assert.Equal(new[] { 1, 1, 1, 1, 1, 1, 1, 1, 1 }, Avy.Subdiv(9, 9));
            Assert.Equal(new[] { 1, 1, 1, 1, 1, 1, 1, 1, 2 }, Avy.Subdiv(10, 9));
            Assert.Equal(new[] { 1, 1, 1, 1, 9, 9, 9, 9, 9 }, Avy.Subdiv(49, 9));
            Assert.Equal(new[] { 9, 9, 9, 9, 9, 9, 9, 9, 9 }, Avy.Subdiv(81, 9));
        }
    }
}
