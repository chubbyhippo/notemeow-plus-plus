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
        public readonly StringBuilder Text = new StringBuilder();
        public List<SelRange> Sels = new List<SelRange> { new SelRange(0, 0) };
        public bool Writable = true;
        public LineRange Visible;
        public int UndoCount;

        public string GetText()
        {
            return Text.ToString();
        }

        public List<SelRange> GetSelections()
        {
            return new List<SelRange>(Sels);
        }

        public void SetSelections(List<SelRange> sels)
        {
            Sels = new List<SelRange>(sels);
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
        public sealed class InfoEntry
        {
            public InfoEntry(string title, string body)
            {
                Title = title;
                Body = body;
            }

            public string Title { get; }
            public string Body { get; }
        }

        public readonly List<string> Hints = new List<string>();
        public readonly List<InfoEntry> Infos = new List<InfoEntry>();
        public readonly Queue<string> Answers = new Queue<string>();

        public readonly List<string> Ran = new List<string>();

        public readonly List<MeowMode> Modes = new List<MeowMode>();

        public List<int> ExpandHints = new List<int>();

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
            ExpandHints = new List<int>();
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
