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
    public class EmacsMotionSpec : SpecDsl
    {
        [Fact(
            DisplayName =
                "given no selection when forward-char then the caret moves right without selecting")]
        public void ForwardCharMovesRight()
        {
            Given("plain text", "<caret>hello");
            WhenCommand("forward-char");
            ThenCaretAt(1);
            ThenNoSelection();
        }

        [Fact(
            DisplayName =
                "given no selection when backward-char then the caret moves left without selecting")]
        public void BackwardCharMovesLeft()
        {
            Given("plain text", "he<caret>llo");
            WhenCommand("backward-char");
            ThenCaretAt(1);
            ThenNoSelection();
        }

        [Fact(
            DisplayName =
                "given no selection when next-line then the caret moves down without selecting")]
        public void NextLineMovesDown()
        {
            Given("two lines", "<caret>one\ntwo");
            WhenCommand("next-line");
            Assert.Equal(1, CaretLine());
            ThenNoSelection();
        }

        [Fact(
            DisplayName =
                "given no selection when previous-line then the caret moves up without selecting")]
        public void PreviousLineMovesUp()
        {
            Given("two lines", "one\nt<caret>wo");
            WhenCommand("previous-line");
            Assert.Equal(0, CaretLine());
            ThenNoSelection();
        }

        [Fact(
            DisplayName =
                "given no selection when move-beginning-of-line then the caret goes to column zero")]
        public void BeginningOfLine()
        {
            Given("indented line", "hel<caret>lo world");
            WhenCommand("move-beginning-of-line");
            ThenCaretAt(0);
            ThenNoSelection();
        }

        [Fact(DisplayName = "given no selection when move-end-of-line then the caret goes to eol")]
        public void EndOfLine()
        {
            Given("plain text", "he<caret>llo");
            WhenCommand("move-end-of-line");
            ThenCaretAt(5);
            ThenNoSelection();
        }

        [Fact(
            DisplayName =
                "given no selection when forward-word then the caret lands at the end of the next word")]
        public void ForwardWord()
        {
            Given("comma separated", "<caret>word1, word2");
            WhenCommand("forward-word");
            ThenCaretAt(5);
            ThenNoSelection();
        }

        [Fact(
            DisplayName =
                "given no selection when backward-word then the caret lands at the start of the word")]
        public void BackwardWord()
        {
            Given("two words", "hello world<caret>");
            WhenCommand("backward-word");
            ThenCaretAt(6);
            ThenNoSelection();
        }

        [Fact(
            DisplayName =
                "given no selection when forward-sentence then the caret lands past the sentence")]
        public void ForwardSentence()
        {
            Given("three sentences", "<caret>One. Two. Three.");
            WhenCommand("forward-sentence");
            ThenCaretAt(5);
            ThenNoSelection();
        }

        [Fact(
            DisplayName =
                "given no selection when backward-sentence then the caret lands at the sentence start")]
        public void BackwardSentence()
        {
            Given("three sentences", "One. Two. Thr<caret>ee.");
            WhenCommand("backward-sentence");
            ThenCaretAt(10);
            ThenNoSelection();
        }

        [Fact(DisplayName = "given w then forward-char extends the selection one char forward")]
        public void WThenForwardCharExtends()
        {
            Given("two words", "<caret>hello world");
            WhenKeys("w");
            ThenSelection("hello");
            WhenCommand("forward-char");
            ThenSelection("hello ");
            ThenSelType(SelType.Char);
            ThenCaretAtSelectionEnd();
        }

        [Fact(DisplayName = "given w then backward-char shrinks the selection from its end")]
        public void WThenBackwardCharShrinks()
        {
            Given("two words", "<caret>hello world");
            WhenKeys("w");
            ThenSelection("hello");
            WhenCommand("backward-char");
            ThenSelection("hell");
        }

        [Fact(DisplayName = "given w then next-line extends the selection down")]
        public void WThenNextLineExtends()
        {
            Given("word then a second line", "<caret>hello\nworld");
            WhenKeys("w");
            ThenSelection("hello");
            WhenCommand("next-line");
            ThenSelection("hello\nworld");
        }

        [Fact(DisplayName = "given w then move-end-of-line extends the selection to eol")]
        public void WThenEndOfLineExtends()
        {
            Given("three words", "<caret>hello brave world");
            WhenKeys("w");
            ThenSelection("hello");
            WhenCommand("move-end-of-line");
            ThenSelection("hello brave world");
        }

        [Fact(
            DisplayName =
                "given caret mid-line when w then move-beginning-of-line extends the selection to bol")]
        public void WThenBeginningOfLineExtends()
        {
            Given("three words", "hello <caret>brave world");
            WhenKeys("w");
            ThenSelection("brave");
            WhenCommand("move-beginning-of-line");
            ThenSelection("hello ");
            ThenCaretAtSelectionStart();
        }

        [Fact(DisplayName = "given w then forward-word extends the word selection (chains with e)")]
        public void WThenForwardWordExtends()
        {
            Given("three words", "<caret>one two three");
            WhenKeys("w");
            ThenSelection("one");
            WhenCommand("forward-word");
            ThenSelection("one two");
            ThenSelType(SelType.Word);
            WhenKeys("e");
            ThenSelection("one two three");
        }

        [Fact(
            DisplayName =
                "given w then forward-sentence extends the selection through the next sentence")]
        public void WThenForwardSentenceExtends()
        {
            Given("two sentences", "<caret>One. Two.");
            WhenKeys("w");
            ThenSelection("One");
            WhenCommand("forward-sentence");
            ThenSelection("One. ");
            WhenCommand("forward-sentence");
            ThenSelection("One. Two.");
        }

        [Fact(
            DisplayName =
                "given w then semicolon then forward-char shrinks from the start (reversed anchor)")]
        public void WThenReverseThenForwardCharShrinks()
        {
            Given("two words", "<caret>hello world");
            WhenKeys("w");
            ThenSelection("hello");
            ThenCaretAtSelectionEnd();
            WhenKeys(";");
            ThenCaretAtSelectionStart();
            WhenCommand("forward-char");
            ThenSelection("ello");
            ThenCaretAtSelectionStart();
        }

        [Fact(DisplayName = "given w then semicolon then backward-char extends past the start")]
        public void WThenReverseThenBackwardCharExtends()
        {
            Given("leading padding then two words", " <caret>hello world");
            WhenKeys("w");
            ThenSelection("hello");
            WhenKeys(";");
            ThenCaretAtSelectionStart();
            WhenCommand("backward-char");
            ThenSelection(" hello");
            ThenCaretAtSelectionStart();
        }

        [Fact(DisplayName = "given a reversed line selection then previous-line extends further up")]
        public void ReversedLineThenPreviousLineExtends()
        {
            Given("three lines", "one\ntwo\nth<caret>ree");
            WhenKeys("x");
            WhenKeys(";");
            ThenCaretAtSelectionStart();
            WhenCommand("previous-line");
            ThenSelection("two\nthree");
        }

        [Fact(
            DisplayName =
                "given beacon cursors when forward-char then every cursor extends its own selection")]
        public void BeaconForwardCharExtendsEach()
        {
            Given("repeats with identical trailing context", "<caret>foo. foo. foo.");
            WhenKeys(",bG");
            GivenCaretAt(0);
            WhenKeys("w");
            ThenCaretCount(3);
            WhenCommand("forward-char");
            var actives = new List<int>();
            foreach (SelRange s in Editor.Sels) actives.Add(s.Active);
            actives.Sort();
            Assert.Equal(new List<int> { 4, 9, 14 }, actives);
        }

        [Fact(DisplayName = "given no selection when beginning-of-buffer then the caret goes to point-min")]
        public void BeginningOfBufferGoesToPointMin()
        {
            Given("two lines", "one\nt<caret>wo");
            WhenCommand("beginning-of-buffer");
            ThenCaretAt(0);
            ThenNoSelection();
        }

        [Fact(DisplayName = "given no selection when end-of-buffer then the caret goes to point-max")]
        public void EndOfBufferGoesToPointMax()
        {
            Given("two lines", "on<caret>e\ntwo");
            WhenCommand("end-of-buffer");
            ThenCaretAt(7);
            ThenNoSelection();
        }

        [Fact(DisplayName = "given w then end-of-buffer extends the selection to point-max")]
        public void EndOfBufferExtendsSelection()
        {
            Given("two words", "<caret>hello world");
            WhenKeys("w");
            ThenSelection("hello");
            WhenCommand("end-of-buffer");
            ThenSelection("hello world");
            ThenCaretAtSelectionEnd();
        }

        [Fact(DisplayName = "given w then beginning-of-buffer extends the selection back to point-min")]
        public void BeginningOfBufferExtendsSelectionBack()
        {
            Given("prefixed word", "ab <caret>hello");
            WhenKeys("w");
            ThenSelection("hello");
            WhenCommand("beginning-of-buffer");
            ThenSelection("ab ");
            ThenCaretAtSelectionStart();
        }

        [Fact(
            DisplayName =
                "given a count when beginning-of-buffer then the caret lands at the next line start past that tenth")]
        public void CountedBeginningOfBufferLandsTenthIn()
        {
            Given(
                "five ten-char lines",
                "<caret>0123456789\n0123456789\n0123456789\n0123456789\n0123456789");
            WhenKeys("3");
            WhenCommand("beginning-of-buffer");
            ThenCaretAt(22);
            ThenNoSelection();
        }

        [Fact(
            DisplayName =
                "given a count when end-of-buffer then the caret lands a tenth back at the next line start")]
        public void CountedEndOfBufferLandsTenthBack()
        {
            Given(
                "five ten-char lines",
                "<caret>0123456789\n0123456789\n0123456789\n0123456789\n0123456789");
            WhenKeys("3");
            WhenCommand("end-of-buffer");
            ThenCaretAt(44);
            ThenNoSelection();
        }

        [Fact(DisplayName =
            "given a count landing on a line boundary when beginning-of-buffer then the caret lands one line past that tenth")]
        public void CountedBeginningOfBufferLandsPastLineBoundary()
        {
            Given("three two-char lines", "<caret>aa\naa\naa\n");
            WhenKeys("3");
            WhenCommand("beginning-of-buffer");
            ThenCaretAt(3);
            ThenNoSelection();
        }

        [Fact(DisplayName =
            "given a long-short-long buffer then repeated next-line keeps the goal column across the short line")]
        public void RepeatedNextLineKeepsGoalColumnAcrossShortLine()
        {
            Given("long short long", "01234567<caret>89\nab\n0123456789");
            WhenCommand("next-line");
            WhenCommand("next-line");
            ThenCaretAt(22);
            ThenNoSelection();
        }

        [Fact(DisplayName =
            "given no selection when forward-paragraph then the caret lands on the separator blank line")]
        public void ForwardParagraphLandsOnSeparatorBlankLine()
        {
            Given("two paragraphs", "a<caret>aa\nbbb\n\nccc");
            WhenCommand("forward-paragraph");
            ThenCaretAt(8);
            ThenNoSelection();
        }

        [Fact(DisplayName =
            "given no selection when backward-paragraph then the caret lands on the empty line joining the paragraph start")]
        public void BackwardParagraphLandsOnEmptyLineJoiningStart()
        {
            Given("two paragraphs", "aaa\n\nbb<caret>b");
            WhenCommand("backward-paragraph");
            ThenCaretAt(4);
            ThenNoSelection();
        }

        [Fact(DisplayName =
            "given a caret on a blank line when forward-paragraph then it crosses to the next paragraph end")]
        public void ForwardParagraphFromBlankLineCrossesToNextEnd()
        {
            Given("blank line between paragraphs", "aaa\n<caret>\nbbb\n\nccc");
            WhenCommand("forward-paragraph");
            ThenCaretAt(9);
            ThenNoSelection();
        }

        [Fact(DisplayName =
            "given a caret on a blank line when backward-paragraph then it lands at the previous paragraph start")]
        public void BackwardParagraphFromBlankLineLandsAtPreviousStart()
        {
            Given("blank line after two-line paragraph", "aaa\nbbb\n<caret>\nccc");
            WhenCommand("backward-paragraph");
            ThenCaretAt(0);
            ThenNoSelection();
        }

        [Fact(DisplayName =
            "given a whitespace-only separator when backward-paragraph then the caret stops at the paragraph text start")]
        public void BackwardParagraphStopsAtTextStartAfterWhitespaceSeparator()
        {
            Given("space-only separator line", "aaa\n \nbb<caret>b");
            WhenCommand("backward-paragraph");
            ThenCaretAt(6);
            ThenNoSelection();
        }

        [Fact(DisplayName =
            "given consecutive empty lines when backward-paragraph then only the adjacent one joins the paragraph start")]
        public void BackwardParagraphJoinsOnlyAdjacentEmptyLine()
        {
            Given("two empty separator lines", "aaa\n\n\nbb<caret>b");
            WhenCommand("backward-paragraph");
            ThenCaretAt(5);
            ThenNoSelection();
        }

        [Fact(DisplayName =
            "given a count when forward-paragraph then the caret walks that many paragraph ends")]
        public void CountedForwardParagraphWalksParagraphEnds()
        {
            Given("three paragraphs", "a<caret>aa\n\nbbb\n\nccc");
            WhenKeys("2");
            WhenCommand("forward-paragraph");
            ThenCaretAt(9);
            ThenNoSelection();
        }

        [Fact(DisplayName =
            "given the last paragraph when forward-paragraph then the caret goes to point-max")]
        public void ForwardParagraphAtLastParagraphGoesToPointMax()
        {
            Given("two paragraphs", "aaa\n\nbb<caret>b");
            WhenCommand("forward-paragraph");
            ThenCaretAt(8);
            ThenNoSelection();
        }

        [Fact(DisplayName =
            "given w then forward-paragraph extends the selection through the paragraph end")]
        public void ForwardParagraphExtendsSelectionThroughEnd()
        {
            Given("paragraph then another", "<caret>hello world\n\nnext");
            WhenKeys("w");
            ThenSelection("hello");
            WhenCommand("forward-paragraph");
            ThenSelection("hello world\n");
            ThenSelType(SelType.Char);
            ThenCaretAtSelectionEnd();
        }

        [Fact(DisplayName =
            "given w then backward-paragraph extends the selection back past the paragraph start")]
        public void BackwardParagraphExtendsSelectionBackPastStart()
        {
            Given("paragraph after a blank line", "aaa\n\nhello wo<caret>rld");
            WhenKeys("w");
            ThenSelection("world");
            WhenCommand("backward-paragraph");
            ThenSelection("\nhello ");
            ThenCaretAtSelectionStart();
        }
    }
}
