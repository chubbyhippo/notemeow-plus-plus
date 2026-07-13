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
using Notemeow.Core;
using Xunit;

namespace Notemeow.Core.Tests
{
    public class SelectionSpec : SpecDsl
    {
        [Fact(DisplayName = "given caret on a word when w then the word is marked and caret sits at its end")]
        public void MarkWord()
        {
            Given("two words", "<caret>hello world");
            WhenKeys("w");
            ThenSelection("hello");
            ThenSelType(SelType.Word);
            ThenCaretAtSelectionEnd();
        }

        [Fact(DisplayName = "given caret between words when w then the next word is marked")]
        public void MarkWordBetween()
        {
            Given("gap between words", "hello <caret> world");
            WhenKeys("w");
            ThenSelection("world");
        }

        [Fact(DisplayName = "given a symbol with underscore when W then the whole symbol is marked")]
        public void MarkSymbol()
        {
            Given("snake case", "<caret>foo_bar baz");
            WhenKeys("W");
            ThenSelection("foo_bar");
            ThenSelType(SelType.Symbol);
        }

        [Fact(DisplayName = "given w then W distinction - w stops at underscore boundary chars")]
        public void WordStopsAtUnderscore()
        {
            Given("snake case", "<caret>foo_bar baz");
            WhenKeys("w");
            ThenSelection("foo");
        }

        [Fact(DisplayName = "given a bare e when pressed twice then it steps word by word (non-expandable)")]
        public void BareEStepsWords()
        {
            Given("three words", "<caret>one two three");
            WhenKeys("e");
            ThenSelection("one");
            WhenKeys("e");
            ThenSelection("two");
        }

        [Fact(
            DisplayName =
                "given words separated by punctuation when e e e then each selection is one bare word")]
        public void EeeBareWords()
        {
            Given("comma separated", "<caret>word1, word2 word3");
            WhenKeys("ee");
            ThenSelection("word2");
            WhenKeys("e");
            ThenSelection("word3");
            ThenCaretAtSelectionEnd();
        }

        [Fact(DisplayName = "given b b b from the end then each selection is one bare word")]
        public void BbbBareWords()
        {
            Given("comma separated", "word1, word2 word3<caret>");
            WhenKeys("b");
            ThenSelection("word3");
            ThenCaretAtSelectionStart();
            WhenKeys("bb");
            ThenSelection("word1");
        }

        [Fact(DisplayName = "given e then b then the same word is re-selected backward")]
        public void EbReselectsBackward()
        {
            Given("comma separated", "<caret>word1, word2 word3");
            WhenKeys("eb");
            ThenSelection("word1");
            ThenCaretAtSelectionStart();
        }

        [Fact(DisplayName = "given a selection of another type when e then the history restarts at the cancel")]
        public void TypeMismatchRestartsHistory()
        {
            Given("two lines", "<caret>hello world\nnext line");
            WhenKeys("x");
            ThenSelection("hello world");
            WhenKeys("e");
            ThenSelection("next");
            WhenKeys("z");
            ThenNoSelection();
            ThenCaretAt(11);
        }

        [Fact(DisplayName = "given w first when e then the word selection extends (meow expand-word rule)")]
        public void WThenEExtends()
        {
            Given("three words", "<caret>one two three");
            WhenKeys("we");
            ThenSelection("one two");
            WhenKeys("e");
            ThenSelection("one two three");
        }

        [Fact(DisplayName = "given w then b extends the selection backward anchored at the word end")]
        public void WThenBExtendsBackward()
        {
            Given("three words", "one t<caret>wo three");
            WhenKeys("w");
            ThenSelection("two");
            WhenKeys("b");
            ThenSelection("one two");
            ThenCaretAtSelectionStart();
        }

        [Fact(DisplayName = "given w b then e re-normalizes forward and extends to the right")]
        public void WbThenERenormalizes()
        {
            Given("three words", "one t<caret>wo three");
            WhenKeys("wbe");
            ThenSelection("one two three");
            ThenCaretAtSelectionEnd();
        }

        [Fact(DisplayName = "given W then B extends the symbol selection backward")]
        public void ShiftWThenB()
        {
            Given("symbols", "foo_a bar_b<caret> baz_c");
            WhenKeys("W");
            ThenSelection("bar_b");
            WhenKeys("B");
            ThenSelection("foo_a bar_b");
            ThenCaretAtSelectionStart();
        }

        [Fact(DisplayName = "given caret at end when b then selects back to word beginning")]
        public void BAtEnd()
        {
            Given("two words", "hello world<caret>");
            WhenKeys("b");
            ThenSelection("world");
            ThenCaretAtSelectionStart();
        }

        [Fact(DisplayName = "given negative argument when - e then selects backward like b")]
        public void NegativeELikeB()
        {
            Given("two words", "hello<caret> world");
            WhenKeys("-e");
            ThenSelection("hello");
            ThenCaretAtSelectionStart();
        }

        [Fact(DisplayName = "given E and B then symbol variants honor underscores")]
        public void SymbolVariants()
        {
            Given("snake case", "<caret>foo_bar baz");
            WhenKeys("E");
            ThenSelection("foo_bar");
            ThenSelType(SelType.Symbol);
        }

        [Fact(DisplayName = "given x then the current line is selected without the newline")]
        public void LineWithoutNewline()
        {
            Given("two lines", "li<caret>ne one\nline two");
            WhenKeys("x");
            ThenSelection("line one");
            ThenSelType(SelType.Line);
            ThenCaretAtSelectionEnd();
        }

        [Fact(DisplayName = "given a line selection when x again then it extends one line down")]
        public void LineExtendsDown()
        {
            Given("three lines", "<caret>one\ntwo\nthree");
            WhenKeys("xx");
            ThenSelection("one\ntwo");
        }

        [Fact(DisplayName = "given a reversed line selection when x then it extends upward")]
        public void ReversedLineExtendsUp()
        {
            Given("three lines", "one\ntwo\nth<caret>ree");
            WhenKeys("x;x");
            ThenSelection("two\nthree");
            ThenCaretAtSelectionStart();
        }

        [Fact(
            DisplayName =
                "given a selection then expand hints overlay the text without inserting inline content")]
        public void ExpandHintsOverlay()
        {
            Given("three words", "<caret>hello world again");
            WhenKeys("w");
            Assert.True(Ui.ExpandHints.Count > 0, "hint positions computed");
            Assert.Equal(11, Ui.ExpandHints[0]);
            WhenKeys("g");
            Assert.Empty(Ui.ExpandHints);
        }

        [Fact(
            DisplayName =
                "given a find selection when the target char sits at the caret then the first hint marks it")]
        public void FindHintAtCaret()
        {
            Given("chars", "<caret>aXX");
            WhenKeys("fX");
            Assert.Equal(new List<int> { 3 }, Ui.ExpandHints);
        }

        [Fact(DisplayName = "given digits after w then the selection expands by that many words")]
        public void DigitsExpandWords()
        {
            Given("five words", "<caret>one two three four five");
            WhenKeys("w2");
            ThenSelection("one two three");
        }

        [Fact(DisplayName = "given 0 after a word mark then the selection expands by ten units")]
        public void ZeroExpandsTen()
        {
            Given("twelve words", "<caret>a b c d e f g h i j k l");
            WhenKeys("w0");
            ThenSelection("a b c d e f g h i j k");
        }

        [Fact(DisplayName = "given digits after x then the selection expands by lines")]
        public void DigitsExpandLines()
        {
            Given("three lines", "<caret>one\ntwo\nthree");
            WhenKeys("x2");
            ThenSelection("one\ntwo\nthree");
        }

        [Fact(DisplayName = "given a reversed selection when digit then it expands backward")]
        public void ReversedDigitExpandsBackward()
        {
            Given("three lines", "one\ntwo\nthr<caret>ee");
            WhenKeys("x;1");
            ThenSelection("two\nthree");
        }

        [Fact(DisplayName = "given semicolon then point and mark swap (meow-reverse)")]
        public void SemicolonReverses()
        {
            Given("two words", "<caret>hello world");
            WhenKeys("w");
            ThenCaretAtSelectionEnd();
            WhenKeys(";");
            ThenSelection("hello");
            ThenCaretAtSelectionStart();
            WhenKeys(";");
            ThenCaretAtSelectionEnd();
        }

        [Fact(
            DisplayName =
                "given goto line via minibuffer then that line is selected (meow-goto-line expands line selection)")]
        public void GotoLineViaMinibuffer()
        {
            Given("three lines", "<caret>one\ntwo\nthree");
            GivenMinibufferAnswers("2");
            WhenKeys("X");
            ThenSelection("two");
            ThenSelType(SelType.Line);
        }

        [Fact(DisplayName = "given Q then goto-line as well (QWERTY binds both Q and X)")]
        public void QGotoLine()
        {
            Given("three lines", "<caret>one\ntwo\nthree");
            GivenRc("nmap Q meow-goto-line");
            GivenMinibufferAnswers("3");
            WhenKeys("Q");
            ThenSelection("three");
        }

        [Fact(
            DisplayName =
                "given a selection history when z then the previous selection is restored with its type")]
        public void ZRestoresPreviousSelection()
        {
            Given("two words", "<caret>hello world");
            WhenKeys("w");
            WhenKeys("x");
            WhenKeys("z");
            ThenSelection("hello");
            ThenSelType(SelType.Word);
            ThenCaretAtSelectionEnd();
        }

        [Fact(
            DisplayName =
                "given w then z then the caret returns to where the chain started (null placeholder)")]
        public void ZNullPlaceholder()
        {
            Given("two words", "he<caret>llo world");
            WhenKeys("w");
            ThenSelection("hello");
            WhenKeys("z");
            ThenNoSelection();
            ThenCaretAt(2);
        }

        [Fact(DisplayName = "given g then the selection history is cleared (meow--cancel-selection)")]
        public void GClearsHistory()
        {
            Given("two words", "<caret>hello world");
            WhenKeys("wxg");
            WhenKeys("z");
            ThenNoSelection();
        }

        [Fact(DisplayName = "given a digit expand then the selection is demoted to select type")]
        public void DigitExpandDemotes()
        {
            Given("five words", "<caret>one two three four five");
            WhenKeys("w2");
            ThenSelection("one two three");
            WhenKeys("e");
            ThenSelection("four");
        }

        [Fact(DisplayName = "given x 2 then x re-selects the current line instead of extending")]
        public void DigitDemotedLineReselects()
        {
            Given("four lines", "<caret>one\ntwo\nthree\nfour");
            WhenKeys("x2");
            ThenSelection("one\ntwo\nthree");
            WhenKeys("x");
            ThenSelection("three");
        }

        [Fact(
            DisplayName =
                "given no history but a grab when z then the grab becomes the selection (meow-pop-grab fallback)")]
        public void PopGrabFallback()
        {
            Given("two words", "<caret>hello world");
            WhenKeys("wG");
            St.SelectionHistory.Clear();
            WhenKeys("z");
            ThenSelection("hello");
        }

        [Fact(DisplayName = "given g then the selection is cancelled")]
        public void GCancels()
        {
            Given("two words", "<caret>hello world");
            WhenKeys("w");
            ThenSelection("hello");
            WhenKeys("g");
            ThenNoSelection();
        }
    }
}
