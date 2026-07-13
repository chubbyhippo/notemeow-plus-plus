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
    public class GrabBeaconSpec : SpecDsl
    {
        [Fact(DisplayName =
            "given a selection when G then it becomes the grab and the selection is cancelled")]
        public void SelectionGBecomesGrab()
        {
            Given("word", "<caret>hello world");
            WhenKeys("wG");
            ThenNoSelection();
            Assert.NotNull(St.Grab);
            Assert.Equal(0, St.Grab.Start);
            Assert.Equal(5, St.Grab.End);
        }

        [Fact(DisplayName =
            "given a grab and a selection elsewhere when R then the two texts swap (meow-swap-grab)")]
        public void SwapGrabSwapsTexts()
        {
            Given("three words", "<caret>one two three");
            WhenKeys("wG");
            GivenCaretAt(8);
            WhenKeys("w");
            ThenSelection("three");
            WhenKeys("R");
            ThenText("three two one");
            ThenNoSelection();
            Assert.Equal(
                "three", Editor.GetText().Substring(St.Grab.Start, St.Grab.End - St.Grab.Start));
        }

        [Fact(DisplayName =
            "given no selection when G then an existing grab is cancelled (meow 1.5.0 body)")]
        public void NoSelectionGCancelsGrab()
        {
            Given("word", "<caret>hello world");
            WhenKeys("wG");
            Assert.NotNull(St.Grab);
            WhenKeys("G");
            Assert.Null(St.Grab);
        }

        [Fact(DisplayName = "given no grab when R then nothing changes")]
        public void NoGrabRNothing()
        {
            Given("word", "<caret>hello");
            WhenKeys("wR");
            ThenText("hello");
            ThenSelection("hello");
        }

        [Fact(DisplayName = "given a selection overlapping the grab when R then the swap is refused")]
        public void OverlappingRRefused()
        {
            Given("three words", "<caret>one two three");
            WhenKeys("weG");
            GivenCaretAt(4);
            WhenKeys("fr");
            WhenKeys("R");
            ThenText("one two three");
        }

        [Fact(DisplayName =
            "given Y then the grab is re-synced to the current selection (meow-sync-grab)")]
        public void SyncGrabResyncs()
        {
            Given("two words", "<caret>hello world");
            WhenKeys("wG");
            GivenCaretAt(6);
            WhenKeys("wY");
            ThenNoSelection();
            Assert.Equal(6, St.Grab.Start);
            Assert.Equal(11, St.Grab.End);
        }

        [Fact(DisplayName =
            "given a grab when marking a word inside it then a cursor lands on every occurrence (BEACON)")]
        public void BeaconWordOccurrences()
        {
            Given("repeats", "<caret>foo bar foo baz foo");
            WhenKeys(",bG");
            GivenCaretAt(0);
            WhenKeys("w");
            ThenCaretCount(3);
        }

        [Fact(DisplayName = "given beacon cursors when c then all occurrences change together")]
        public void BeaconChangeTogether()
        {
            Given("repeats", "<caret>foo bar foo baz foo");
            WhenKeys(",bG");
            GivenCaretAt(0);
            WhenKeys("wc");
            ThenText(" bar  baz ");
            ThenMode(MeowMode.Insert);
            ThenCaretCount(3);
        }

        [Fact(DisplayName = "given beacon cursors when c then every cursor lands at its own edit")]
        public void BeaconChangeCursorOffsets()
        {
            Given("repeats", "<caret>foo bar foo baz foo");
            WhenKeys(",bG");
            GivenCaretAt(0);
            WhenKeys("wc");
            ThenText(" bar  baz ");
            Assert.Equal(
                new List<SelRange>
                {
                    new SelRange(0, 0),
                    new SelRange(5, 5),
                    new SelRange(10, 10),
                },
                Editor.Sels);
        }

        [Fact(DisplayName =
            "given a grab when x inside it then a cursor lands on every line (line beacon)")]
        public void BeaconLineOccurrences()
        {
            Given("three lines", "<caret>a\nb\nc");
            WhenKeys(",bG");
            GivenCaretAt(0);
            WhenKeys("x");
            ThenCaretCount(3);
        }

        [Fact(DisplayName = "given beacon cursors when g then they collapse to one")]
        public void BeaconCollapseOnG()
        {
            Given("repeats", "<caret>foo bar foo baz foo");
            WhenKeys(",bG");
            GivenCaretAt(0);
            WhenKeys("w");
            ThenCaretCount(3);
            WhenKeys("g");
            ThenCaretCount(1);
            ThenNoSelection();
        }

        [Fact(DisplayName = "given a selection outside the grab then no beacon cursors appear")]
        public void NoBeaconOutsideGrab()
        {
            Given("repeats", "<caret>foo bar foo");
            WhenKeys("wG");
            GivenCaretAt(8);
            WhenKeys("w");
            ThenCaretCount(1);
        }
    }
}
