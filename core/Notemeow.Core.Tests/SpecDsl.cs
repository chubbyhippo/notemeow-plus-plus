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

using System;
using System.Collections.Generic;
using Notemeow.Core;
using Xunit;

namespace Notemeow.Core.Tests
{
    public abstract class SpecDsl
    {
        protected FakeEditor Editor;
        protected FakeClipboard Clip;
        protected FakeUi Ui;
        protected MeowState St;

        protected SpecDsl()
        {
            Editor = new FakeEditor();
            Clip = new FakeClipboard();
            Ui = new FakeUi();
            St = new MeowState();
            Rc.SetForTest(new Rc.Config());
        }

        protected Ctx Ctx()
        {
            return new Ctx(Editor, Clip, Ui, St);
        }

        protected void Given(string description, string textWithCaret)
        {
            int at = textWithCaret.IndexOf("<caret>", StringComparison.Ordinal);
            Editor.Text.Length = 0;
            Editor.Text.Append(textWithCaret.Replace("<caret>", ""));
            int off = Math.Max(at, 0);
            Editor.Sels = new List<SelRange> { new SelRange(off, off) };
            St = new MeowState();
        }

        protected void GivenRc(string text)
        {
            Rc.SetForTest(Rc.Parse(new List<string>(text.Split(new[] { '\n' }))));
        }

        protected void GivenClipboard(string text)
        {
            Clip.Content = text;
        }

        protected void GivenMinibufferAnswers(params string[] answers)
        {
            foreach (string a in answers) Ui.Answers.Enqueue(a);
        }

        protected void GivenCaretAt(int offset)
        {
            Editor.Sels = new List<SelRange> { new SelRange(offset, offset) };
        }

        protected void GivenReadOnly()
        {
            Editor.Writable = false;
        }

        protected void WhenKeys(string keys)
        {
            for (int i = 0; i < keys.Length; i++) Engine.HandleChar(Ctx(), keys[i]);
        }

        protected void WhenCommand(string command)
        {
            Engine.RunEmacsMotion(Ctx(), command);
        }

        protected bool PressEsc()
        {
            return Engine.EscapeKey(Ctx());
        }

        protected string SelectedText()
        {
            SelRange s = Editor.Sels[0];
            if (s.Anchor == s.Active) return null;
            int lo = Math.Min(s.Anchor, s.Active);
            int hi = Math.Max(s.Anchor, s.Active);
            return Editor.GetText().Substring(lo, hi - lo);
        }

        protected int CaretLine()
        {
            return Text.LineOfOffset(Editor.GetText(), Editor.Sels[0].Active);
        }

        protected void ThenSelection(string expected)
        {
            Assert.Equal(expected, SelectedText());
        }

        protected void ThenNoSelection()
        {
            SelRange s = Editor.Sels[0];
            Assert.Equal(s.Anchor, s.Active);
        }

        protected void ThenCaretAt(int offset)
        {
            Assert.Equal(offset, Editor.Sels[0].Active);
        }

        protected void ThenCaretLine(int line)
        {
            Assert.Equal(line, CaretLine());
        }

        protected void ThenCaretAtSelectionStart()
        {
            SelRange s = Editor.Sels[0];
            Assert.NotEqual(s.Anchor, s.Active);
            Assert.Equal(Math.Min(s.Anchor, s.Active), s.Active);
        }

        protected void ThenCaretAtSelectionEnd()
        {
            SelRange s = Editor.Sels[0];
            Assert.NotEqual(s.Anchor, s.Active);
            Assert.Equal(Math.Max(s.Anchor, s.Active), s.Active);
        }

        protected void ThenText(string expected)
        {
            Assert.Equal(expected, Editor.GetText());
        }

        protected void ThenMode(MeowMode expected)
        {
            Assert.Equal(expected, St.Mode);
        }

        protected void ThenSelType(SelType expected)
        {
            Assert.Equal(expected, St.SelType);
        }

        protected void ThenClipboard(string expected)
        {
            Assert.Equal(expected, Clip.Content);
        }

        protected void ThenCaretCount(int expected)
        {
            Assert.Equal(expected, Editor.Sels.Count);
        }
    }
}
