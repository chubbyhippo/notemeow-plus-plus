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
    public class ModesKeypadSpec : SpecDsl
    {
        [Fact(DisplayName = "given INSERT when escape then back to NORMAL")]
        public void EscapeExitsInsert()
        {
            Given("word", "<caret>hello");
            WhenKeys("i");
            ThenMode(MeowMode.Insert);
            PressEsc();
            ThenMode(MeowMode.Normal);
        }

        [Fact(DisplayName = "given beacon cursors in NORMAL when escape then they collapse")]
        public void EscapeCollapsesBeaconCursors()
        {
            Given("repeats", "<caret>foo bar foo");
            WhenKeys(",bG");
            GivenCaretAt(0);
            WhenKeys("w");
            ThenCaretCount(2);
            PressEsc();
            ThenCaretCount(1);
            ThenMode(MeowMode.Normal);
        }

        [Fact(DisplayName = "given a pending find when escape then the pending key is dropped")]
        public void EscapeDropsPendingFind()
        {
            Given("word", "<caret>hello");
            WhenKeys("f");
            Assert.NotNull(St.Pending);
            PressEsc();
            Assert.Null(St.Pending);
            WhenKeys("l");
            ThenCaretAt(1);
        }

        [Fact(DisplayName = "given nothing meow-related when escape then it reports unhandled")]
        public void EscapeReportsUnhandled()
        {
            Given("word", "<caret>hello");
            Assert.False(PressEsc(), "the host may fall through to its own escape");
        }

        [Fact(DisplayName =
            "given a read-only document then all motions work and the modify commands are inert")]
        public void ReadOnlyGatesModifyCommands()
        {
            Given("two lines", "<caret>one\ntwo");
            GivenReadOnly();
            WhenKeys("j");
            Assert.Equal(1, CaretLine());
            WhenKeys("kw");
            ThenSelection("one");
            WhenKeys("s");
            ThenText("one\ntwo");
            ThenSelection("one");
            WhenKeys("y");
            ThenClipboard("one");
            WhenKeys("d");
            WhenKeys("p");
            ThenText("one\ntwo");
            ThenMode(MeowMode.Normal);
        }

        [Fact(DisplayName =
            "given INSERT when the keypad action fires then a keypad command returns to INSERT")]
        public void KeypadActionReturnsToInsert()
        {
            Given("word", "ab<caret>cd");
            GivenRc("map <leader>zz meow-left");
            WhenKeys("i");
            ThenMode(MeowMode.Insert);
            Engine.EnterKeypad(Ctx());
            ThenMode(MeowMode.Keypad);
            WhenKeys("zz");
            ThenMode(MeowMode.Insert);
            ThenCaretAt(1);
        }

        [Fact(DisplayName = "given INSERT when the keypad action then escape then back to INSERT")]
        public void KeypadActionEscapeBackToInsert()
        {
            Given("word", "<caret>hello");
            WhenKeys("i");
            Engine.EnterKeypad(Ctx());
            ThenMode(MeowMode.Keypad);
            PressEsc();
            ThenMode(MeowMode.Insert);
        }

        [Fact(DisplayName =
            "given NORMAL when the keypad action fires then KEYPAD round-trips to NORMAL")]
        public void KeypadActionRoundTripsToNormal()
        {
            Given("word", "<caret>hello");
            Engine.EnterKeypad(Ctx());
            ThenMode(MeowMode.Keypad);
            PressEsc();
            ThenMode(MeowMode.Normal);
        }

        [Fact(DisplayName =
            "given SPC then KEYPAD opens and a digit becomes the count for the next command")]
        public void KeypadDigitBecomesCount()
        {
            Given("four lines", "<caret>a\nb\nc\nd");
            WhenKeys(" ");
            ThenMode(MeowMode.Keypad);
            WhenKeys("3");
            ThenMode(MeowMode.Normal);
            WhenKeys("j");
            Assert.Equal(3, CaretLine());
        }

        [Fact(DisplayName = "given SPC x then the keypad keeps collecting the prefix")]
        public void KeypadCollectsPrefix()
        {
            Given("word", "<caret>hello");
            WhenKeys(" x");
            ThenMode(MeowMode.Keypad);
            Assert.Equal("x", St.Keypad.ToString());
        }

        [Fact(DisplayName = "given an undefined keypad sequence then KEYPAD exits back to NORMAL")]
        public void UndefinedKeypadSequenceExits()
        {
            Given("word", "<caret>hello");
            WhenKeys(" x~");
            ThenMode(MeowMode.Normal);
            ThenText("hello");
        }

        [Fact(DisplayName = "given KEYPAD when escape then back to NORMAL without dispatch")]
        public void EscapeCancelsKeypad()
        {
            Given("word", "<caret>hello");
            WhenKeys(" x");
            PressEsc();
            ThenMode(MeowMode.Normal);
            ThenText("hello");
        }

        [Fact(DisplayName = "given a keypad action entry then the host command runs")]
        public void KeypadActionEntryRunsHostCommand()
        {
            Given("word", "<caret>hello");
            WhenKeys(" xs");
            ThenMode(MeowMode.Normal);
            Assert.Equal(new List<string> { "IDM_FILE_SAVE" }, Ui.Ran);
        }

        [Fact(DisplayName =
            "given INSERT then the adapter is told to swap the cursor, and back on escape")]
        public void InsertSwapsCursorAndBack()
        {
            Given("word", "<caret>hello");
            WhenKeys("i");
            Assert.Equal(new List<MeowMode> { MeowMode.Insert }, Ui.Modes);
            PressEsc();
            Assert.Equal(new List<MeowMode> { MeowMode.Insert, MeowMode.Normal }, Ui.Modes);
        }

        [Fact(DisplayName =
            "given the bundled defaults then SPC m exposes the M- motion and edit layer")]
        public void BundledDefaultsExposeMetaLayerOnSpcM()
        {
            var keypad = Rc.Keypad();
            Assert.Equal("backward-sentence", keypad["ma"].Command);
            Assert.Equal("backward-word", keypad["mb"].Command);
            Assert.Equal("capitalize-word", keypad["mc"].Command);
            Assert.Equal("kill-word", keypad["md"].Command);
            Assert.Equal("forward-sentence", keypad["me"].Command);
            Assert.Equal("forward-word", keypad["mf"].Command);
            Assert.Equal("downcase-word", keypad["ml"].Command);
            Assert.Equal("upcase-word", keypad["mu"].Command);
            Assert.Equal("beginning-of-buffer", keypad["m<"].Command);
            Assert.Equal("end-of-buffer", keypad["m>"].Command);
            Assert.Equal("backward-paragraph", keypad["m{"].Command);
            Assert.Equal("forward-paragraph", keypad["m}"].Command);
        }

        [Fact(DisplayName =
            "given the SPC m keypad then a meta word motion runs and returns to NORMAL")]
        public void SpcMMetaWordMotionRunsAndReturnsToNormal()
        {
            Given("two words", "<caret>hello world");
            WhenKeys(" mf");
            Assert.True(Editor.Sels[0].Active > 0, "caret advanced");
            ThenMode(MeowMode.Normal);
        }
    }
}
