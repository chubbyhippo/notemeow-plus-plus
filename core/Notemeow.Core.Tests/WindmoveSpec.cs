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
using static Notemeow.Core.Windmove;

namespace Notemeow.Core.Tests
{
    public class WindmoveSpec : SpecDsl
    {
        private static ViewLayout SideBySide(bool onSecond)
        {
            return new ViewLayout(true, false, onSecond);
        }

        private static ViewLayout Stacked(bool onSecond)
        {
            return new ViewLayout(true, true, onSecond);
        }

        private static ViewLayout SingleView()
        {
            return new ViewLayout(false, false, false);
        }

        [Fact(DisplayName =
            "given side-by-side views then left from the second view crosses to the main")]
        public void LeftFromSecondCrosses()
        {
            Assert.Equal("notemeow.focusOtherView", Plan(Dir.Left, SideBySide(true)));
        }

        [Fact(DisplayName =
            "given side-by-side views then right from the main view crosses to the second")]
        public void RightFromMainCrosses()
        {
            Assert.Equal("notemeow.focusOtherView", Plan(Dir.Right, SideBySide(false)));
        }

        [Fact(DisplayName = "given side-by-side views then the outward directions find no window")]
        public void OutwardDirectionsNoWindow()
        {
            Assert.Null(Plan(Dir.Left, SideBySide(false)));
            Assert.Null(Plan(Dir.Right, SideBySide(true)));
        }

        [Fact(DisplayName = "given side-by-side views then up and down find no window")]
        public void UpDownNoWindow()
        {
            Assert.Null(Plan(Dir.Up, SideBySide(false)));
            Assert.Null(Plan(Dir.Down, SideBySide(true)));
        }

        [Fact(DisplayName =
            "given stacked views then down from the main and up from the second cross over")]
        public void StackedCrossings()
        {
            Assert.Equal("notemeow.focusOtherView", Plan(Dir.Down, Stacked(false)));
            Assert.Equal("notemeow.focusOtherView", Plan(Dir.Up, Stacked(true)));
            Assert.Null(Plan(Dir.Left, Stacked(true)));
            Assert.Null(Plan(Dir.Down, Stacked(true)));
        }

        [Fact(DisplayName = "given a single view then every direction finds no window")]
        public void SingleViewNoWindow()
        {
            Assert.Null(Plan(Dir.Left, SingleView()));
            Assert.Null(Plan(Dir.Right, SingleView()));
            Assert.Null(Plan(Dir.Up, SingleView()));
            Assert.Null(Plan(Dir.Down, SingleView()));
        }

        [Fact(DisplayName = "given no window in the direction then the message is Emacs verbatim")]
        public void NoWindowMessageVerbatim()
        {
            Assert.Equal("No window left from selected window", NoWindowMessage(Dir.Left));
            Assert.Equal("No window down from selected window", NoWindowMessage(Dir.Down));
        }

        [Fact(DisplayName = "given the bundled rc then SPC w hjkl dispatch windmove")]
        public void BundledRcWindmoveBindings()
        {
            Rc.Config d = Rc.Defaults();
            Assert.Equal("notemeow.windmoveLeft", d.Keypad["wh"].Action);
            Assert.Equal("notemeow.windmoveDown", d.Keypad["wj"].Action);
            Assert.Equal("notemeow.windmoveUp", d.Keypad["wk"].Action);
            Assert.Equal("notemeow.windmoveRight", d.Keypad["wl"].Action);
        }

        [Fact(DisplayName = "given one two or many windows then ace-window plans self other or labels")]
        public void AceWindowPlansSelfOtherOrLabels()
        {
            Assert.Equal(AceWindow.Plan.None, AceWindow.PlanFor(1));
            Assert.Equal(AceWindow.Plan.Other, AceWindow.PlanFor(2));
            Assert.Equal(AceWindow.Plan.Labels, AceWindow.PlanFor(3));
            Assert.Equal(AceWindow.Plan.Labels, AceWindow.PlanFor(9));
        }
    }
}
