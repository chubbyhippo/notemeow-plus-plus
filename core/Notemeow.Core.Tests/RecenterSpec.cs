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
    public class RecenterSpec : SpecDsl
    {
        private const string Buffer = "one\ntwo\nthree<caret>\nfour\nfive\n";

        [Fact(DisplayName =
            "given the recenter cycle then the positions follow Emacs recenter-positions")]
        public void RecenterCycleFollowsEmacs()
        {
            Assert.Equal(
                [RevealAt.Center, RevealAt.Top, RevealAt.Bottom, RevealAt.Center],
                new[]
                {
                    View.RecenterPosition(0),
                    View.RecenterPosition(1),
                    View.RecenterPosition(2),
                    View.RecenterPosition(3),
                });
        }

        [Fact(DisplayName = "given a different previous command then the recenter cycle starts over")]
        public void DifferentPreviousCommandRestarts()
        {
            Assert.Equal(1, View.NextRecenterPhase(View.RecenterCommand, 0));
            Assert.Equal(3, View.NextRecenterPhase(View.RecenterCommand, 2));
            Assert.Equal(0, View.NextRecenterPhase("meow-left", 2));
            Assert.Equal(0, View.NextRecenterPhase(null, 2));
        }

        [Fact(DisplayName = "given repeated C-l then the view cycles center top bottom like Emacs")]
        public void RepeatedRecenterCycles()
        {
            Given("a caret mid-buffer", Buffer);
            for (int i = 0; i < 4; i++) WhenCommand(View.RecenterCommand);
            Assert.Equal(
                new[] { RevealAt.Center, RevealAt.Top, RevealAt.Bottom, RevealAt.Center },
                Ui.Revealed);
        }

        [Fact(DisplayName = "given a motion between two C-l then the second one centers again")]
        public void MotionRestartsTheCycle()
        {
            Given("a caret mid-buffer", Buffer);
            WhenCommand(View.RecenterCommand);
            WhenKeys("h");
            WhenCommand(View.RecenterCommand);
            Assert.Equal(new[] { RevealAt.Center, RevealAt.Center }, Ui.Revealed);
        }

        [Fact(DisplayName = "given the bundled rc then C-l runs recenter-top-bottom")]
        public void BundledRcBindsRecenter()
        {
            GivenRc("");
            Rc.Binding binding = Chords.BindingFor(Chord.Parse("C-l"));
            Assert.NotNull(binding);
            Assert.Equal(View.RecenterCommand, binding.Command);
        }
    }
}
