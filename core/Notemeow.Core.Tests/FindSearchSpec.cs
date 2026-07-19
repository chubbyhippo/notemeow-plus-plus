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
using Notemeow.Core;
using Xunit;

namespace Notemeow.Core.Tests
{
    public class FindSearchSpec : SpecDsl
    {
        private int SelMin()
        {
            SelRange s = Editor.Sels[0];
            return Math.Min(s.Anchor, s.Active);
        }

        [Fact(DisplayName = "given f X then selects from point through the char inclusive")]
        public void FXSelectsThroughInclusive()
        {
            Given("marker text", "<caret>abcXdef");
            WhenKeys("fX");
            ThenSelection("abcX");
            ThenSelType(SelType.Find);
            ThenCaretAtSelectionEnd();
        }

        [Fact(DisplayName = "given t X then selects up to but excluding the char")]
        public void TXSelectsExcluding()
        {
            Given("marker text", "<caret>abcXdef");
            WhenKeys("tX");
            ThenSelection("abc");
            ThenSelType(SelType.Till);
        }

        [Fact(DisplayName =
            "given w then f X then a fresh find selection runs from the word end through the char")]
        public void WThenFXFreshFind()
        {
            Given("comma separated", "w<caret>ord1, word2 word3");
            WhenKeys("w");
            ThenSelection("word1");
            WhenKeys("f3");
            ThenSelection(", word2 word3");
            ThenSelType(SelType.Find);
            ThenCaretAtSelectionEnd();
        }

        [Fact(DisplayName = "given w then t X then the till selection stops before the char")]
        public void WThenTXTillStops()
        {
            Given("comma separated", "w<caret>ord1, word2 word3");
            WhenKeys("wt3");
            ThenSelection(", word2 word");
            ThenSelType(SelType.Till);
        }

        [Fact(DisplayName = "given a count when 2 f a then the second occurrence is reached")]
        public void CountTwoFA()
        {
            Given("repeating", "<caret>xaxaxa");
            WhenKeys("2fa");
            ThenSelection("xaxa");
        }

        [Fact(DisplayName =
            "given a find selection when digit then it expands to the next occurrence")]
        public void FindDigitExpands()
        {
            Given("repeating", "<caret>xaxaxa");
            WhenKeys("fa1");
            ThenSelection("xaxa");
            WhenKeys("1");
            ThenSelection("xaxaxa");
        }

        [Fact(DisplayName = "given the char is absent when f then nothing changes")]
        public void FindAbsentCharNoChange()
        {
            Given("plain", "<caret>hello");
            WhenKeys("fZ");
            ThenNoSelection();
            ThenCaretAt(0);
        }

        [Fact(DisplayName = "given negative argument when - f then finds backward")]
        public void NegativeFindsBackward()
        {
            Given("repeating", "xabc<caret>def");
            WhenKeys("-fa");
            ThenSelection("abc");
            ThenCaretAtSelectionStart();
        }

        [Fact(DisplayName = "given w then n repeats the pushed word search forward (meow-search)")]
        public void WThenNSearchForward()
        {
            Given("repeats", "<caret>foo bar foo baz foo");
            WhenKeys("w");
            WhenKeys("n");
            ThenSelection("foo");
            Assert.Equal(8, SelMin());
        }

        [Fact(DisplayName = "given repeated n then the search wraps at the end of the buffer")]
        public void RepeatedNWraps()
        {
            Given("repeats", "<caret>foo bar foo");
            WhenKeys("wnn");
            Assert.Equal(0, SelMin());
            ThenSelection("foo");
        }

        [Fact(DisplayName = "given a reversed selection when n then the search goes backward")]
        public void ReversedNSearchBackward()
        {
            Given("repeats", "foo bar <caret>foo bar foo");
            WhenKeys("w");
            WhenKeys(";");
            WhenKeys("n");
            Assert.Equal(0, SelMin());
            ThenSelection("foo");
            ThenCaretAtSelectionStart();
        }

        [Fact(DisplayName =
            "given a selection that does not match the pattern when n then the selection text becomes the pattern")]
        public void NonMatchingSelectionBecomesPattern()
        {
            Given("repeats", "foo <caret>bar foo bar");
            St.SearchHistory.Add("zzz");
            WhenKeys(",e");
            WhenKeys("n");
            ThenSelection("bar");
            Assert.Equal(12, SelMin());
        }

        [Fact(DisplayName = "given no pattern and no selection when n then nothing is selected")]
        public void NNoPatternNothing()
        {
            Given("plain", "<caret>hello");
            WhenKeys("n");
            ThenNoSelection();
        }

        [Fact(DisplayName =
            "given visit with minibuffer input then the first match after point is selected")]
        public void VisitFirstMatchAfterPoint()
        {
            Given("repeats", "<caret>alpha beta gamma beta");
            GivenMinibufferAnswers("beta");
            WhenKeys("v");
            ThenSelection("beta");
            Assert.Equal(6, SelMin());
            ThenSelType(SelType.Visit);
        }

        [Fact(DisplayName = "given visit then n continues to the next match")]
        public void VisitThenNContinues()
        {
            Given("repeats", "<caret>alpha beta gamma beta");
            GivenMinibufferAnswers("beta");
            WhenKeys("vn");
            Assert.Equal(17, SelMin());
        }

        [Fact(DisplayName = "given W on a dollar symbol then n finds the next symbol occurrence")]
        public void DollarSymbolMarkThenNFindsNext()
        {
            Given("dollar symbols", "$<caret>foo bar $foo");
            WhenKeys("W");
            ThenSelection("$foo");
            WhenKeys("n");
            ThenSelection("$foo");
            ThenCaretAt(13);
        }
    }
}
