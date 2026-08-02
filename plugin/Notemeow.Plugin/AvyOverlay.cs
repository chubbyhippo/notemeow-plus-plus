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

namespace Notemeow.Plugin
{
    internal static class AvyOverlay
    {
        internal sealed class Label(string text, int x, int y)
        {
            public string Text { get; } = text;
            public int X { get; } = x;
            public int Y { get; } = y;
        }

        private const int ColorKey = 0x00010101;
        private const int BoldWeight = 700;
        private const uint FixedPitch = 1;

        private static int LabelBg => NppApi.BgrFromRgb(Rc.OverlayColor());
        private static int LabelFg => NppApi.BgrFromRgb(Rc.OverlayTextColor());

        private static readonly OverlayWindow Window = new(
            "NotemeowAvyOverlay",
            Win32.WsExLayered | Win32.WsExTransparent | Win32.WsExNoActivate | Win32.WsExToolWindow,
            ColorKey,
            PaintInto);

        private static List<Label> current = [];
        private static int lineHeight = 14;
        private static int boxColor = LabelBg;
        private static IntPtr labelFont;
        private static int labelFontLineHeight;

        private static IntPtr LabelFont()
        {
            if (labelFont != IntPtr.Zero && labelFontLineHeight == lineHeight) return labelFont;
            if (labelFont != IntPtr.Zero) Win32.DeleteObject(labelFont);
            labelFont = Win32.CreateFontW(
                -(lineHeight * 3 / 4),
                0,
                0,
                0,
                BoldWeight,
                0,
                0,
                0,
                1,
                0,
                0,
                Win32.ClearTypeQuality,
                FixedPitch,
                "Consolas");
            labelFontLineHeight = lineHeight;
            return labelFont;
        }

        internal static void Show(IntPtr sci, List<Label> labels, int height)
        {
            Show(sci, labels, height, LabelBg);
        }

        internal static void Show(IntPtr sci, List<Label> labels, int height, int box)
        {
            current = labels ?? [];
            boxColor = box;
            if (height > 0) lineHeight = height;
            if (!Window.Ensure()) return;
            if (!Win32.TryGetScreenRect(sci, out Win32.RECT area)) return;
            Window.ShowAt(area.Left, area.Top, area.Width, area.Height);
        }

        internal static void Hide()
        {
            current = [];
            Window.Hide();
        }

        private static void PaintInto(IntPtr hdc)
        {
            IntPtr oldFont = Win32.SelectObject(hdc, LabelFont());
            _ = Win32.SetBkMode(hdc, Win32.TransparentBkMode);
            IntPtr box = Win32.CreateSolidBrush(boxColor);
            foreach (Label lb in current)
            {
                string text = lb.Text ?? "";
                if (text.Length == 0) continue;
                Win32.GetTextExtentPoint32W(hdc, text, text.Length, out Win32.SIZE ext);
                int boxH = Math.Max(ext.Cy, lineHeight);
                var r = new Win32.RECT
                {
                    Left = lb.X,
                    Top = lb.Y,
                    Right = lb.X + ext.Cx + 4,
                    Bottom = lb.Y + boxH,
                };
                Win32.FillRect(hdc, ref r, box);
                _ = Win32.SetTextColor(hdc, LabelFg);
                Win32.TextOutW(hdc, lb.X + 2, lb.Y, text, text.Length);
            }
            Win32.DeleteObject(box);
            Win32.SelectObject(hdc, oldFont);
        }
    }
}
