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
using System.Linq;
using Notemeow.Core;
using Xunit;

namespace Notemeow.Core.Tests
{
    public class RcSpec : SpecDsl
    {
        [Fact(DisplayName = "given an action mapping then it parses into a normal override")]
        public void ActionMappingParses()
        {
            Rc.Config c = Rc.Parse(new List<string> { "nmap S <action>(extension.aceJump)" });
            Assert.Equal("extension.aceJump", c.Normal['S'].Action);
            Assert.Empty(c.Errors);
        }

        [Fact(DisplayName = "given a parameterized action then the whole serialized command is kept")]
        public void ParameterizedActionKeptWhole()
        {
            string id =
                "org.eclipse.ui.views.showView("
                + "org.eclipse.ui.views.showView.viewId=org.eclipse.ui.views.BookmarkView)";
            Rc.Config c = Rc.Parse(new List<string> { "map <leader>bj <action>(" + id + ")" });
            Assert.Equal(id, c.Keypad["bj"].Action);
            Assert.Empty(c.Errors);
        }

        [Fact(DisplayName = "given comment-only rc edits then the reload button reports no changes")]
        public void CommentOnlyEditsNoReload()
        {
            Rc.SetUserLines(new List<string> { "nmap Z ,b" });
            Assert.True(RcFileState.EqualTo(new List<string> { "\" just a comment", "nmap Z ,b" }));
            Assert.False(RcFileState.EqualTo(new List<string> { "nmap Q meow-goto-line" }));
        }

        [Fact(DisplayName = "given a key-sequence mapping then it parses as replay keys")]
        public void KeySequenceParsesAsReplay()
        {
            Rc.Config c = Rc.Parse(new List<string> { "nmap Z ,b" });
            Assert.Equal(",b", c.Normal['Z'].Keys);
            Assert.True(c.Normal['Z'].Recursive);
        }

        [Fact(DisplayName = "given nnoremap then the binding is non-recursive")]
        public void NnoremapNonRecursive()
        {
            Rc.Config c = Rc.Parse(new List<string> { "nnoremap Z ,b" });
            Assert.False(c.Normal['Z'].Recursive);
        }

        [Fact(DisplayName = "given a meow command name then it parses into a command binding")]
        public void MeowCommandNameParses()
        {
            Rc.Config c = Rc.Parse(
                new List<string> { "nmap n meow-mark-word", "nmap d ignore", "nmap Z repeat" });
            Assert.Equal("meow-mark-word", c.Normal['n'].Command);
            Assert.Equal("ignore", c.Normal['d'].Command);
            Assert.Equal("repeat", c.Normal['Z'].Command);
            Assert.Empty(c.Errors);
        }

        [Fact(DisplayName = "given mmap then the binding lands in the motion map")]
        public void MmapLandsInMotionMap()
        {
            Rc.Config c = Rc.Parse(new List<string> { "mmap n meow-next", "mnoremap e k" });
            Assert.Equal("meow-next", c.Motion['n'].Command);
            Assert.Equal("k", c.Motion['e'].Keys);
            Assert.False(c.Motion['e'].Recursive);
            Assert.Empty(c.Normal);
            Assert.Empty(c.Errors);
        }

        [Fact(DisplayName = "given an unknown meow command then an error is collected")]
        public void UnknownCommandCollectsError()
        {
            Rc.Config c = Rc.Parse(new List<string> { "nmap Z meow-frobnicate" });
            Assert.Single(c.Errors);
            Assert.Contains("meow-frobnicate", c.Errors[0]);
        }

        [Fact(DisplayName = "given a cmap or cnoremap line then the rc loads it without error")]
        public void CmapCnoremapLoadsWithoutError()
        {
            Rc.Config c = Rc.Parse(new List<string> { "cmap kj <Esc>", "cnoremap <C-a> <Home>" });
            Assert.Empty(c.Errors);
            Assert.Empty(c.Normal);
            Assert.Empty(c.Motion);
            Assert.Empty(c.Keypad);
        }

        [Fact(DisplayName = "given leader mappings and descriptions then the keypad table extends")]
        public void LeaderMappingsExtendKeypad()
        {
            GivenRc(
                "map <leader>gd <action>(editor.action.revealDefinition)\ndesc <leader>g goto things");
            Assert.Equal("editor.action.revealDefinition", Rc.Cfg().Keypad["gd"].Action);
            Assert.Equal("goto things", Rc.Cfg().KeypadDesc["g"]);
            Assert.Equal("editor.action.revealDefinition", Rc.Keypad()["gd"].Action);
            Assert.Equal("notemeow.editRc", Rc.Keypad()["cm"].Action);
        }

        [Fact(DisplayName = "given the ideavimrc WhichKeyDesc let syntax then descriptions parse")]
        public void WhichKeyDescLetSyntaxParses()
        {
            Rc.Config c = Rc.Parse(
                new List<string> { "let g:WhichKeyDesc_leader_x = \"<leader>x C-x files/buffers\"" });
            Assert.Equal("C-x files/buffers", c.KeypadDesc["x"]);
            Assert.Empty(c.Errors);
        }

        [Fact(DisplayName = "given set lines then which-key options apply and vim options are ignored")]
        public void SetLinesApplyWhichKeyOptions()
        {
            Rc.Config c = Rc.Parse(
                new List<string>
                {
                    "set nowhich-key",
                    "set timeoutlen=400",
                    "set clipboard+=unnamedplus",
                    "let mapleader=\" \"",
                });
            Assert.Equal(false, c.WhichKey);
            Assert.Equal(400, c.WhichKeyDelayMs);
            Assert.Empty(c.Errors);
        }

        [Fact(DisplayName = "which-key settings layer user over bundled defaults")]
        public void WhichKeyLayersUserOverBundled()
        {
            Assert.True(Rc.WhichKeyEnabled());
            Assert.Equal(300, Rc.WhichKeyDelayMs());
            GivenRc("set nowhich-key\nset timeoutlen=150");
            Assert.False(Rc.WhichKeyEnabled());
            Assert.Equal(150, Rc.WhichKeyDelayMs());
        }

        [Fact(DisplayName = "given overlay color set lines then they parse into rgb colors")]
        public void OverlayColorSetLinesParseToRgb()
        {
            Rc.Config c = Rc.Parse(
                new List<string>
                {
                    "set overlay-color=#E52B50",
                    "set overlay-text-color=#ffffff",
                    "set expand-hint-color=#d05c0a",
                    "set grab-color=#CDE8CD",
                });
            Assert.Equal(0xE52B50, c.OverlayColor);
            Assert.Equal(0xFFFFFF, c.OverlayTextColor);
            Assert.Equal(0xD05C0A, c.ExpandHintColor);
            Assert.Equal(0xCDE8CD, c.GrabColor);
            Assert.Empty(c.Errors);
        }

        [Fact(DisplayName = "given a malformed overlay color then an error is collected and it stays unset")]
        public void MalformedOverlayColorErrorsAndStaysUnset()
        {
            Rc.Config c = Rc.Parse(
                new List<string> { "set overlay-color=#12345", "set grab-color=nope" });
            Assert.Null(c.OverlayColor);
            Assert.Null(c.GrabColor);
            Assert.Equal(2, c.Errors.Count);
            Assert.Contains("overlay-color", c.Errors[0]);
        }

        [Fact(DisplayName = "given an unknown set color option then it is ignored without error")]
        public void UnknownSetColorOptionIgnored()
        {
            Rc.Config c = Rc.Parse(new List<string> { "set cursor-color=#123456" });
            Assert.Null(c.OverlayColor);
            Assert.Empty(c.Errors);
        }

        [Fact(DisplayName = "overlay colors layer user over the bundled default")]
        public void OverlayColorsLayerUserOverBundled()
        {
            Assert.Equal(0xE52B50, Rc.OverlayColor());
            GivenRc("set overlay-color=#010203\nset grab-color=#040506");
            Assert.Equal(0x010203, Rc.OverlayColor());
            Assert.Equal(0x040506, Rc.GrabColor());
        }

        [Fact(DisplayName = "given a trailing comment then it is stripped from the line")]
        public void TrailingCommentStripped()
        {
            Rc.Config c = Rc.Parse(
                new List<string>
                {
                    "nmap S <action>(extension.aceJump)   \" jump anywhere",
                    "map <leader>zz ,b            \" select the buffer",
                });
            Assert.Equal("extension.aceJump", c.Normal['S'].Action);
            Assert.Equal(",b", c.Keypad["zz"].Keys);
            Assert.Empty(c.Errors);
        }

        [Fact(DisplayName = "the bundled default notemeowrc defines the whole keymap")]
        public void BundledRcDefinesWholeKeymap()
        {
            Rc.Config d = Rc.Defaults();
            Assert.Empty(d.Errors);
            foreach (var e in Qwerty())
            {
                if (e.Key == 'Q') continue;
                Rc.Binding b = d.Normal.TryGetValue(e.Key, out Rc.Binding v) ? v : null;
                Assert.NotNull(b);
                Assert.Equal(e.Value, b.Command);
            }
            Assert.Equal("avy-goto-line", d.Normal['Q'].Command);
            Assert.Equal("avy-goto-char-timer", d.Normal['S'].Command);
            Assert.Equal("meow-next", d.Motion['j'].Command);
            Assert.Equal("meow-prev", d.Motion['k'].Command);
            Assert.Equal("IDM_VIEW_SWITCHTO_DOCLIST", d.Keypad[" "].Action);
            Assert.Equal("notemeow.editRc", d.Keypad["cm"].Action);
            Assert.Equal("notemeow.reloadRc", d.Keypad["cM"].Action);
            Assert.Equal("notemeow.aceResize", d.Keypad["wr"].Action);
            Assert.Equal("IDM_FILE_SAVE", d.Keypad["xs"].Action);
            Assert.True(d.Keypad.Count > 25);
        }

        [Fact(DisplayName = "given bad lines then errors are collected with line numbers")]
        public void BadLinesCollectErrors()
        {
            Rc.Config c = Rc.Parse(
                new List<string>
                {
                    "frobnicate everything",
                    "nmap <Space> ,b",
                    "map <leader>1 <action>(X)",
                    "nmap Q <CR>",
                    "mmap <leader>x ,b",
                });
            Assert.Equal(5, c.Errors.Count);
            Assert.StartsWith("line 1", c.Errors[0]);
        }

        [Fact(DisplayName = "given an rc key-sequence override then the key replays through the engine")]
        public void KeySequenceOverrideReplays()
        {
            Given("two words", "on<caret>e two");
            GivenRc("nmap Z ,b");
            WhenKeys("Z");
            ThenSelection("one two");
        }

        [Fact(DisplayName = "given a recursive map then the RHS expands user maps")]
        public void RecursiveMapExpandsUserMaps()
        {
            Given("two words", "one two<caret>");
            GivenRc("nmap B ,b\nnmap Y B");
            WhenKeys("Y");
            ThenSelection("one two");
        }

        [Fact(DisplayName = "given nnoremap then the RHS runs the bundled default instead")]
        public void NnoremapRunsBundledDefault()
        {
            Given("two words", "one two<caret>");
            GivenRc("nmap B ,b\nnnoremap Z B");
            WhenKeys("Z");
            ThenSelection("two");
        }

        [Fact(DisplayName = "given a self-referencing map then recursion is depth-limited")]
        public void SelfReferenceDepthLimited()
        {
            Given("plain", "<caret>hello");
            GivenRc("nmap Z Z");
            WhenKeys("Z");
            ThenText("hello");
        }

        [Fact(DisplayName = "given an rc keypad mapping with keys then SPC seq replays them")]
        public void KeypadMappingReplaysKeys()
        {
            Given("two words", "on<caret>e two");
            GivenRc("map <leader>k ,b");
            WhenKeys(" k");
            ThenSelection("one two");
            ThenMode(MeowMode.Normal);
        }

        [Fact(DisplayName = "given an rc keypad mapping then it overrides the bundled entry")]
        public void KeypadMappingOverridesBundled()
        {
            Given("two words", "on<caret>e two");
            GivenRc("map <leader>bm ,b");
            WhenKeys(" bm");
            ThenSelection("one two");
        }

        [Fact(DisplayName = "given a layout rebinding then the key runs the meow command")]
        public void LayoutRebindingRunsCommand()
        {
            Given("two words", "on<caret>e two");
            GivenRc("nmap n meow-mark-word");
            WhenKeys("n");
            ThenSelection("one");
        }

        [Fact(DisplayName = "given ignore then the key is disabled")]
        public void IgnoreDisablesKey()
        {
            Given("chars", "<caret>abc");
            GivenRc("nmap d ignore");
            WhenKeys("d");
            ThenText("abc");
        }

        [Fact(DisplayName = "given a motion rebinding then MOTION-state editors use it")]
        public void MotionRebindingApplies()
        {
            Given("three lines", "<caret>one\ntwo\nthree");
            GivenRc("mmap n meow-next");
            St.Mode = MeowMode.Motion;
            WhenKeys("n");
            Assert.Equal(1, CaretLine());
            WhenKeys("j");
            Assert.Equal(2, CaretLine());
        }

        [Fact(DisplayName = "given repeat on another key then it repeats the last command")]
        public void RepeatRebindingRepeatsLast()
        {
            Given("chars", "<caret>abcdef");
            GivenRc("nmap Z repeat");
            WhenKeys("d");
            ThenText("bcdef");
            WhenKeys("Z");
            ThenText("cdef");
        }

        [Fact(DisplayName = "given a mapped key when quote then the mapping repeats")]
        public void QuoteRepeatsMappedKey()
        {
            Given("chars", "<caret>abcdef");
            GivenRc("nmap Z d");
            WhenKeys("Z");
            ThenText("bcdef");
            WhenKeys("'");
            ThenText("cdef");
        }

        [Fact(DisplayName = "given keypad entries then which-key rows show terminals and groups")]
        public void WhichKeyShowsTerminalsAndGroups()
        {
            GivenRc("map <leader>zz <action>(workbench.action.quickOpen)\ndesc <leader>z my group");
            List<WhichKey.Row> top = WhichKey.KeypadRows("");
            Assert.True(top.Any(r => r.Key == "z" && r.Label == "my group"));
            List<WhichKey.Row> inner = WhichKey.KeypadRows("z");
            Assert.True(inner.Any(r => r.Key == "z" && r.Label == "workbench.action.quickOpen"));
        }

        [Fact(DisplayName = "given a terminal with a description then which-key prefers it")]
        public void WhichKeyPrefersDescription()
        {
            GivenRc("map <leader>zz <action>(workbench.action.quickOpen)\ndesc <leader>zz open a file");
            Assert.True(
                WhichKey.KeypadRows("z").Any(r => r.Key == "z" && r.Label == "open a file"));
        }

        [Fact(DisplayName = "given the default table then the SPC SPC entry renders as SPC")]
        public void SpcSpcRendersAsSpc()
        {
            Assert.True(WhichKey.KeypadRows("").Any(r => r.Key == "SPC"));
        }

        private static Dictionary<char, string> Qwerty()
        {
            var m = new Dictionary<char, string>();
            for (int n = 0; n <= 9; n++) m[(char)('0' + n)] = "meow-expand-" + n;
            m['-'] = "meow-negative-argument";
            m[';'] = "meow-reverse";
            m[','] = "meow-inner-of-thing";
            m['.'] = "meow-bounds-of-thing";
            m['['] = "meow-beginning-of-thing";
            m[']'] = "meow-end-of-thing";
            m['<'] = "meow-beginning-of-thing";
            m['>'] = "meow-end-of-thing";
            m['a'] = "meow-append";
            m['A'] = "meow-open-below";
            m['b'] = "meow-back-word";
            m['B'] = "meow-back-symbol";
            m['c'] = "meow-change";
            m['d'] = "meow-delete";
            m['D'] = "meow-backward-delete";
            m['e'] = "meow-next-word";
            m['E'] = "meow-next-symbol";
            m['f'] = "meow-find";
            m['g'] = "meow-cancel-selection";
            m['G'] = "meow-grab";
            m['h'] = "meow-left";
            m['H'] = "meow-left-expand";
            m['i'] = "meow-insert";
            m['I'] = "meow-open-above";
            m['j'] = "meow-next";
            m['J'] = "meow-next-expand";
            m['k'] = "meow-prev";
            m['K'] = "meow-prev-expand";
            m['l'] = "meow-right";
            m['L'] = "meow-right-expand";
            m['m'] = "meow-join";
            m['n'] = "meow-search";
            m['o'] = "meow-block";
            m['O'] = "meow-to-block";
            m['p'] = "meow-yank";
            m['q'] = "meow-quit";
            m['Q'] = "meow-goto-line";
            m['r'] = "meow-replace";
            m['R'] = "meow-swap-grab";
            m['s'] = "meow-kill";
            m['t'] = "meow-till";
            m['u'] = "meow-undo";
            m['U'] = "meow-undo-in-selection";
            m['v'] = "meow-visit";
            m['w'] = "meow-mark-word";
            m['W'] = "meow-mark-symbol";
            m['x'] = "meow-line";
            m['X'] = "meow-goto-line";
            m['y'] = "meow-save";
            m['Y'] = "meow-sync-grab";
            m['z'] = "meow-pop-selection";
            m['\''] = "repeat";
            return m;
        }
    }
}
