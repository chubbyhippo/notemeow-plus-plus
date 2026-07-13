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
using static Notemeow.Core.AttachPolicy;

namespace Notemeow.Core.Tests
{
    public class AttachSpec
    {
        [Fact(DisplayName = "given the main view then meow attaches in NORMAL")]
        public void MainViewAttaches()
        {
            Assert.Equal(MeowMode.Normal, AttachMode("main"));
            Assert.True(IsWritable("main"));
        }

        [Fact(DisplayName = "given the second view then meow attaches in NORMAL")]
        public void SecondViewAttaches()
        {
            Assert.Equal(MeowMode.Normal, AttachMode("second"));
        }

        [Fact(DisplayName = "given a new untitled document then meow attaches in NORMAL")]
        public void UntitledDocumentAttaches()
        {
            Assert.Equal(MeowMode.Normal, AttachMode("untitled"));
            Assert.True(IsWritable("untitled"));
        }

        [Fact(DisplayName = "given the find results panel then NORMAL, reported read-only")]
        public void FindResultsReadOnly()
        {
            Assert.Equal(MeowMode.Normal, AttachMode("finderesult"));
            Assert.False(IsWritable("finderesult"));
        }

        [Fact(DisplayName = "given the document map then meow stays away")]
        public void DocumentMapStaysAway()
        {
            Assert.Null(AttachMode("docmap"));
        }

        [Fact(DisplayName = "given a one-line field then meow stays away")]
        public void OneLineFieldStaysAway()
        {
            Assert.Null(AttachMode("oneline"));
        }

        [Fact(DisplayName = "given dialog and search inputs then meow stays away")]
        public void DialogInputsStayAway()
        {
            Assert.Null(AttachMode("dialog"));
            Assert.Null(AttachMode("search"));
        }
    }
}
