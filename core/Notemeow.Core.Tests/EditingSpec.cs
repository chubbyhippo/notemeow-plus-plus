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
    public class EditingSpec : SpecDsl
    {
        [Fact(DisplayName = "given a selection when i then INSERT starts at the selection beginning")]
        public void SelectionIStartsInsertAtBeginning()
        {
            Given("word", "<caret>hello world");
            WhenKeys("wi");
            ThenMode(MeowMode.Insert);
            ThenCaretAt(0);
            ThenNoSelection();
        }

        [Fact(DisplayName = "given a selection when a then INSERT starts at the selection end")]
        public void SelectionAStartsInsertAtEnd()
        {
            Given("word", "<caret>hello world");
            WhenKeys("wa");
            ThenMode(MeowMode.Insert);
            ThenCaretAt(5);
            ThenNoSelection();
        }

        [Fact(DisplayName = "given no selection when i then INSERT starts at point (no cursor-position hack)")]
        public void NoSelectionIStartsInsertAtPoint()
        {
            Given("word", "he<caret>llo");
            WhenKeys("i");
            ThenMode(MeowMode.Insert);
            ThenCaretAt(2);
        }

        [Fact(DisplayName = "given INSERT mode then printable keys are not intercepted")]
        public void InsertModePrintableNotIntercepted()
        {
            Given("word", "<caret>hello");
            WhenKeys("i");
            Assert.False(
                Engine.HandleChar(Ctx(), 'z'),
                "typed keys must reach the default handler in INSERT");
        }

        [Fact(DisplayName = "given A then a line opens below and INSERT starts")]
        public void CapitalAOpensLineBelow()
        {
            Given("one line", "ab<caret>cd");
            WhenKeys("A");
            ThenMode(MeowMode.Insert);
            ThenText("abcd\n");
            ThenCaretAt(5);
        }

        [Fact(DisplayName = "given I then a line opens above and INSERT starts")]
        public void CapitalIOpensLineAbove()
        {
            Given("one line", "ab<caret>cd");
            WhenKeys("I");
            ThenMode(MeowMode.Insert);
            ThenText("\nabcd");
            ThenCaretAt(0);
        }

        [Fact(DisplayName = "given a selection when c then it is killed into INSERT (meow-change)")]
        public void SelectionCKilledIntoInsert()
        {
            Given("word", "<caret>hello world");
            WhenKeys("wc");
            ThenText(" world");
            ThenMode(MeowMode.Insert);
            ThenCaretAt(0);
        }

        [Fact(
            DisplayName =
                "given no selection when c then the char at point is changed (meow-change-char fallback)")]
        public void NoSelectionCChangeChar()
        {
            Given("word", "a<caret>bc");
            WhenKeys("c");
            ThenText("ac");
            ThenMode(MeowMode.Insert);
        }

        [Fact(
            DisplayName =
                "given the caret on a newline when c then the lines join (change-char takes any char)")]
        public void CaretOnNewlineCJoins()
        {
            Given("two lines", "ab<caret>\ncd");
            WhenKeys("c");
            ThenText("abcd");
            ThenMode(MeowMode.Insert);
        }

        [Fact(DisplayName = "given the caret at end of buffer when c then nothing happens, not even INSERT")]
        public void CaretAtEndOfBufferCNothing()
        {
            Given("word", "ab<caret>");
            WhenKeys("c");
            ThenText("ab");
            ThenMode(MeowMode.Normal);
        }

        [Fact(DisplayName = "given U then undo runs only with an active region (undo-in-selection is gated)")]
        public void CapitalUUndoGatedByRegion()
        {
            Given("word", "<caret>hello");
            WhenKeys("U");
            Assert.Equal(0, Editor.UndoCount);
            WhenKeys("wU");
            Assert.Equal(1, Editor.UndoCount);
        }

        [Fact(DisplayName = "given a selection when d then it is deleted without touching the clipboard")]
        public void SelectionDDeletedNoClipboard()
        {
            Given("word", "<caret>hello world");
            GivenClipboard("KEEP");
            WhenKeys("wd");
            ThenText(" world");
            ThenClipboard("KEEP");
            ThenMode(MeowMode.Normal);
        }

        [Fact(
            DisplayName =
                "given no selection when d then the char at point is deleted (delete-char fallback)")]
        public void NoSelectionDDeleteChar()
        {
            Given("word", "a<caret>bc");
            WhenKeys("d");
            ThenText("ac");
        }

        [Fact(DisplayName = "given D then the char before point is deleted (meow-backward-delete)")]
        public void CapitalDBackwardDelete()
        {
            Given("word", "ab<caret>c");
            WhenKeys("D");
            ThenText("ac");
            ThenCaretAt(1);
        }

        [Fact(DisplayName = "given a selection when s then it is killed to the clipboard (meow-kill)")]
        public void SelectionSKillToClipboard()
        {
            Given("word", "<caret>hello world");
            WhenKeys("ws");
            ThenText(" world");
            ThenClipboard("hello");
        }

        [Fact(DisplayName = "given no selection when s then kill-line takes over (meow-C-k fallback)")]
        public void NoSelectionSKillLine()
        {
            Given("two lines", "he<caret>llo\nworld");
            WhenKeys("s");
            ThenText("he\nworld");
            ThenClipboard("llo");
        }

        [Fact(DisplayName = "given the caret at eol when s then the newline is killed (kill-line joins)")]
        public void CaretAtEolSKillsNewline()
        {
            Given("two lines", "he<caret>\nworld");
            WhenKeys("s");
            ThenText("heworld");
        }

        [Fact(
            DisplayName =
                "given a join selection when s then the lines join with a single space (fixup-whitespace)")]
        public void JoinSelectionSJoinsSingleSpace()
        {
            Given("indented continuation", "one\n  t<caret>wo");
            WhenKeys("ms");
            ThenText("one two");
            ThenCaretAt(3);
        }

        [Fact(DisplayName = "given a join before a closing bracket then no space is inserted")]
        public void JoinBeforeClosingBracketNoSpace()
        {
            Given("hanging paren", "f(x\n  <caret>)");
            WhenKeys("ms");
            ThenText("f(x)");
        }

        [Fact(
            DisplayName =
                "given y then the selection is copied and cancelled (kill-ring-save deactivates the mark)")]
        public void YCopiesAndCancels()
        {
            Given("word", "<caret>hello world");
            WhenKeys("wy");
            ThenText("hello world");
            ThenClipboard("hello");
            ThenNoSelection();
            ThenCaretAt(5);
        }

        [Fact(
            DisplayName =
                "given a line selection when y then the newline is copied and the caret lands past it")]
        public void LineSelectionYCopiesNewline()
        {
            Given("two lines", "o<caret>ne\ntwo");
            WhenKeys("xy");
            ThenText("one\ntwo");
            ThenClipboard("one\n");
            ThenNoSelection();
            ThenCaretAt(4);
        }

        [Fact(DisplayName = "given x x then y then both lines are copied with the trailing newline")]
        public void TwoLinesYCopiesTrailingNewline()
        {
            Given("three lines", "o<caret>ne\ntwo\nthree");
            WhenKeys("xxy");
            ThenText("one\ntwo\nthree");
            ThenClipboard("one\ntwo\n");
            ThenNoSelection();
            ThenCaretAt(8);
        }

        [Fact(DisplayName = "given a line selection when s then the whole line goes including its newline")]
        public void LineSelectionSWholeLineWithNewline()
        {
            Given("three lines", "o<caret>ne\ntwo\nthree");
            WhenKeys("xs");
            ThenText("two\nthree");
            ThenClipboard("one\n");
            ThenCaretAt(0);
        }

        [Fact(
            DisplayName =
                "given a reversed line selection when s then the newline stays (backward selections kill as-is)")]
        public void ReversedLineSelectionSNewlineStays()
        {
            Given("three lines", "one\nt<caret>wo\nthree");
            WhenKeys("x;s");
            ThenText("one\n\nthree");
            ThenClipboard("two");
        }

        [Fact(DisplayName = "given the last line when s then there is no newline to take")]
        public void LastLineSNoNewline()
        {
            Given("two lines", "one\nt<caret>wo");
            WhenKeys("xs");
            ThenText("one\n");
            ThenClipboard("two");
        }

        [Fact(
            DisplayName =
                "given p then the clipboard is inserted at point with the caret after it (meow-yank)")]
        public void PYanksClipboard()
        {
            Given("word", "<caret>hello");
            GivenClipboard("XY");
            WhenKeys("p");
            ThenText("XYhello");
            ThenCaretAt(2);
        }

        [Fact(
            DisplayName =
                "given r then the selection is replaced by the clipboard which stays intact (meow-replace)")]
        public void RReplacesWithClipboard()
        {
            Given("word", "<caret>hello world");
            GivenClipboard("XY");
            WhenKeys("wr");
            ThenText("XY world");
            ThenClipboard("XY");
            ThenNoSelection();
        }

        [Fact(DisplayName = "given r without a selection then nothing happens")]
        public void RWithoutSelectionNothing()
        {
            Given("word", "<caret>hello");
            GivenClipboard("XY");
            WhenKeys("r");
            ThenText("hello");
        }

        [Fact(DisplayName = "given u then the selection is cancelled first (meow-undo)")]
        public void UCancelsSelectionFirst()
        {
            Given("word", "<caret>hello world");
            WhenKeys("wu");
            ThenNoSelection();
        }

        [Fact(DisplayName = "given x x then repeated u past the undo stack then nothing blows up")]
        public void RepeatedUPastStackNoCrash()
        {
            Given("three lines", "<caret>one\ntwo\nthree");
            WhenKeys("xx");
            WhenKeys("uuuuuu");
            ThenText("one\ntwo\nthree");
            Assert.Equal(6, Editor.UndoCount);
        }

        [Fact(DisplayName = "given quote then the last command repeats")]
        public void QuoteRepeatsLastCommand()
        {
            Given("chars", "<caret>abcdef");
            WhenKeys("d");
            ThenText("bcdef");
            WhenKeys("'");
            ThenText("cdef");
        }

        [Fact(DisplayName = "given quote after a two-key command then the whole unit repeats")]
        public void QuoteAfterTwoKeyRepeatsWholeUnit()
        {
            Given("markers", "<caret>xaxaxa");
            WhenKeys("fa");
            ThenSelection("xa");
            WhenKeys("'");
            ThenSelection("xa");
            Assert.Equal(
                2,
                Math.Min(Editor.Sels[0].Anchor, Editor.Sels[0].Active));
        }

        [Fact(DisplayName = "given quote after finding a quote char then the find repeats")]
        public void QuoteAfterFindingQuoteRepeatsFind()
        {
            Given("quotes", "<caret>a'b'c");
            WhenKeys("f'");
            ThenSelection("a'");
            WhenKeys("'");
            ThenSelection("b'");
        }

        [Fact(
            DisplayName =
                "given a caret mid-word when upcase-word then the rest upcases and the caret moves to word end")]
        public void UpcaseWordMidWord()
        {
            Given("mixed-case word", "he<caret>LLo world");
            WhenCommand("upcase-word");
            ThenText("heLLO world");
            ThenCaretAt(5);
        }

        [Fact(DisplayName = "given a count when upcase-word then that many words upcase")]
        public void UpcaseWordCount()
        {
            Given("three words", "<caret>hello world foo");
            WhenKeys("2");
            WhenCommand("upcase-word");
            ThenText("HELLO WORLD foo");
            ThenCaretAt(11);
        }

        [Fact(
            DisplayName =
                "given a negative count when upcase-word then the previous word upcases and the caret stays")]
        public void UpcaseWordNegativeCount()
        {
            Given("two words", "hello <caret>world");
            WhenKeys("-");
            WhenCommand("upcase-word");
            ThenText("HELLO world");
            ThenCaretAt(6);
        }

        [Fact(DisplayName = "given a caret when downcase-word then the word downcases")]
        public void DowncaseWord()
        {
            Given("upper words", "<caret>HELLO WORLD");
            WhenCommand("downcase-word");
            ThenText("hello WORLD");
            ThenCaretAt(5);
        }

        [Fact(
            DisplayName =
                "given a caret mid-word when capitalize-word then the slice capitalizes as a fresh word")]
        public void CapitalizeWordMidWord()
        {
            Given("mixed-case word", "he<caret>LLo world");
            WhenCommand("capitalize-word");
            ThenText("heLlo world");
            ThenCaretAt(5);
        }

        [Fact(DisplayName = "given a count when capitalize-word then each word capitalizes")]
        public void CapitalizeWordCount()
        {
            Given("mixed words", "<caret>heLLO WOrld");
            WhenKeys("2");
            WhenCommand("capitalize-word");
            ThenText("Hello World");
            ThenCaretAt(11);
        }

        [Fact(
            DisplayName =
                "given a selection when upcase-word then it upcases from the caret and deactivates it")]
        public void UpcaseWordWithSelection()
        {
            Given("two words", "<caret>hello world");
            WhenKeys("w");
            ThenSelection("hello");
            WhenCommand("upcase-word");
            ThenText("hello WORLD");
            ThenNoSelection();
            ThenCaretAt(11);
        }

        [Fact(DisplayName = "given a caret when kill-word then the word kills to the clipboard")]
        public void KillWordToClipboard()
        {
            Given("two words", "<caret>hello world");
            WhenCommand("kill-word");
            ThenText(" world");
            ThenCaretAt(0);
            ThenClipboard("hello");
        }

        [Fact(DisplayName = "given a negative count when kill-word then the previous word kills backward")]
        public void KillWordNegativeCount()
        {
            Given("two words", "hello world<caret>");
            WhenKeys("-");
            WhenCommand("kill-word");
            ThenText("hello ");
            ThenCaretAt(6);
            ThenClipboard("world");
        }
    }
}
