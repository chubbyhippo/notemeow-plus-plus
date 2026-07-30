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

using Xunit;

namespace Notemeow.Core.Tests
{
    public class ChordSpec : SpecDsl
    {
        [Fact(DisplayName =
            "given the host spelling then it normalizes to the same chord as the Emacs one")]
        public void HostSpellingNormalizes()
        {
            Assert.Equal(Chord.Parse("C-f"), Chord.Parse("control F"));
            Assert.Equal(Chord.Parse("M-b"), Chord.Parse("alt B"));
            Assert.Equal(Chord.Parse("C-M-x"), Chord.Parse("control alt X"));
            Assert.False(Chord.Parse("control F").Shift);
            Assert.True(Chord.Parse("C-F").Shift);
        }

        [Fact(DisplayName =
            "given SPC or TAB as the key name then the chord parses like Emacs writes it")]
        public void NamedKeysParse()
        {
            Assert.Equal(new Chord(false, true, false, ' '), Chord.Parse("M-SPC"));
            Assert.Equal(Chord.Parse("M-SPC"), Chord.Parse("alt SPACE"));
            Assert.Equal(new Chord(true, false, false, '\t'), Chord.Parse("C-TAB"));
            Assert.Null(Chord.Parse("SPC"));
        }

        [Fact(DisplayName = "given a cmap line then it parses into a chord binding")]
        public void CmapParsesIntoChordBinding()
        {
            Rc.Config c = Rc.Parse(["cmap control F forward-char"]);
            Assert.Empty(c.Errors);
            Assert.True(c.Chords.TryGetValue(new Chord(true, false, false, 'f'), out var binding));
            Assert.Equal("forward-char", binding.Command);
        }

        [Fact(DisplayName =
            "given a cmap with no modifier or a bad keystroke then errors are collected")]
        public void BadChordsCollectErrors()
        {
            Rc.Config c = Rc.Parse(["cmap kj forward-char", "cmap control forward-char"]);
            Assert.Equal(2, c.Errors.Count);
            Assert.Contains("not a chord", c.Errors[0]);
            Assert.Contains("not a chord", c.Errors[1]);
            Assert.Empty(c.Chords);
        }

        [Fact(DisplayName =
            "given a pressed chord event then bindingFor resolves it and plain keys do not")]
        public void BindingForResolvesChordsOnly()
        {
            GivenRc("cmap C-f forward-char");
            Assert.NotNull(Chords.BindingFor(Chord.Parse("C-f")));
            Assert.Null(Chords.BindingFor(Chord.Parse("f")));
            Assert.Null(Chords.BindingFor(null));
        }

        [Fact(DisplayName = "given shift alone then it is not a chord but Ctrl and Alt-Shift are")]
        public void ShiftAloneIsNotAChord()
        {
            Assert.Null(Chord.Parse("S-f"));
            Assert.Null(Chord.Parse("shift F"));
            Assert.NotNull(Chord.Parse("C-f"));
            Assert.NotNull(Chord.Parse("alt shift E"));
            Assert.True(Chord.Parse("alt shift E").Shift);
        }

        [Fact(DisplayName =
            "given NORMAL or MOTION then a mapped chord is claimed but INSERT and KEYPAD are not")]
        public void ClaimsInNormalAndMotionOnly()
        {
            GivenRc("cmap C-f forward-char");
            Assert.True(Chords.Claims(MeowMode.Normal, Chord.Parse("C-f")));
            Assert.True(Chords.Claims(MeowMode.Motion, Chord.Parse("C-f")));
            Assert.False(Chords.Claims(MeowMode.Insert, Chord.Parse("C-f")));
            Assert.False(Chords.Claims(MeowMode.Keypad, Chord.Parse("C-f")));
            Assert.False(Chords.Claims(MeowMode.Normal, Chord.Parse("C-q")));
        }

        [Fact(DisplayName = "given an unmapped chord then it is handed back rather than swallowed")]
        public void UnmappedChordPassesThrough()
        {
            Given("plain text", "<caret>hello");
            GivenRc("");
            Assert.False(Chords.Dispatch(Ctx(), Chord.Parse("C-q")));
            ThenCaretAt(0);
        }

        [Fact(DisplayName =
            "given both spellings of a punctuation chord then they collapse to one binding")]
        public void PunctuationSpellingsCollapse()
        {
            Assert.Equal(Chord.Parse("M-<"), Chord.Parse("alt shift COMMA"));
            Assert.Equal(Chord.Parse("M->"), Chord.Parse("alt shift PERIOD"));
            Assert.Equal(Chord.Parse("M-{"), Chord.Parse("alt shift OPEN_BRACKET"));
            Assert.Equal(Chord.Parse("C-/"), Chord.Parse("control SLASH"));
            Assert.Equal(Chord.Parse("C-_"), Chord.Parse("control shift MINUS"));
            Assert.Equal(Chord.Parse("M-^"), Chord.Parse("alt shift 6"));
        }

        [Fact(DisplayName = "given the bundled defaults then the whole Emacs chord layer resolves")]
        public void BundledChordLayerResolves()
        {
            var chords = Rc.ChordBindings();
            Assert.Equal("forward-char", chords[Chord.Parse("C-f")].Command);
            Assert.Equal("backward-char", chords[Chord.Parse("C-b")].Command);
            Assert.Equal("next-line", chords[Chord.Parse("C-n")].Command);
            Assert.Equal("previous-line", chords[Chord.Parse("C-p")].Command);
            Assert.Equal("move-beginning-of-line", chords[Chord.Parse("C-a")].Command);
            Assert.Equal("move-end-of-line", chords[Chord.Parse("C-e")].Command);
            Assert.Equal("forward-word", chords[Chord.Parse("M-f")].Command);
            Assert.Equal("backward-word", chords[Chord.Parse("M-b")].Command);
            Assert.Equal("backward-sentence", chords[Chord.Parse("M-a")].Command);
            Assert.Equal("forward-sentence", chords[Chord.Parse("M-e")].Command);
            Assert.Equal("upcase-word", chords[Chord.Parse("M-u")].Command);
            Assert.Equal("downcase-word", chords[Chord.Parse("M-l")].Command);
            Assert.Equal("capitalize-word", chords[Chord.Parse("M-c")].Command);
            Assert.Equal("kill-word", chords[Chord.Parse("M-d")].Command);
            Assert.Equal("beginning-of-buffer", chords[Chord.Parse("M-<")].Command);
            Assert.Equal("end-of-buffer", chords[Chord.Parse("M->")].Command);
            Assert.Equal("backward-paragraph", chords[Chord.Parse("M-{")].Command);
            Assert.Equal("forward-paragraph", chords[Chord.Parse("M-}")].Command);
            Assert.Equal("meow-undo", chords[Chord.Parse("C-/")].Command);
            Assert.Equal("meow-undo", chords[Chord.Parse("C-_")].Command);
            Assert.Equal("meow-delete", chords[Chord.Parse("C-d")].Command);
            Assert.Equal("meow-kill", chords[Chord.Parse("C-k")].Command);
            Assert.Equal("meow-kill", chords[Chord.Parse("C-w")].Command);
            Assert.Equal("meow-save", chords[Chord.Parse("M-w")].Command);
            Assert.Equal("meow-yank", chords[Chord.Parse("C-y")].Command);
            Assert.Equal("meow-cancel-selection", chords[Chord.Parse("C-g")].Command);
            Assert.Equal("back-to-indentation", chords[Chord.Parse("M-m")].Command);
            Assert.Equal("open-line", chords[Chord.Parse("C-o")].Command);
            Assert.Equal("delete-horizontal-space", chords[Chord.Parse("M-\\")].Command);
            Assert.Equal("just-one-space", chords[Chord.Parse("M-SPC")].Command);
            Assert.Equal("ms", chords[Chord.Parse("M-^")].Keys);
        }

        [Fact(DisplayName = "given a home cmap override then it wins over the bundled default")]
        public void HomeCmapOverridesBundled()
        {
            GivenRc("cmap C-f meow-kill");
            Assert.Equal("meow-kill", Chords.BindingFor(Chord.Parse("C-f")).Command);
        }

        [Fact(DisplayName = "given a home cmap ignore then the chord is handed back to the IDE")]
        public void HomeCmapIgnoreHandsBack()
        {
            GivenRc("cmap C-f ignore");
            Assert.Null(Chords.BindingFor(Chord.Parse("C-f")));
        }

        [Fact(DisplayName = "given a NORMAL editor then dispatching a chord binding runs its command")]
        public void NormalEditorDispatchesChord()
        {
            Given("word", "<caret>hello");
            GivenRc("cmap C-f forward-char");
            Assert.True(Chords.Dispatch(Ctx(), Chord.Parse("C-f")));
            ThenCaretAt(1);
        }
    }
}
