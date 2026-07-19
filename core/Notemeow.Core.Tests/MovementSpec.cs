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
    public class MovementSpec : SpecDsl
    {
        [Fact(DisplayName = "given a caret when l then it moves right without selecting")]
        public void LMovesRight()
        {
            Given("plain text", "<caret>hello");
            WhenKeys("l");
            ThenCaretAt(1);
            ThenNoSelection();
        }

        [Fact(DisplayName = "given a caret when h then it moves left")]
        public void HMovesLeft()
        {
            Given("plain text", "he<caret>llo");
            WhenKeys("h");
            ThenCaretAt(1);
            ThenNoSelection();
        }

        [Fact(DisplayName = "given two lines when j then caret moves to next line")]
        public void JMovesToNextLine()
        {
            Given("two lines", "<caret>one\ntwo");
            WhenKeys("j");
            ThenCaretLine(1);
        }

        [Fact(DisplayName = "given a count when 2 l then caret moves two chars (digit argument)")]
        public void CountMovesTwoChars()
        {
            Given("plain text", "<caret>hello");
            WhenKeys("2l");
            ThenCaretAt(2);
            ThenNoSelection();
        }

        [Fact(DisplayName = "given four lines when 3 j then caret moves three lines down")]
        public void CountMovesThreeLinesDown()
        {
            Given("four lines", "<caret>a\nb\nc\nd");
            WhenKeys("3j");
            ThenCaretLine(3);
        }

        [Fact(DisplayName = "given negative argument when - 2 j then caret moves two lines up")]
        public void NegativeArgMovesUp()
        {
            Given("four lines", "a\nb\nc\n<caret>d");
            WhenKeys("-2j");
            ThenCaretLine(1);
        }

        [Fact(DisplayName = "given no selection when H then a char selection is created leftwards")]
        public void ShiftHCreatesCharSelection()
        {
            Given("plain text", "hel<caret>lo");
            WhenKeys("H");
            ThenSelection("l");
            ThenSelType(SelType.Char);
            ThenCaretAtSelectionStart();
        }

        [Fact(
            DisplayName =
                "given a char selection when h then the selection survives and extends (meow keeps char selections)")]
        public void HExtendsCharSelection()
        {
            Given("plain text", "hel<caret>lo");
            WhenKeys("Hh");
            ThenSelection("el");
            ThenSelType(SelType.Char);
        }

        [Fact(
            DisplayName =
                "given a word selection when h then the selection is cancelled (only char selections survive)")]
        public void HCancelsWordSelection()
        {
            Given("plain text", "<caret>hello world");
            WhenKeys("w");
            ThenSelection("hello");
            WhenKeys("h");
            ThenNoSelection();
        }

        [Fact(DisplayName = "given L J then char selection extends right and down")]
        public void ShiftLJExtends()
        {
            Given("two lines", "<caret>ab\ncd");
            WhenKeys("LJ");
            ThenSelType(SelType.Char);
            Assert.NotNull(SelectedText());
            ThenCaretAtSelectionEnd();
        }

        [Fact(DisplayName = "given an undefined key in NORMAL then it is swallowed and types nothing")]
        public void UndefinedKeySwallowed()
        {
            Given("plain text", "<caret>hello");
            WhenKeys("#%");
            ThenText("hello");
        }

        [Fact(DisplayName = "given the caret at bol when h then it crosses to the previous line end")]
        public void HCrossesToPreviousLineEnd()
        {
            Given("two lines", "abc\n<caret>def");
            WhenKeys("h");
            ThenCaretAt(3);
            ThenNoSelection();
        }

        [Fact(DisplayName = "given the caret at eol when l then it crosses to the next line start")]
        public void LCrossesToNextLineStart()
        {
            Given("two lines", "abc<caret>\ndef");
            WhenKeys("l");
            ThenCaretAt(4);
        }

        [Fact(DisplayName = "given j j through a short line then the goal column is kept")]
        public void GoalColumnKeptThroughShortLine()
        {
            Given("short middle line", "abcd<caret>ef\nxy\nlmnopq");
            WhenKeys("j");
            ThenCaretAt(9);
            WhenKeys("j");
            ThenCaretAt(14);
        }

        [Fact(DisplayName = "given j on the last line then the caret moves to the end of buffer")]
        public void JOnLastLineGoesToBufferEnd()
        {
            Given("two lines", "ab\nc<caret>def");
            WhenKeys("j");
            ThenCaretAt(7);
        }

        [Fact(DisplayName = "given k on the first line then the caret moves to the beginning of buffer")]
        public void KOnFirstLineGoesToBufferStart()
        {
            Given("two lines", "a<caret>bc\ndef");
            WhenKeys("k");
            ThenCaretAt(0);
        }

        [Fact(DisplayName = "given a CRLF document then the goal column clamps before the carriage return")]
        public void CrlfGoalColumnClampsBeforeCarriageReturn()
        {
            Given("crlf long short long", "abc<caret>d\r\nx\r\nefgh");
            WhenKeys("j");
            ThenCaretAt(7);
            WhenKeys("j");
            ThenCaretAt(12);
        }
    }
}
