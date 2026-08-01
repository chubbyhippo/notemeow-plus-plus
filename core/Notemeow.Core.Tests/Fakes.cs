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
using System.Text;
using Notemeow.Core;

namespace Notemeow.Core.Tests
{
    public class FakeEditor : IEditorPort
    {
        public readonly StringBuilder Text = new();
        public List<SelRange> Sels = [new SelRange(0, 0)];
        public bool Writable = true;
        public LineRange Visible;
        public int UndoCount;

        public string GetText()
        {
            return Text.ToString();
        }

        public List<SelRange> GetSelections()
        {
            return [.. Sels];
        }

        public void SetSelections(List<SelRange> sels)
        {
            Sels = [.. sels];
        }

        public void Edit(List<TextEdit> edits)
        {
            var ordered = new List<TextEdit>(edits);
            ordered.Sort((a, b) => b.Start.CompareTo(a.Start));
            foreach (TextEdit e in ordered)
            {
                Text.Remove(e.Start, e.End - e.Start);
                Text.Insert(e.Start, e.Text);
            }
        }

        public bool IsWritable()
        {
            return Writable;
        }

        public LineRange VisibleLineRange()
        {
            return Visible;
        }

        public void Undo()
        {
            UndoCount++;
        }

        public void CloseEditor()
        {
        }

        public OffsetRange SymbolRangeAt(int offset)
        {
            return null;
        }
    }

    public class FakeClipboard : IClipboardPort
    {
        public string Content;

        public string Read()
        {
            return Content;
        }

        public void Write(string text)
        {
            Content = text;
        }
    }

    public class FakeUi : IUiPort
    {
        public sealed class InfoEntry(string title, string body)
        {
            public string Title { get; } = title;
            public string Body { get; } = body;
        }

        public readonly List<string> Hints = [];
        public readonly List<InfoEntry> Infos = [];
        public readonly Queue<string> Answers = new();

        public readonly List<string> Ran = [];

        public readonly List<MeowMode> Modes = [];

        public List<int> ExpandHints = [];

        public readonly List<RevealAt> Revealed = [];

        public void RevealCaret(RevealAt at)
        {
            Revealed.Add(at);
        }

        public void Hint(string text)
        {
            Hints.Add(text);
        }

        public void Info(string title, string body)
        {
            Infos.Add(new InfoEntry(title, body));
        }

        public string Input(string prompt, string initial)
        {
            return Answers.Count > 0 ? Answers.Dequeue() : null;
        }

        public void RunCommand(string id)
        {
            Ran.Add(id);
        }

        public void ScheduleWhichKey(string kind, string buffer)
        {
        }

        public void HideWhichKey()
        {
        }

        public void ShowExpandHints(List<int> positions)
        {
            ExpandHints = positions;
        }

        public void ClearExpandHints()
        {
            ExpandHints = [];
        }

        public void ShowAvyMatches(List<OffsetRange> matches)
        {
        }

        public void ShowAvyLabels(List<AvyLabel> labels)
        {
        }

        public void ClearAvy()
        {
        }

        public void ModeChanged(MeowState st)
        {
            Modes.Add(st.Mode);
        }

        public void Refresh(MeowState st)
        {
        }
    }
}
