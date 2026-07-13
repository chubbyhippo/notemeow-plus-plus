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

namespace Notemeow.Core
{
    public interface IEditorPort
    {
        string GetText();

        List<SelRange> GetSelections();

        void SetSelections(List<SelRange> sels);

        void Edit(List<TextEdit> edits);

        bool IsWritable();

        LineRange VisibleLineRange();

        void Undo();

        void CloseEditor();

        OffsetRange SymbolRangeAt(int offset);
    }

    public interface IClipboardPort
    {
        string Read();

        void Write(string text);
    }

    public interface IUiPort
    {
        void Hint(string text);

        void Info(string title, string body);

        string Input(string prompt, string initial);

        void RunCommand(string id);

        void ScheduleWhichKey(string kind, string buffer);

        void HideWhichKey();

        void ShowExpandHints(List<int> positions);

        void ClearExpandHints();

        void ShowAvyMatches(List<OffsetRange> matches);

        void ShowAvyLabels(List<AvyLabel> labels);

        void ClearAvy();

        void ModeChanged(MeowState st);

        void Refresh(MeowState st);
    }

    public delegate void MeowCommand(Ctx ctx);

    public sealed class Ctx
    {
        public Ctx(IEditorPort port, IClipboardPort clipboard, IUiPort ui, MeowState st)
        {
            Port = port;
            Clipboard = clipboard;
            Ui = ui;
            St = st;
        }

        public IEditorPort Port { get; }
        public IClipboardPort Clipboard { get; }
        public IUiPort Ui { get; }
        public MeowState St { get; }

        public void SetMode(MeowMode mode)
        {
            St.Mode = mode;
            if (mode != MeowMode.Keypad) St.Keypad.Length = 0;
            Ui.ModeChanged(St);
        }
    }
}
