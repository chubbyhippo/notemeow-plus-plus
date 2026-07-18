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
    public class RepeatSpec : SpecDsl
    {
        private const string NavRc =
            "map <leader>tn meow-next\n"
            + "repeat nav . meow-next\n"
            + "repeat nav , meow-prev";

        [Fact(DisplayName = "given repeat lines then named groups parse with their member targets")]
        public void RepeatLinesParseGroups()
        {
            Rc.Config c = Rc.Parse(
                new List<string>
                {
                    "repeat nav . meow-next",
                    "repeat nav , meow-prev",
                    "repeat zoom i <action>(IDM_VIEW_ZOOMIN)",
                });
            Assert.Equal("meow-next", c.Repeat["nav"]['.'].Command);
            Assert.Equal("meow-prev", c.Repeat["nav"][','].Command);
            Assert.Equal("IDM_VIEW_ZOOMIN", c.Repeat["zoom"]['i'].Action);
            Assert.Empty(c.Errors);
        }

        [Fact(DisplayName = "given a repeat line with a bad target then an error is collected")]
        public void BadTargetCollectsError()
        {
            Rc.Config c = Rc.Parse(
                new List<string> { "repeat nav . meow-frobnicate", "repeat nav" });
            Assert.Equal(2, c.Errors.Count);
            Assert.Contains("meow-frobnicate", c.Errors[0]);
        }

        [Fact(DisplayName =
            "given a repeat key that is not a single printable key then an error is collected")]
        public void BadRepeatKeyCollectsError()
        {
            Rc.Config c = Rc.Parse(
                new List<string> { "repeat nav ab meow-next", "repeat nav <Space> meow-next" });
            Assert.Equal(2, c.Errors.Count);
        }

        [Fact(DisplayName = "given home rc repeat lines then they layer per key over the bundled group")]
        public void HomeRcLayersOverBundledGroup()
        {
            GivenRc("repeat change , meow-prev\nrepeat change e <action>(IDM_FILE_SAVE)");
            Dictionary<char, Rc.Binding> g = Rc.RepeatGroups()["change"];
            Assert.Equal("IDM_SEARCH_CHANGED_NEXT", g['.'].Action);
            Assert.Equal("meow-prev", g[','].Command);
            Assert.Equal("IDM_FILE_SAVE", g['e'].Action);
        }

        [Fact(DisplayName = "given a repeat member bound to ignore then the key is given back")]
        public void IgnoreGivesMemberBack()
        {
            GivenRc("repeat zoom o ignore");
            Dictionary<char, Rc.Binding> g = Rc.RepeatGroups()["zoom"];
            Assert.False(g.ContainsKey('o'));
            Assert.Equal("IDM_VIEW_ZOOMIN", g['i'].Action);
        }

        [Fact(DisplayName = "the bundled default notemeowrc declares the init el repeat groups")]
        public void BundledRcDeclaresRepeatGroups()
        {
            Dictionary<string, Dictionary<char, Rc.Binding>> d = Rc.Defaults().Repeat;
            Assert.Equal("IDM_SEARCH_CHANGED_NEXT", d["change"]['.'].Action);
            Assert.Equal("IDM_SEARCH_CHANGED_PREV", d["change"][','].Action);
            Assert.True(
                new HashSet<char>(d["zoom"].Keys)
                    .SetEquals(new[] { 'i', '=', 'o', '-', 'u', '0' }));
            Assert.False(d.ContainsKey("error"));
            Assert.False(d.ContainsKey("expand"));
        }

        [Fact(DisplayName = "given the bundled rc then the tab repeat group cycles editor tabs")]
        public void BundledRcTabGroupCyclesTabs()
        {
            Dictionary<char, Rc.Binding> g = Rc.Defaults().Repeat["tab"];
            Assert.Equal("IDM_VIEW_TAB_NEXT", g['n'].Action);
            Assert.Equal("IDM_VIEW_TAB_PREV", g['p'].Action);
            Assert.Equal("IDM_VIEW_TAB_NEXT", g['.'].Action);
            Assert.Equal("IDM_VIEW_TAB_PREV", g[','].Action);
            Assert.True(
                new HashSet<char>(g.Keys).SetEquals(new[] { 'n', 'p', '.', ',' }));
        }

        [Fact(DisplayName = "given a repeat line edit then the reload button sees a change")]
        public void RepeatLineEditLightsReload()
        {
            Rc.SetUserLines(new List<string> { "nmap Z ,b" });
            Assert.False(
                RcFileState.EqualTo(new List<string> { "nmap Z ,b", "repeat nav . meow-next" }));
        }

        [Fact(DisplayName =
            "given a keypad nav entry in a repeat group then tapping the members keeps walking")]
        public void MemberTapsKeepWalking()
        {
            Given("four lines", "<caret>one\ntwo\nthree\nfour");
            GivenRc(NavRc);
            WhenKeys(" tn");
            ThenCaretLine(1);
            WhenKeys(".");
            ThenCaretLine(2);
            WhenKeys(".");
            ThenCaretLine(3);
            WhenKeys(",");
            ThenCaretLine(2);
            ThenMode(MeowMode.Normal);
        }

        [Fact(DisplayName = "given a normal key bound to a member target then it arms the same run")]
        public void TargetIdentityArmsRun()
        {
            Given("four lines", "<caret>one\ntwo\nthree\nfour");
            GivenRc(NavRc);
            WhenKeys("j");
            ThenCaretLine(1);
            WhenKeys(".");
            ThenCaretLine(2);
        }

        [Fact(DisplayName =
            "given a non-member key then the run ends and the key keeps its normal meaning")]
        public void NonMemberEndsRunAndFallsThrough()
        {
            Given("four lines", "<caret>one\ntwo\nthree\nfour");
            GivenRc(NavRc);
            WhenKeys(" tn");
            Assert.NotNull(Engine.RepeatMap);
            WhenKeys("w");
            ThenSelection("two");
            Assert.Null(Engine.RepeatMap);
        }

        [Fact(DisplayName = "given the run over then the member keys mean their normal commands again")]
        public void MemberKeysRestoredAfterRun()
        {
            Given("four lines", "<caret>one\ntwo\nthree\nfour");
            GivenRc(NavRc);
            WhenKeys(" tn");
            WhenKeys("x");
            ThenSelection("two");
            WhenKeys(".");
            Assert.Equal(Pending.Bounds, St.Pending);
            ThenCaretLine(1);
        }

        [Fact(DisplayName = "given escape then the run ends")]
        public void EscapeEndsRun()
        {
            Given("four lines", "<caret>one\ntwo\nthree\nfour");
            GivenRc(NavRc);
            WhenKeys(" tn");
            Assert.NotNull(Engine.RepeatMap);
            PressEsc();
            Assert.Null(Engine.RepeatMap);
            WhenKeys(".");
            Assert.Equal(Pending.Bounds, St.Pending);
            ThenCaretLine(1);
        }

        [Fact(DisplayName = "given SPC during a run then the keypad still opens")]
        public void KeypadOpensDuringRun()
        {
            Given("four lines", "<caret>one\ntwo\nthree\nfour");
            GivenRc(NavRc);
            WhenKeys(" tn");
            WhenKeys(" tn");
            ThenCaretLine(2);
            ThenMode(MeowMode.Normal);
        }

        [Fact(DisplayName = "given a digit during a run then it falls through as a count")]
        public void DigitFallsThroughAsCount()
        {
            Given("four lines", "<caret>one\ntwo\nthree\nfour");
            GivenRc(NavRc);
            WhenKeys(" tn");
            ThenCaretLine(1);
            WhenKeys("2j");
            ThenCaretLine(3);
        }

        [Fact(DisplayName = "given a run then a member tap continues after an editor switch")]
        public void MemberTapContinuesAfterEditorSwitch()
        {
            Given("four lines", "<caret>one\ntwo\nthree\nfour");
            GivenRc(NavRc);
            WhenKeys(" tn");
            ThenCaretLine(1);
            St = new MeowState();
            WhenKeys(".");
            ThenCaretLine(2);
        }

        [Fact(DisplayName = "given a run then the armed keys are the group members")]
        public void ArmedKeysAreGroupMembers()
        {
            Given("four lines", "<caret>one\ntwo\nthree\nfour");
            GivenRc(NavRc);
            WhenKeys(" tn");
            Assert.NotNull(Engine.RepeatMap);
            Assert.True(new HashSet<char>(Engine.RepeatMap.Keys).SetEquals(new[] { '.', ',' }));
            WhenKeys("w");
            Assert.Null(Engine.RepeatMap);
        }

        [Fact(DisplayName =
            "given the bundled rc then SPC x z repeats the last command and bare z keeps repeating like Emacs C-x z")]
        public void SpcXzRepeatsAndBareZContinues()
        {
            Given("delete run", "<caret>aaaaa");
            WhenKeys("d");
            ThenText("aaaa");
            WhenKeys(" xz");
            ThenText("aaa");
            WhenKeys("z");
            ThenText("aa");
            WhenKeys("z");
            ThenText("a");
            ThenMode(MeowMode.Normal);
        }
    }
}
